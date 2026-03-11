namespace SUIM.Parse.Components;

using System;
using System.IO;
using SUIM.Model;
using SUIM.Parse;

/// <summary>
/// A helper class for Tags that will get replaced by markup and can have code behind
/// </summary>
/// <param name="tagName"></param>
public class VirtualComponent(string tagName) : LayoutElement(tagName)
{
    public string? Source { get; set; }
    public Dictionary<string, object?> Attributes { get; } = [];
    public bool IsView { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("source", StringComparison.OrdinalIgnoreCase))
        {
            Source = value as string;
        }
        else if (name.Equals("isview", StringComparison.OrdinalIgnoreCase))
        {
            IsView = value is bool b ? b : Convert.ToBoolean(value);
        }
        else 
        {
            // Preserve as regular attribute for components or custom usage
            Attributes[name] = value;

            try
            {
                base.SetAttribute(name, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting attribute: {ex}");
            }
        }
    }

    public UIElement? Expand(object? parentModel = null, Dictionary<string, Dictionary<string, string>>? inheritedStyles = null, string? basePath = null)
    {
        var source = Source ?? $"{GetType().Name}.suim";

        var finalPath = source;
        var fileExists = File.Exists(finalPath);
        if (!fileExists && !Path.IsPathRooted(finalPath) && !string.IsNullOrEmpty(basePath))
        {
            var crumPath = IsView ? "views" : "components";
            finalPath = Path.Combine(basePath, crumPath, source);
            fileExists = File.Exists(finalPath);
            if (!fileExists)
            {
                finalPath = Path.Combine(basePath, source);
                fileExists = File.Exists(finalPath);
            }
        }

        if (!fileExists) throw new FileNotFoundException($"SUIM markup file not found: {finalPath}");

        var componentName = Path.GetFileNameWithoutExtension(finalPath);
        var markup = File.ReadAllText(finalPath);
        var element = MarkupParser.Parse(markup, null, inheritedStyles, basePath, componentName);
        
        Model = element.Model;

        // Map attributes from this tag to the component model
        if (Model is ObservableObject oo)
        {
            foreach (var attr in Attributes)
            {
                var name = attr.Key;

                if (attr.Value is string val)
                {
                    if (val.StartsWith('@'))
                    {
                        // Binding to parent model property
                        if (parentModel is ObservableObject parentOO)
                        {
                            var parentPropName = val[1..];
                            // Preserve the component's initial value as a fallback when the parent doesn't provide a value yet.
                            var initialValue = oo.GetValue(name);
                            oo.SetProxy(name, () =>
                            {
                                var pv = parentOO.GetValue(parentPropName);
                                var isInvalid_pv = pv == null || (pv is string s && string.IsNullOrEmpty(s));
                                var isInvalid_iv = initialValue == null || (initialValue is string s2 && string.IsNullOrEmpty(s2));
                                return isInvalid_pv && !isInvalid_iv ? initialValue : pv;
                            }, (v) => parentOO.SetValue(parentPropName, v));

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
                    else if (val.Contains('(') && parentModel is ObservableObject parentOO2)
                    {
                        // Attribute looks like a method call expression (e.g. "MyHandler()") coming from parent.
                        // Create a proxy that resolves to a Delegate from the parent model so component internals can invoke it.
                        oo.SetProxy(name, () => parentOO2.GetHandler(val), null);
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

        // Convert any bindings declared on the component tag (e.g. visibility="@PopupVisibility")
        // into proxies on the component model that map to the parent model property.
        if (this.Bindings.Count > 0)
        {
            foreach (var binding in this.Bindings.ToList())
            {
                var compProp = binding.TargetPropertyName;
                var parentProp = binding.ModelPropertyName;

                if (parentModel is ObservableObject parentOO && Model is ObservableObject compOO)
                {
                    // Create a proxy on the component model that forwards to the parent property
                    compOO.SetProxy(compProp, () => parentOO.GetValue(parentProp), (v) => parentOO.SetValue(parentProp, v));

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
                else if (parentModel != null && Model is ObservableObject compOO2)
                {
                    // Parent is a plain object: use reflection-based proxy
                    compOO2.SetProxy(compProp, () => parentModel.GetType().GetProperty(parentProp)?.GetValue(parentModel), (v) =>
                    {
                        var p = parentModel.GetType().GetProperty(parentProp);
                        if (p != null && p.CanWrite) p.SetValue(parentModel, v);
                    });
                }

                // Remove the binding so the mapper doesn't try to bind the component model to the parent property again
                this.Bindings.Remove(binding);
            }
        }

        ClearChildren();
        
        return element;
    }
}
