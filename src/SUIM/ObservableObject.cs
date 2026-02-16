namespace SUIM;

using System.ComponentModel;
using System.Dynamic;
using System.Reflection;

public class ObservableObject : DynamicObject, INotifyPropertyChanged
{
    private readonly Dictionary<string, object?> _properties = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private object? _source;

    public void Initialize(object model)
    {
        if (model == null) return;
        _source = model;

        foreach (var prop in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead)
            {
                _properties[prop.Name] = prop.GetValue(model);
            }
        }
    }

    public Delegate? GetHandler(string name)
    {
        if (_source == null) return null;

        // 1. Check if the property itself is a delegate
        if (_properties.TryGetValue(name, out var val) && val is Delegate d)
        {
            return d;
        }

        // 2. Check for a method on the source object
        var method = _source.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (method != null)
        {
            // Create a delegate. We don't know the exact type, but we can try to create an Action or similar.
            // For now, let's return the method info wrapped or loosely typed if possible, 
            // but the caller expects a Delegate. 
            // We'll try to create an Action or Action<arg> based on parameters.
            
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                return method.CreateDelegate(typeof(Action), _source);
            }
            if (parameters.Length == 1 && typeof(SUIM.Components.UIElement).IsAssignableFrom(parameters[0].ParameterType))
            {
                return method.CreateDelegate(typeof(Action<SUIM.Components.UIElement>), _source);
            }
             if (parameters.Length == 2 && typeof(EventArgs).IsAssignableFrom(parameters[1].ParameterType)) 
             {
                 // Handle standard EventHandler pattern: void Method(object sender, EventArgs e)
                  return method.CreateDelegate(typeof(EventHandler), _source);
             }
        }

        return null;
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
