namespace SUIM;

using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;

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

    public Delegate? GetHandler(string name)
    {
        if (_source == null) return null;

        if (string.IsNullOrWhiteSpace(name)) return null;

        name = name.Trim();

        // If the name is a call expression like "Func()" or "Func(this, 'a', 1)",
        // extract the method name and arguments.
        string methodName = name;
        string? argsStr = null;
        var open = name.IndexOf('(');
        var close = name.LastIndexOf(')');
        if (open > 0 && close > open)
        {
            methodName = name.Substring(0, open).Trim();
            argsStr = name.Substring(open + 1, close - open - 1).Trim();
        }

        // 1. Check if the property itself is a delegate (use methodName without parentheses)
        if (_properties.TryGetValue(methodName, out var val) && val is Delegate pd)
        {
            if (argsStr == null)
                return pd;

            // If arguments provided, return an Action that will invoke the delegate with parsed args
            var args = ParseArguments(argsStr);
            return new Action(() => pd.DynamicInvoke(args));
        }

        // 2. Check for methods with matching name (multiple methods with same name = overloading)
        var methods = _source.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
        var matchingMethods = methods.Where(m => m.Name == methodName).ToList();

        if (matchingMethods.Count == 0)
            return null;

        // If explicit arguments were provided in the expression, try to resolve by matching parameter count/types
        if (argsStr != null)
        {
            var args = ParseArguments(argsStr);

            foreach (var method in matchingMethods)
            {
                var parms = method.GetParameters();
                if (parms.Length != args.Length) continue;

                bool match = true;
                for (int i = 0; i < parms.Length; i++)
                {
                    var a = args[i];
                    if (a == null) continue; // allow null for reference types
                    if (!parms[i].ParameterType.IsAssignableFrom(a.GetType()))
                    {
                        match = false;
                        break;
                    }
                }

                if (!match) continue;

                // Return an Action that invokes the method with the parsed arguments
                return new Action(() => method.Invoke(_source, args));
            }

            return null;
        }

        // No explicit arguments: use priority-based resolution for overloaded methods
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

    private object?[] ParseArguments(string argsStr)
    {
        if (string.IsNullOrWhiteSpace(argsStr)) return [];

        var parts = argsStr.Split(',');
        var result = new object?[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i].Trim();
            if (p.Equals("this", System.StringComparison.OrdinalIgnoreCase))
            {
                // ObservableObject has no UIElement context; map to null
                result[i] = null;
            }
            else if ((p.StartsWith("'") && p.EndsWith("'")) || (p.StartsWith("\"") && p.EndsWith("\"")))
            {
                result[i] = p.Substring(1, p.Length - 2);
            }
            else if (bool.TryParse(p, out var b))
            {
                result[i] = b;
            }
            else if (int.TryParse(p, out var n))
            {
                result[i] = n;
            }
            else if (float.TryParse(p, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f))
            {
                result[i] = f;
            }
            else
            {
                result[i] = p;
            }
        }

        return result;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (_proxies.TryGetValue(binder.Name, out var proxy))
        {
            result = proxy.Getter();
            return true;
        }
        return _properties.TryGetValue(binder.Name, out result);
    }

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

    public object? GetValue(string propertyName)
    {
        if (_proxies.TryGetValue(propertyName, out var proxy))
        {
            return proxy.Getter();
        }
        _properties.TryGetValue(propertyName, out var value);
        return value;
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
