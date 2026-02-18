namespace SUIM;

using System.ComponentModel;
using System.Dynamic;
using System.Linq;
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

        // 2. Check for methods with matching name (multiple methods with same name = overloading)
        var methods = _source.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.IgnoreReturn);
        var matchingMethods = methods.Where(m => m.Name == name).ToList();

        if (matchingMethods.Count == 0)
            return null;

        // Priority-based resolution for overloaded methods:
        // 1. Parameterless method (Action)
        var parameterlessMethod = matchingMethods.FirstOrDefault(m => m.GetParameters().Length == 0);
        if (parameterlessMethod != null)
        {
            return parameterlessMethod.CreateDelegate<Action>(_source);
        }

        // 2. Method taking single UIElement (Action<UIElement>)
        var uiElementMethod = matchingMethods.FirstOrDefault(m =>
            m.GetParameters().Length == 1 &&
            typeof(Components.UIElement).IsAssignableFrom(m.GetParameters()[0].ParameterType));
        if (uiElementMethod != null)
        {
            return uiElementMethod.CreateDelegate<Action<Components.UIElement>>(_source);
        }

        // 3. EventHandler pattern (object sender, EventArgs e)
        var eventHandlerMethod = matchingMethods.FirstOrDefault(m =>
        {
            var parms = m.GetParameters();
            return parms.Length == 2 &&
                parms[0].ParameterType == typeof(object) &&
                typeof(EventArgs).IsAssignableFrom(parms[1].ParameterType);
        });
        if (eventHandlerMethod != null)
        {
            return eventHandlerMethod.CreateDelegate<EventHandler>(_source);
        }

        // 4. Fall back to first method if others don't match
        if (matchingMethods.Count > 0)
        {
            var fallbackMethod = matchingMethods[0];
            var parameters = fallbackMethod.GetParameters();
            
            try
            {
                if (parameters.Length == 0)
                    return fallbackMethod.CreateDelegate<Action>(_source);
                else if (parameters.Length == 1 && typeof(Components.UIElement).IsAssignableFrom(parameters[0].ParameterType))
                    return fallbackMethod.CreateDelegate<Action<Components.UIElement>>(_source);
                else if (parameters.Length == 2)
                    return fallbackMethod.CreateDelegate<EventHandler>(_source);
            }
            catch
            {
                // If delegate creation fails, return null
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
