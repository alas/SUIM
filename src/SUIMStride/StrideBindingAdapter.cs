namespace SUIMStride;

using System.Data;
using Stride.UI;
using Stride.UI.Controls;
using StrideButton = Stride.UI.Controls.Button;
using StrideUIElement = Stride.UI.UIElement;
using SUIM.Binding;
using SUIM.Model;
using SUIMUIElement = SUIM.Parse.Components.UIElement;

internal sealed class StrideBindingAdapter : IBindingAdapter
{
    public static readonly StrideBindingAdapter Instance = new();

    public EventBindingOptions EventOptions { get; } = new()
    {
        ExtraEventArgsTypes = [typeof(Stride.UI.Events.RoutedEventArgs)]
    };

    public bool TryBindTwoWay(SUIMUIElement suimElement, object backendElement, string targetPropertyName, object model, string modelPropertyName)
    {
        if (model is not ObservableObject oo) return false;
        if (backendElement is not StrideUIElement strideElement) return false;

        if (strideElement is EditText et && (targetPropertyName.Equals("text", StringComparison.OrdinalIgnoreCase) || targetPropertyName.Equals("value", StringComparison.OrdinalIgnoreCase)))
        {
            oo.SetProxy(modelPropertyName, () => et.Text, val => et.Text = val?.ToString() ?? "");
            et.TextChanged += (s, e) => oo.NotifyChanged(modelPropertyName);
            return true;
        }

        if (strideElement is ToggleButton tb && (targetPropertyName.Equals("checked", StringComparison.OrdinalIgnoreCase) || targetPropertyName.Equals("value", StringComparison.OrdinalIgnoreCase)))
        {
            oo.SetProxy(modelPropertyName,
                () => tb.State == ToggleState.Checked,
                val => tb.State = (val is bool b && b) ? ToggleState.Checked : ToggleState.UnChecked);

            tb.Checked += (s, e) => oo.NotifyChanged(modelPropertyName);
            tb.Unchecked += (s, e) => oo.NotifyChanged(modelPropertyName);
            return true;
        }

        return false;
    }

    public bool TryApplyValue(SUIMUIElement suimElement, object backendElement, string targetPropertyName, object? value)
    {
        if (backendElement is not StrideUIElement strideElement) return false;

        if (value is string s && s.Length > 1 && s[0] == '@' && s[1] != '@') return true;

        try
        {
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
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not set {targetPropertyName} to {value}: {ex}");
        }

        return true;
    }

    public bool TryBindEvent(SUIMUIElement suimElement, object backendElement, string eventName, Delegate handler)
    {
        if (backendElement is not StrideUIElement strideElement) return false;

        if (string.Equals(eventName, "click", StringComparison.OrdinalIgnoreCase) && strideElement is StrideButton btn)
        {
            BindClickHandler(btn, handler, suimElement);
            return true;
        }

        return false;
    }

    private static void BindClickHandler(StrideButton btn, Delegate handler, SUIMUIElement suimElement)
    {
        if (handler is EventHandler<Stride.UI.Events.RoutedEventArgs> routedHandler)
        {
            btn.Click += routedHandler;
        }
        else if (handler is EventHandler eh)
        {
            EventHandler<Stride.UI.Events.RoutedEventArgs> wrappedHandler = (s, e) => eh(s, e);
            btn.Click += wrappedHandler;
        }
        else if (handler is Action<SUIMUIElement> actionWithElement)
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
}
