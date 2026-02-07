namespace SUIM;

using System.Reflection;
using System.Collections;

/// <summary>
/// Manages the connection between a Model property and a UI Element property.
/// </summary>
public class PropertyBinding(object model, string modelPropName, UIElement target, string targetPropName)
{
    public object Model { get; } = model;
    public string ModelPropName { get; } = modelPropName;
    public PropertyInfo? ModelProperty { get; } = model is IDictionary ? null : model.GetType().GetProperty(modelPropName.Replace("-", ""), BindingFlags.IgnoreCase)
            ?? throw new Exception($"SUIM Binding Error: Property '{modelPropName}' not found on Model {model.GetType().Name}");
    public UIElement TargetElement { get; } = target;
    public string TargetPropertyName { get; } = targetPropName;

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

        TargetElement.SetAttribute(TargetPropertyName, rawValue);
    }
}
