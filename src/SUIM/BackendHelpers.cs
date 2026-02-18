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
    /// Resolve a method name or expression on a model object into a Delegate.
    /// Supports:
    /// 1. Simple method names (Action, Action&lt;UIElement&gt;, EventHandler)
    /// 2. Method calls with arguments: name(this, 'string', 123, true)
    /// </summary>
    public static Delegate? ResolveEventAction(string expression, object? model, UIElement element)
    {
        if (model == null || string.IsNullOrWhiteSpace(expression)) return null;

        expression = expression.Trim();

        // Check if it's a method call with parentheses
        var openParen = expression.IndexOf('(');
        var closeParen = expression.LastIndexOf(')');

        if (openParen > 0 && closeParen > openParen)
        {
            var methodName = expression.Substring(0, openParen).Trim();
            var argsStr = expression.Substring(openParen + 1, closeParen - openParen - 1).Trim();
            var args = ParseArguments(argsStr, element);

            return ResolveMethodWithArguments(methodName, model, args);
        }

        return null;
    }

    private static object?[] ParseArguments(string argsStr, UIElement element)
    {
        if (string.IsNullOrWhiteSpace(argsStr)) return [];

        // Simple comma-separated split (doesn't handle commas inside strings for now, but good enough for this spec)
        var parts = argsStr.Split(',');
        var result = new object?[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i].Trim();
            if (p.Equals("this", StringComparison.OrdinalIgnoreCase))
            {
                result[i] = element;
            }
            else if ((p.StartsWith('\'') && p.EndsWith('\'')) || (p.StartsWith('"') && p.EndsWith('"')))
            {
                result[i] = p.Substring(1, p.Length - 2);
            }
            else if (bool.TryParse(p, out var b))
            {
                result[i] = b;
            }
            else if (int.TryParse(p, out var n))
            {
                result[i] = n;
            }
            else if (float.TryParse(p, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f))
            {
                result[i] = f;
            }
            else
            {
                // Unrecognized or potentially a model property (could expand this later)
                result[i] = p;
            }
        }

        return result;
    }

    private static Delegate? ResolveMethodWithArguments(string methodName, object model, object?[] args)
    {
        // 1. Check if model has a property with the name that contains a Delegate
        try
        {
            var prop = model.GetType().GetProperty(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null && typeof(Delegate).IsAssignableFrom(prop.PropertyType))
            {
                if (prop.GetValue(model) is Delegate d)
                {
                    return new Action(() => d.DynamicInvoke(args));
                }
            }
        }
        catch { }

        // 2. Check for methods
        var methods = model.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        var matchingMethods = methods.Where(m => m.Name == methodName).ToList();

        foreach (var method in matchingMethods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == args.Length)
            {
                // Simplified type checking
                bool match = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (args[i] != null && !parameters[i].ParameterType.IsAssignableFrom(args[i]!.GetType()))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return new Action(() => method.Invoke(model, args));
                }
            }
        }

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
