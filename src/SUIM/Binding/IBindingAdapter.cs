namespace SUIM.Binding;

using SUIM.Parse.Components;

public interface IBindingAdapter
{
    EventBindingOptions EventOptions { get; }

    bool TryBindTwoWay(UIElement suimElement, object backendElement, string targetPropertyName, object model, string modelPropertyName);

    bool TryApplyValue(UIElement suimElement, object backendElement, string targetPropertyName, object? value);

    bool TryBindEvent(UIElement suimElement, object backendElement, string eventName, Delegate handler);
}

public sealed class EventBindingOptions
{
    public Type[] ExtraEventArgsTypes { get; init; } = [];
}
