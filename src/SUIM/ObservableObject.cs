namespace SUIM;

using System.ComponentModel;
using System.Dynamic;
using System.Reflection;

public class ObservableObject : DynamicObject, INotifyPropertyChanged
{
    private readonly Dictionary<string, object?> _properties = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Initialize(object model)
    {
        if (model == null) return;

        foreach (var prop in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead)
            {
                _properties[prop.Name] = prop.GetValue(model);
            }
        }
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        return _properties.TryGetValue(binder.Name, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        if (_properties.TryGetValue(binder.Name, out var existingValue) && Equals(existingValue, value))
        {
            return true;
        }

        _properties[binder.Name] = value;
        OnPropertyChanged(binder.Name);
        return true;
    }

    public object? GetValue(string propertyName)
    {
        _properties.TryGetValue(propertyName, out var value);
        return value;
    }

    public void SetValue(string propertyName, object? value)
    {
        if (_properties.TryGetValue(propertyName, out var existingValue) && Equals(existingValue, value))
        {
            return;
        }

        _properties[propertyName] = value;
        OnPropertyChanged(propertyName);
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
