namespace SUIM.Model;

using System;
using System.Linq;
using System.Reflection;
using SUIM.Parse;
using SUIM.Parse.Components;

public static class EventHandlerResolver
{
    public static void ResolveComponentEvents(VirtualComponent component, object? codeBehind)
    {
        BindEventsRecursive(component, codeBehind);
        if (codeBehind == null || component.Events.Count == 0) return;

        foreach (var evt in component.Events)
        {
            var eventName = evt.Key.ToLowerInvariant();
            var expression = evt.Value;
            var handler = ResolveHandler(codeBehind, expression, component);
            if (handler != null)
            {
                component.ResolvedEvents[eventName] = handler;
            }
        }
    }

    public static void BindEventsRecursive(UIElement element, object? target)
    {
        foreach (var evt in element.Events)
        {
            var eventName = evt.Key.ToLowerInvariant();
            var expression = evt.Value;

            var handler = ResolveHandler(target, expression, element);
            if (handler != null)
            {
                element.ResolvedEvents[eventName] = handler;
            }
        }

        foreach (var child in element.Children)
        {
            BindEventsRecursive(child, target);
        }
    }

    public static Delegate? ResolveHandler(object? target, string expression, UIElement? context = null, Binding.EventBindingOptions? options = null)
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
            if (prop.GetValue(target) is Delegate pd)
            {
                if (argsStr == null) return pd;
                var args = ParseArguments(argsStr, context);
                return new Action(() => pd.DynamicInvoke(args));
            }
        }

        // 2. Check for ObservableObject properties (special case since it's dynamic)
        if (target is ObservableObject oo)
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
            return parms.Length == 2
                && parms[0].ParameterType == typeof(object)
                && typeof(EventArgs).IsAssignableFrom(parms[1].ParameterType);
        });
        if (eventHandlerMethod != null)
        {
            return CreateDelegate(eventHandlerMethod, target, typeof(EventHandler));
        }

        // 3b. Extra EventArgs patterns (backend-specific)
        if (options?.ExtraEventArgsTypes != null)
        {
            foreach (var argsType in options.ExtraEventArgsTypes)
            {
                var extraArgsMethod = matchingMethods.FirstOrDefault(m =>
                {
                    var parms = m.GetParameters();
                    return parms.Length == 2
                        && parms[0].ParameterType == typeof(object)
                        && parms[1].ParameterType == argsType;
                });
                if (extraArgsMethod != null)
                {
                    var delegateType = typeof(EventHandler<>).MakeGenericType(argsType);
                    return CreateDelegate(extraArgsMethod, target, delegateType);
                }
            }
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

    private static object?[] ParseArguments(string argsStr, UIElement? context = null)
    {
        if (string.IsNullOrWhiteSpace(argsStr)) return [];

        var parts = SplitArguments(argsStr);
        var result = new object?[parts.Count];

        var scope = new Dictionary<string, object?>();
        if (context != null) scope["this"] = context;
        
        var evaluator = new ExpressionEvaluator([scope]);

        for (int i = 0; i < parts.Count; i++)
        {
            result[i] = evaluator.Evaluate(parts[i]);
        }

        return result;
    }

    private static List<string> SplitArguments(string argsStr)
    {
        var parts = new List<string>();
        int start = 0;
        int balance = 0;
        bool inQuote = false;
        char quoteChar = '\0';

        for (int i = 0; i < argsStr.Length; i++)
        {
            char c = argsStr[i];
            if (!inQuote && (c == '\'' || c == '"'))
            {
                inQuote = true;
                quoteChar = c;
            }
            else if (inQuote && c == quoteChar)
            {
                inQuote = false;
            }
            else if (!inQuote)
            {
                if (c == '(') balance++;
                else if (c == ')') balance--;
                else if (c == ',' && balance == 0)
                {
                    parts.Add(argsStr[start..i].Trim());
                    start = i + 1;
                }
            }
        }
        parts.Add(argsStr[start..].Trim());
        return parts;
    }
}
