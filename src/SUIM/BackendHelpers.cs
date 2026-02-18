namespace SUIM;

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using SUIM.Components;

/// <summary>
/// Backend-agnostic helpers used by engine-specific integrations (Stride, Unity, Godot, etc.).
/// Contains color parsing, model method resolution and property binding setup utilities.
/// </summary>
public static class BackendHelpers
{
    public readonly struct ParsedColor
    {
        public byte R { get; init; }
        public byte G { get; init; }
        public byte B { get; init; }
        public byte A { get; init; }
    }

    public static ParsedColor ParseColor(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr)) return new ParsedColor { R = 255, G = 255, B = 255, A = 255 };

        if (colorStr.StartsWith('#'))
        {
            var hex = colorStr.Substring(1);
            if (hex.Length == 6)
            {
                return new ParsedColor
                {
                    R = Convert.ToByte(hex.Substring(0, 2), 16),
                    G = Convert.ToByte(hex.Substring(2, 2), 16),
                    B = Convert.ToByte(hex.Substring(4, 2), 16),
                    A = 255
                };
            }
            else if (hex.Length == 8)
            {
                return new ParsedColor
                {
                    A = Convert.ToByte(hex.Substring(0, 2), 16),
                    R = Convert.ToByte(hex.Substring(2, 2), 16),
                    G = Convert.ToByte(hex.Substring(4, 2), 16),
                    B = Convert.ToByte(hex.Substring(6, 2), 16)
                };
            }
        }

        // named colors (basic set)
        if (string.Equals(colorStr, "red", StringComparison.OrdinalIgnoreCase)) return new ParsedColor { R = 255, G = 0, B = 0, A = 255 };
        if (string.Equals(colorStr, "green", StringComparison.OrdinalIgnoreCase)) return new ParsedColor { R = 0, G = 255, B = 0, A = 255 };
        if (string.Equals(colorStr, "blue", StringComparison.OrdinalIgnoreCase)) return new ParsedColor { R = 0, G = 0, B = 255, A = 255 };
        if (string.Equals(colorStr, "black", StringComparison.OrdinalIgnoreCase)) return new ParsedColor { R = 0, G = 0, B = 0, A = 255 };
        if (string.Equals(colorStr, "yellow", StringComparison.OrdinalIgnoreCase)) return new ParsedColor { R = 255, G = 255, B = 0, A = 255 };
        if (string.Equals(colorStr, "cyan", StringComparison.OrdinalIgnoreCase)) return new ParsedColor { R = 0, G = 255, B = 255, A = 255 };
        if (string.Equals(colorStr, "magenta", StringComparison.OrdinalIgnoreCase)) return new ParsedColor { R = 255, G = 0, B = 255, A = 255 };
        if (string.Equals(colorStr, "transparent", StringComparison.OrdinalIgnoreCase)) return new ParsedColor { R = 0, G = 0, B = 0, A = 0 };
        if (string.Equals(colorStr, "white", StringComparison.OrdinalIgnoreCase)) return new ParsedColor { R = 255, G = 255, B = 255, A = 255 };

        // fallback white
        return new ParsedColor { R = 255, G = 255, B = 255, A = 255 };
    }

    /// <summary>
    /// Resolve a method name on a model object into a Delegate using priority-based resolution.
    /// Priority: parameterless -> UIElement parameter -> EventHandler pattern -> fallback.
    /// This does not have any engine-specific RoutedEventArgs knowledge.
    /// Also supports returning a Delegate stored in a property whose name matches methodName.
    /// </summary>
    public static Delegate? ResolveMethodAsDelegate(string methodName, object? model)
    {
        if (model == null) return null;

        // If model is an ObservableObject, prefer its GetHandler (it already implements priority rules)
        if (model is ObservableObject oo)
        {
            return oo.GetHandler(methodName);
        }

        // If model has a property with the name that contains a Delegate, return it directly
        try
        {
            var prop = model.GetType().GetProperty(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null && typeof(Delegate).IsAssignableFrom(prop.PropertyType))
            {
                if (prop.GetValue(model) is Delegate val) return val;
            }
        }
        catch { }

        // Cast to object to avoid dynamic dispatch issues
        object targetObject = (object)model;
        var methods = targetObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        var matchingMethods = methods.Where(m => m.Name == methodName).ToList();
        if (matchingMethods.Count == 0) return null;

        // 1. Priority: Parameterless method (Action)
        var parameterless = matchingMethods.FirstOrDefault(m => m.GetParameters().Length == 0);
        if (parameterless != null)
        {
            try { return Delegate.CreateDelegate(typeof(Action), targetObject, parameterless); } catch { }
        }

        // 2. Priority: Method taking single UIElement (Action<UIElement>)
        var uiElementMethod = matchingMethods.FirstOrDefault(m =>
            m.GetParameters().Length == 1
            && typeof(UIElement).IsAssignableFrom(m.GetParameters()[0].ParameterType));
        if (uiElementMethod != null)
        {
            try { return Delegate.CreateDelegate(typeof(Action<UIElement>), targetObject, uiElementMethod); } catch { }
        }

        // 3. Priority: EventHandler pattern (object sender, EventArgs e)
        var eventHandlerMethod = matchingMethods.FirstOrDefault(m =>
        {
            var parms = m.GetParameters();
            return parms.Length == 2
                && parms[0].ParameterType == typeof(object)
                && typeof(EventArgs).IsAssignableFrom(parms[1].ParameterType);
        });
        if (eventHandlerMethod != null)
        {
            try { return Delegate.CreateDelegate(typeof(EventHandler), targetObject, eventHandlerMethod); } catch { }
        }

        // Fallback: try to create a delegate for the first method using heuristics
        var fallback = matchingMethods[0];
        var parameters = fallback.GetParameters();
        try
        {
            if (parameters.Length == 0) return Delegate.CreateDelegate(typeof(Action), targetObject, fallback);
            if (parameters.Length == 1 && typeof(UIElement).IsAssignableFrom(parameters[0].ParameterType)) return Delegate.CreateDelegate(typeof(Action<UIElement>), targetObject, fallback);
            if (parameters.Length == 2) return Delegate.CreateDelegate(typeof(EventHandler), targetObject, fallback);
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Setup property binding: immediately calls applyValue with current value and subscribes to INotifyPropertyChanged if available.
    /// Returns an unsubscribe Action if a subscription was made; otherwise null.
    /// </summary>
    public static Action? SetupPropertyBinding(object? model, string modelPropertyName, Action<object?> applyValue)
    {
        if (model == null) return null;

        try
        {
            object? value = null;
            if (model is ObservableObject oo)
            {
                value = oo.GetValue(modelPropertyName);
            }
            else
            {
                value = model.GetType().GetProperty(modelPropertyName)?.GetValue(model);
            }
            applyValue(value);
        }
        catch { }

        if (model is INotifyPropertyChanged inpc)
        {
            void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == modelPropertyName)
                {
                    try
                    {
                        object? newValue = null;
                        if (model is ObservableObject oo2)
                        {
                            newValue = oo2.GetValue(modelPropertyName);
                        }
                        else
                        {
                            newValue = model.GetType().GetProperty(modelPropertyName)?.GetValue(model);
                        }
                        applyValue(newValue);
                    }
                    catch { }
                }
            }

            inpc.PropertyChanged += OnPropertyChanged;
            return () => inpc.PropertyChanged -= OnPropertyChanged;
        }

        return null;
    }
}
