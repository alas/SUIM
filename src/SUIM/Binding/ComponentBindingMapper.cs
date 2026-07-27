namespace SUIM.Binding;

using SUIM.Model;
using SUIM.Parse.Components;
using System.Linq;

public static class ComponentBindingMapper
{
    public static void ApplyBindings(VirtualComponent component, dynamic parentModel, object? codeBehind = null)
    {
        ApplyAttributeBindings(component, parentModel, codeBehind);
        ApplyExplicitBindings(component, parentModel, codeBehind);
    }

    private static void ApplyAttributeBindings(VirtualComponent component, dynamic parentModel, object? codeBehind = null)
    {
        if (component.Model is not ObservableObject oo) return;

        foreach (var attr in component.Attributes)
        {
            var name = attr.Key;

            if (attr.Value is string val)
            {
                if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                    && component.ResolvedEvents.TryGetValue(name[2..].ToLowerInvariant(), out var resolvedHandler))
                {
                    oo.SetValue(name, resolvedHandler);
                }
                else if (BindingExpression.TryGetModelPropertyName(val, out var parentPropName))
                {
                    // Binding to parent model property
                    if (parentModel is ObservableObject parentOO)
                    {
                        // Preserve the component's initial value as a fallback when the parent doesn't provide a value yet.
                        var initialValue = oo.GetValue(name);
                        oo.SetProxy(name, () =>
                        {
                            var pv = parentOO.GetValue(parentPropName);
                            var isInvalidPv = pv == null || (pv is string s && string.IsNullOrEmpty(s));
                            var isInvalidIv = initialValue == null || (initialValue is string s2 && string.IsNullOrEmpty(s2));
                            return isInvalidPv && !isInvalidIv ? initialValue : pv;
                        }, v => parentOO.SetValue(parentPropName, v));

                        // When parent changes, notify the component model so bindings inside the component update.
                        parentOO.PropertyChanged += (s, e) =>
                        {
                            try
                            {
                                if (e.PropertyName == parentPropName)
                                {
                                    oo.NotifyChanged(name);
                                }
                            }
                            catch { }
                        };
                    }
                    else
                    {
                        oo.SetValue(name, attr.Value);
                    }
                }
                else if (val.Contains('('))
                {
                    var handler = EventHandlerResolver.ResolveHandler(codeBehind, val, component);
                    if (handler != null)
                    {
                        oo.SetValue(name, handler);
                    }
                    else if (parentModel is ObservableObject parentOO2)
                    {
                        // Attribute looks like a method call expression (e.g. "MyHandler()") coming from parent.
                        // Create a proxy that resolves to a Delegate from the parent model so component internals can invoke it.
                        oo.SetValue(name, parentOO2.GetHandler(val));
                    }
                }
                else
                {
                    oo.SetValue(name, attr.Value);
                }
            }
            else
            {
                oo.SetValue(name, attr.Value);
            }
        }
    }

    private static void ApplyExplicitBindings(VirtualComponent component, dynamic parentModel, object? codeBehind = null)
    {
        if (component.Bindings.Count == 0) return;

        foreach (var binding in component.Bindings.ToList())
        {
            var compProp = binding.TargetPropertyName;
            var parentProp = binding.ModelPropertyName;

            if (parentModel is ObservableObject parentOO && component.Model is ObservableObject compOO)
            {
                // Create a proxy on the component model that forwards to the parent property
                compOO.SetProxy(compProp, () => parentOO.GetValue(parentProp), v => parentOO.SetValue(parentProp, v));

                // When parent changes, notify the component model so internal bindings update
                parentOO.PropertyChanged += (s, e) =>
                {
                    try
                    {
                        if (e.PropertyName == parentProp)
                        {
                            compOO.NotifyChanged(compProp);
                        }
                    }
                    catch { }
                };
            }
            else if (component.Model is ObservableObject compOO2)
            {
                // Parent is a plain object: use reflection-based proxy
                compOO2.SetProxy(compProp, () => parentModel.GetType().GetProperty(parentProp)?.GetValue(parentModel), v =>
                {
                    var p = parentModel.GetType().GetProperty(parentProp);
                    if (p != null && p.CanWrite) p.SetValue(parentModel, v);
                });
            }

            // Remove the binding so the mapper doesn't try to bind the component model to the parent property again
            component.Bindings.Remove(binding);
        }
    }
}
