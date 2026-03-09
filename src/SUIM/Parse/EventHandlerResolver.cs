namespace SUIM.Parse;

using System;
using System.Linq;
using System.Reflection;
using SUIM.Parse.Components;

public static class EventHandlerResolver
{
    public static Delegate? ResolveHandler(object target, string expression, UIElement? context = null)
    {
        if (target == null || string.IsNullOrWhiteSpace(expression)) return null;

        expression = expression.Trim();
        if (expression.StartsWith('@')) expression = expression[1..];

        // Extract method name and arguments
        string methodName = expression;
        string? argsStr = null;
        var open = expression.IndexOf('(');
        var close = expression.LastIndexOf(')');
        
        if (open > 0 && close > open)
        {
            methodName = expression[..open].Trim();
            argsStr = expression.Substring(open + 1, close - open - 1).Trim();
        }

        // 1. Check for properties that are delegates
        var prop = target.GetType().GetProperty(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop != null && typeof(Delegate).IsAssignableFrom(prop.PropertyType))
        {
            var pd = prop.GetValue(target) as Delegate;
            if (pd != null)
            {
                if (argsStr == null) return pd;
                var args = ParseArguments(argsStr, context);
                return new Action(() => pd.DynamicInvoke(args));
            }
        }

        // 2. Check for ObservableObject properties (special case since it's dynamic)
        if (target is SUIM.Model.ObservableObject oo)
        {
            var val = oo.GetValue(methodName);
            if (val is Delegate pd)
            {
                if (argsStr == null) return pd;
                var args = ParseArguments(argsStr, context);
                return new Action(() => pd.DynamicInvoke(args));
            }
        }

        // 3. Find matching methods
        var methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase);
        var matchingMethods = methods.Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase)).ToList();

        if (matchingMethods.Count == 0) return null;

        // If explicit arguments were provided
        if (argsStr != null)
        {
            var args = ParseArguments(argsStr, context);

            foreach (var method in matchingMethods)
            {
                var parms = method.GetParameters();
                if (parms.Length != args.Length) continue;

                bool match = true;
                for (int i = 0; i < parms.Length; i++)
                {
                    var a = args[i];
                    if (a == null)
                    {
                        if (parms[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(parms[i].ParameterType) == null)
                        {
                            match = false;
                            break;
                        }
                        continue;
                    }
                    if (!parms[i].ParameterType.IsAssignableFrom(a.GetType()))
                    {
                        match = false;
                        break;
                    }
                }

                if (!match) continue;

                // Return an Action that invokes the method with the parsed arguments
                return new Action(() => method.Invoke(target, args));
            }

            return null;
        }

        // No explicit arguments: use priority-based resolution
        // 1. Parameterless method
        var parameterlessMethod = matchingMethods.FirstOrDefault(m => m.GetParameters().Length == 0);
        if (parameterlessMethod != null)
        {
            return CreateDelegate(parameterlessMethod, target, typeof(Action));
        }

        // 2. Method taking single UIElement (if context is available)
        var uiElementMethod = matchingMethods.FirstOrDefault(m =>
            m.GetParameters().Length == 1 &&
            typeof(UIElement).IsAssignableFrom(m.GetParameters()[0].ParameterType));
        if (uiElementMethod != null && context != null)
        {
            return new Action(() => uiElementMethod.Invoke(target, [context]));
        }

        // 3. EventHandler pattern
        var eventHandlerMethod = matchingMethods.FirstOrDefault(m =>
        {
            var parms = m.GetParameters();
            return parms.Length == 2 &&
                parms[0].ParameterType == typeof(object) &&
                typeof(EventArgs).IsAssignableFrom(parms[1].ParameterType);
        });
        if (eventHandlerMethod != null)
        {
            return CreateDelegate(eventHandlerMethod, target, typeof(EventHandler));
        }

        // 4. Single parameter Action<object?>
        var singleParamMethod = matchingMethods.FirstOrDefault(m => m.GetParameters().Length == 1);
        if (singleParamMethod != null)
        {
            return new Action<object?>(p => singleParamMethod.Invoke(target, [p]));
        }

        return null;
    }

    private static Delegate? CreateDelegate(MethodInfo method, object? target, Type delegateType)
    {
        try
        {
            return Delegate.CreateDelegate(delegateType, target, method);
        }
        catch
        {
            return null;
        }
    }

    public static object?[] ParseArguments(string argsStr, UIElement? context = null)
    {
        if (string.IsNullOrWhiteSpace(argsStr)) return [];

        // Simple comma split (doesn't handle commas inside strings or nested parens, 
        // but sufficient for basic SUIM usage as seen in existing codebase)
        var parts = argsStr.Split(',');
        var result = new object?[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i].Trim();
            if (p.Equals("this", StringComparison.OrdinalIgnoreCase))
            {
                result[i] = context;
            }
            else if ((p.StartsWith('\'') && p.EndsWith('\'')) || (p.StartsWith('"') && p.EndsWith('"')))
            {
                result[i] = p[1..^1];
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
                result[i] = p; // Fallback to raw string if not literal
            }
        }

        return result;
    }
}
