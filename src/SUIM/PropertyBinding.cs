namespace SUIM;

using System.Reflection;

/// <summary>
/// Manages the connection between a Model property and a UI Element property.
/// </summary>
public class PropertyBinding(object model, string modelPropName, object target, string targetPropName)
{
    public object Model { get; } = model;
    public PropertyInfo ModelProperty { get; } = model.GetType().GetProperty(modelPropName)
            ?? throw new Exception($"SUIM Binding Error: Property '{modelPropName}' not found on Model {model.GetType().Name}");
    public object TargetElement { get; } = target;
    public PropertyInfo TargetProperty { get; } = target.GetType().GetProperty(targetPropName,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            ?? throw new Exception($"SUIM Binding Error: Property '{targetPropName}' not found on Element {target.GetType().Name}");

    public void Apply()
    {
        var value = ModelProperty.GetValue(Model);
        TargetProperty.SetValue(TargetElement, value);
    }
}
