namespace SUIM.Binding;

using System;
using System.ComponentModel;
using SUIM.Model;
using SUIM.Parse.Components;

public static class BindingEngine
{
    public static void ApplyBindings(UIElement suimElement, object backendElement, IBindingAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(suimElement);
        ArgumentNullException.ThrowIfNull(backendElement);
        ArgumentNullException.ThrowIfNull(adapter);

        TransferBindings(suimElement, backendElement, adapter);
        TransferEvents(suimElement, backendElement, adapter);
    }

    private static void TransferBindings(UIElement suimElement, object backendElement, IBindingAdapter adapter)
    {
        if (suimElement.Bindings.Count == 0) return;

        var model = suimElement.GetEffectiveModel();

        foreach (var binding in suimElement.Bindings)
        {
            if (model == null)
            {
                if (suimElement.IsComponentRoot || suimElement.Parent?.GetEffectiveModel() == null)
                    throw new InvalidOperationException($"Binding '{binding.ModelPropertyName}' found on tag '{suimElement.TagName}' but no model context is available.");
                continue;
            }

            if (adapter.TryBindTwoWay(suimElement, backendElement, binding.TargetPropertyName, model, binding.ModelPropertyName))
            {
                continue;
            }

            SetupPropertyBinding(model, binding.ModelPropertyName, (Action<object?>)((newValue) =>
            {
                if (!adapter.TryApplyValue(suimElement, backendElement, binding.TargetPropertyName, newValue))
                {
                    Console.WriteLine($"Binding target not supported: {binding.TargetPropertyName} on {backendElement.GetType().Name}");
                }
            }));
        }
    }

    private static void TransferEvents(UIElement suimElement, object backendElement, IBindingAdapter adapter)
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
                adapter.TryBindEvent(suimElement, backendElement, eventName, resolvedHandler);
                continue;
            }

            if (model == null)
                throw new InvalidOperationException($"Event '{eventName}' found on tag '{suimElement.TagName}' but no model context is available.");

            Delegate? handler = null;
            if (!string.IsNullOrWhiteSpace(handlerName))
            {
                handler = EventHandlerResolver.ResolveHandler(model, handlerName, suimElement, adapter.EventOptions);

                if (handler == null && model is ObservableObject oo)
                {
                    handler = oo.GetHandler(handlerName);
                }
            }

            if (handler != null)
            {
                suimElement.ResolvedEvents[eventKey] = handler;
                adapter.TryBindEvent(suimElement, backendElement, eventName, handler);
            }
        }
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
