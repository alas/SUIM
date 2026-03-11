namespace SUIM.Binding;

using SUIM.Parse.Components;

public static class BindingExpression
{
    public static void ApplyAttribute(UIElement target, string name, string value)
    {
        var isBinding = IsBindingValue(value);
        if (!isBinding)
        {
            target.SetAttribute(name, value);
        }

        if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            target.Events[name[2..]] = value;
            return;
        }

        if (isBinding)
        {
            TryAddBinding(target, name, value);
        }
    }

    public static bool IsBindingValue(string? value)
    {
        return !string.IsNullOrEmpty(value) && value!.Length > 1 && value[0] == '@' && value[1] != '@';
    }

    public static bool TryGetModelPropertyName(string? value, out string modelPropertyName)
    {
        if (IsBindingValue(value))
        {
            modelPropertyName = value![1..];
            return true;
        }

        modelPropertyName = string.Empty;
        return false;
    }

    public static bool TryAddBinding(UIElement element, string targetPropertyName, string? value)
    {
        if (!TryGetModelPropertyName(value, out var modelPropertyName)) return false;

        element.Bindings.Add(new BindingDefinition(targetPropertyName, modelPropertyName));
        return true;
    }
}
