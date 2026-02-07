namespace SUIM;

using System.Reflection;
using System.Collections;
using System.Globalization;

/// <summary>
/// Manages the connection between a Model property and a UI Element property.
/// </summary>
public class PropertyBinding(object model, string modelPropName, UIElement target, string targetPropName)
{
    public object Model { get; } = model;
    public string ModelPropName { get; } = modelPropName;
    public PropertyInfo? ModelProperty { get; } = model is IDictionary ? null : model.GetType().GetProperty(modelPropName.Replace("-", ""), BindingFlags.IgnoreCase)
            ?? throw new Exception($"SUIM Binding Error: Property '{modelPropName}' not found on Model {model.GetType().Name}");
    public object TargetElement { get; } = target;
    public PropertyInfo TargetProperty { get; } = target.GetType().GetProperty(targetPropName,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            ?? throw new Exception($"SUIM Binding Error: Property '{targetPropName}' not found on Element {target.GetType().Name}");

    public void Apply()
    {
        object? rawValue;
        if (Model is IDictionary dict)
        {
            if (!dict.Contains(ModelPropName))
                throw new Exception($"SUIM Binding Error: Key '{ModelPropName}' not found on model dictionary");

            rawValue = dict[ModelPropName];
        }
        else
        {
            rawValue = ModelProperty!.GetValue(Model);
        }

        var targetType = TargetProperty.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        object? converted = ConvertForTarget(rawValue, underlyingType);

        TargetProperty.SetValue(TargetElement, converted);
    }

    private static object? ConvertForTarget(object? value, Type targetType)
    {
        if (value == null)
            return null;

        if (targetType == typeof(string))
            return value.ToString();

        var valueType = value.GetType();
        if (targetType.IsAssignableFrom(valueType))
            return value;

        if (value is string s)
        {
            if (targetType.IsEnum)
                return Enum.Parse(targetType, s, true);

            if (targetType == typeof(bool))
                return bool.Parse(s);

            if (targetType == typeof(int))
                return int.Parse(s, CultureInfo.InvariantCulture);

            if (targetType == typeof(long))
                return long.Parse(s, CultureInfo.InvariantCulture);

            if (targetType == typeof(float))
                return float.Parse(s, CultureInfo.InvariantCulture);

            if (targetType == typeof(double))
                return double.Parse(s, CultureInfo.InvariantCulture);

            if (targetType == typeof(decimal))
                return decimal.Parse(s, CultureInfo.InvariantCulture);

            // Fallback to ChangeType which handles other IConvertible targets
            return Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
        }

        if (value is IConvertible)
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        return value;
    }
}
