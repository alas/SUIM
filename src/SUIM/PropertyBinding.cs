namespace SUIM;

using System.ComponentModel;

/// <summary>
/// Manages the connection between a Model property and a UI Element property.
/// </summary>
public class PropertyBinding : IDisposable
{
    public ObservableObject Model { get; }
    public string ModelPropName { get; }
    public UIElement TargetElement { get; }
    public string TargetPropertyName { get; }

    public PropertyBinding(ObservableObject model, string modelPropName, UIElement target, string targetPropName)
    {
        Model = model;
        ModelPropName = modelPropName;
        TargetElement = target;
        TargetPropertyName = targetPropName;

        Model.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == ModelPropName)
        {
            Apply();
        }
    }

    public void Apply()
    {
        object? rawValue = Model.GetValue(ModelPropName);

        TargetElement.SetAttribute(TargetPropertyName, rawValue);
    }

    public void Dispose()
    {
        if (Model is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged -= OnPropertyChanged;
        }
        GC.SuppressFinalize(this);
    }
}
