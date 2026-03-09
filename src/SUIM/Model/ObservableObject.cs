namespace SUIM.Model;

using System.ComponentModel;
using System.Dynamic;
using System.Reflection;
using SUIM.Parse;

public class ObservableObject : DynamicObject, INotifyPropertyChanged
{
    private readonly Dictionary<string, object?> _properties = [];
    private readonly Dictionary<string, (Func<object?> Getter, Action<object?>? Setter)> _proxies = [];

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

    /// <summary>
    /// Used for binding, dont delete
    /// </summary>
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (_proxies.TryGetValue(binder.Name, out var proxy))
        {
            result = proxy.Getter();
            return true;
        }
        return _properties.TryGetValue(binder.Name, out result);
    }

    /// <summary>
    /// Used for binding, dont delete
    /// </summary>
    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        if (_proxies.TryGetValue(binder.Name, out var proxy))
        {
            if (proxy.Setter != null)
            {
                proxy.Setter(value);
                OnPropertyChanged(binder.Name);
                return true;
            }
            return false; // Read-only proxy
        }

        if (_properties.TryGetValue(binder.Name, out var existingValue) && Equals(existingValue, value))
        {
            return true;
        }

        _properties[binder.Name] = value;
        OnPropertyChanged(binder.Name);
        return true;
    }

    public Delegate? GetHandler(string name)
    {
        if (_source == null) return null;
        return EventHandlerResolver.ResolveHandler(_source, name);
    }

    public void SetValue(string propertyName, object? value)
    {
        if (_proxies.TryGetValue(propertyName, out var proxy))
        {
            if (proxy.Setter != null)
            {
                proxy.Setter(value);
                OnPropertyChanged(propertyName);
            }
            return;
        }

        if (_properties.TryGetValue(propertyName, out var existingValue) && Equals(existingValue, value))
        {
            return;
        }

        _properties[propertyName] = value;
        OnPropertyChanged(propertyName);
    }

    public object? GetValue(string propertyName)
    {
        if (_proxies.TryGetValue(propertyName, out var proxy))
        {
            return proxy.Getter();
        }
        _properties.TryGetValue(propertyName, out var value);
        return value;
    }

    public void SetProxy(string propertyName, Func<object?> getter, Action<object?>? setter = null)
    {
        _proxies[propertyName] = (getter, setter);
    }

    public void NotifyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
