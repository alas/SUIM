namespace SUIM.Components;

using System;
using System.IO;

public class CustomComponent(string tagName) : UIElement(tagName)
{
    public string? Source { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("source", StringComparison.OrdinalIgnoreCase))
        {
            Source = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public void Expand(object? parentModel = null, Dictionary<string, Dictionary<string, string>>? inheritedStyles = null, string? basePath = null)
    {
        if (string.IsNullOrEmpty(Source)) return;

        var finalPath = Source;
        var fileExists = File.Exists(finalPath);
        if (!fileExists && !Path.IsPathRooted(finalPath) && !string.IsNullOrEmpty(basePath))
        {
            finalPath = Path.Combine(basePath, "components", Source);
            fileExists = fileExists || File.Exists(finalPath);
            if (!File.Exists(finalPath))
            {
                finalPath = Path.Combine(basePath, Source);
                fileExists = fileExists || File.Exists(finalPath);
            }
        }

        if (!fileExists) throw new FileNotFoundException($"SUIM markup file not found: {finalPath}");

        string componentName = Path.GetFileNameWithoutExtension(finalPath);
        string markup = File.ReadAllText(finalPath);
        var (element, componentModel) = MarkupParser.Parse(markup, null, inheritedStyles, basePath, componentName);
        
        this.Model = componentModel;
        this.IsComponentRoot = true;

        // Map attributes from this tag to the component model
        if (componentModel is ObservableObject oo)
        {
            foreach (var attr in Attributes)
            {
                var name = attr.Key;

                if (attr.Value is string val && val.StartsWith('@'))
                {
                    // Binding to parent model
                    if (parentModel is ObservableObject parentOO)
                    {
                        var parentPropName = val.Substring(1);
                        oo.SetProxy(name, () => parentOO.GetValue(parentPropName), (v) => parentOO.SetValue(parentPropName, v));
                    }
                }
                else
                {
                    oo.SetValue(name, attr.Value);
                }
            }
        }

        ClearChildren();
        AddChild(element, null);
    }
}
