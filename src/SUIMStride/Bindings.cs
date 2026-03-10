namespace SUIMStride;

using System.Data;
using Stride.UI;
using Stride.UI.Controls;
using StrideButton = Stride.UI.Controls.Button;
using StrideUIElement = Stride.UI.UIElement;
using SUIM;
using SUIM.Model;
using SUIMElement = SUIM.Parse.Components.UIElement;

internal static class Bindings
{
    public static void TransferBindings(SUIMElement suimElement, StrideUIElement strideElement)
    {
        var model = suimElement.GetEffectiveModel();

        foreach (var binding in suimElement.Bindings)
        {
            if (model == null)
            {
                if (suimElement.IsComponentRoot || suimElement.Parent?.GetEffectiveModel() == null)
                    throw new InvalidOperationException($"Binding '{binding.ModelPropertyName}' found on tag '{suimElement.TagName}' but no model context is available.");
                continue;
            }
            SetupBinding(model, binding.ModelPropertyName, binding.TargetPropertyName, strideElement);
        }

        TransferEvents(suimElement, strideElement);
    }

    private static void TransferEvents(SUIMElement suimElement, StrideUIElement strideElement)
    {
        if (suimElement.Events.Count == 0) return;
        var model = suimElement.GetEffectiveModel();

        foreach (var kvp in suimElement.Events)
        {
            var eventName = kvp.Key;
            var handlerName = kvp.Value;
            var eventKey = eventName.ToLowerInvariant();

            // Prefer resolved handlers (e.g., component code-behind) when available
            if (suimElement.ResolvedEvents.TryGetValue(eventKey, out var resolvedHandler))
            {
                if (string.Equals(eventName, "click", StringComparison.OrdinalIgnoreCase) && strideElement is StrideButton resolvedBtn)
                {
                    BindClickHandler(resolvedBtn, resolvedHandler, suimElement);
                }
                continue;
            }

            if (model == null)
                throw new InvalidOperationException($"Event '{eventName}' found on tag '{suimElement.TagName}' but no model context is available.");

            // Resolve handler using the effective model for this element (components must be isolated)
            Delegate? handler = null;
            if (!string.IsNullOrWhiteSpace(handlerName))
            {
                // Unified inline handler resolution (supports @prop, method names, and calls with args)
                handler = SUIM.Parse.EventHandlerResolver.ResolveHandler(model, handlerName, suimElement);

                if (handler == null && model is ObservableObject oo)
                {
                    // Fallback to code-behind methods on the source object
                    handler = oo.GetHandler(handlerName);
                }

                // If generic resolver didn't find anything, try Stride-specific resolver that understands RoutedEventArgs
                handler ??= ResolveMethodAsDelegate(handlerName, model);
            }

            if (handler != null)
            {
                suimElement.ResolvedEvents[eventKey] = handler;
                // Map to Stride event
                if (string.Equals(eventName, "click", StringComparison.OrdinalIgnoreCase) && strideElement is StrideButton btn)
                {
                    BindClickHandler(btn, handler, suimElement);
                }
                // Add more event types here as needed
            }
        }
    }

    private static void BindClickHandler(StrideButton btn, Delegate handler, SUIMElement suimElement)
    {
        // Support multiple handler types for click events
        if (handler is EventHandler<Stride.UI.Events.RoutedEventArgs> routedHandler)
        {
            btn.Click += routedHandler;
        }
        else if (handler is EventHandler eh)
        {
            EventHandler<Stride.UI.Events.RoutedEventArgs> wrappedHandler = (s, e) => eh(s, e);
            btn.Click += wrappedHandler;
        }
        else if (handler is Action<SUIMElement> actionWithElement)
        {
            EventHandler<Stride.UI.Events.RoutedEventArgs> wrappedHandler = (s, e) => actionWithElement(suimElement);
            btn.Click += wrappedHandler;
        }
        else if (handler is Action a)
        {
            EventHandler<Stride.UI.Events.RoutedEventArgs> wrappedHandler = (s, e) => a();
            btn.Click += wrappedHandler;
        }
    }

    /// <summary>
    /// Resolves a method name to a delegate using priority-based resolution.
    /// Priority: Parameterless -> UIElement parameter -> EventHandler pattern
    /// </summary>
    private static Delegate? ResolveMethodAsDelegate(string methodName, dynamic model)
    {
        // Keep a Stride-specific variant that supports RoutedEventArgs in addition to the generic helper
        if (model == null) return null;

        // Cast to object to avoid dynamic dispatch issues
        object targetObject = model;
        var methods = targetObject.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var matchingMethods = methods.Where(m => m.Name == methodName).ToList();
        if (matchingMethods.Count == 0)
            return null;

        // Try EventHandler<RoutedEventArgs> pattern (Stride specific)
        var routedHandlerMethod = matchingMethods.FirstOrDefault(m =>
        {
            var parms = m.GetParameters();
            return parms.Length == 2 &&
                parms[0].ParameterType == typeof(object) &&
                parms[1].ParameterType == typeof(Stride.UI.Events.RoutedEventArgs);
        });
        if (routedHandlerMethod != null)
        {
            try { return Delegate.CreateDelegate(typeof(EventHandler<Stride.UI.Events.RoutedEventArgs>), targetObject, routedHandlerMethod); }
            catch { /* Fall through */ }
        }

        return null;
    }

    private static void SetupBinding(dynamic? model, string modelPropertyName, string targetPropertyName, StrideUIElement strideElement)
    {
        if (model == null) return;

        // 2-way: UI -> model (via proxy)
        if (model is ObservableObject oo)
        {
            if (strideElement is EditText et && (targetPropertyName.Equals("text", StringComparison.OrdinalIgnoreCase) || targetPropertyName.Equals("value", StringComparison.OrdinalIgnoreCase)))
            {
                oo.SetProxy(modelPropertyName, () => et.Text, (val) => et.Text = val?.ToString() ?? "");
                et.TextChanged += (s, e) => oo.NotifyChanged(modelPropertyName);
                return; // Proxy handles everything
            }
            else if (strideElement is ToggleButton tb && (targetPropertyName.Equals("checked", StringComparison.OrdinalIgnoreCase) || targetPropertyName.Equals("value", StringComparison.OrdinalIgnoreCase)))
            {
                oo.SetProxy(modelPropertyName,
                    () => tb.State == ToggleState.Checked,
                    (val) => tb.State = (val is bool b && b) ? ToggleState.Checked : ToggleState.UnChecked);

                tb.Checked += (s, e) => oo.NotifyChanged(modelPropertyName);
                tb.Unchecked += (s, e) => oo.NotifyChanged(modelPropertyName);
                return; // Proxy handles everything
            }
        }

        // 1-way fallback (model -> UI)
        BackendHelpers.SetupPropertyBinding((object?)model, modelPropertyName, newValue => ApplyBindingValue(strideElement, targetPropertyName, newValue));
    }

    private static void ApplyBindingValue(StrideUIElement strideElement, string targetPropertyName, object? value)
    {
        try
        {
            // Handle Text property
            if (string.Equals(targetPropertyName, "text", StringComparison.OrdinalIgnoreCase) || string.Equals(targetPropertyName, "value", StringComparison.OrdinalIgnoreCase))
            {
                if (strideElement is TextBlock tb)
                    tb.Text = value?.ToString() ?? "";
                else if (strideElement is EditText et)
                    et.Text = value?.ToString() ?? "";
                else if (strideElement is StrideButton btn && btn.Content is TextBlock btnText)
                    btnText.Text = value?.ToString() ?? "";
                else
                {
                    var ex = new NotSupportedException("Element does not support 'text' binding.");
                    ex.Data["Type"] = strideElement.GetType().Name;
                    ex.Data["Data"] = value;
                    throw ex;
                }
            }
            // Handle other common properties
            else if (string.Equals(targetPropertyName, "visibility", StringComparison.OrdinalIgnoreCase))
            {
                if (value != null)
                {
                    if (Enum.TryParse<Visibility>(value.ToString(), true, out var vis))
                    {
                        strideElement.Visibility = vis;
                    }
                    else
                    {
                        var ex = new DataException("Could not parse 'visibility' binding.");
                        ex.Data["Type"] = strideElement.GetType().Name;
                        ex.Data["Data"] = value;
                        throw ex;
                    }
                }
            }
            else if (string.Equals(targetPropertyName, "opacity", StringComparison.OrdinalIgnoreCase))
            {
                if (float.TryParse(value?.ToString() ?? "1", out var opacity))
                    strideElement.Opacity = opacity;
                else
                {
                    var ex = new DataException("Could not parse 'opacity' binding.");
                    ex.Data["Type"] = strideElement.GetType().Name;
                    ex.Data["Data"] = value;
                    throw ex;
                }
            }
            else
            {
                throw new NotSupportedException($"targetPropertyName:{targetPropertyName} value:{value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not set {targetPropertyName} to {value}: {ex}");
        }
    }
}
