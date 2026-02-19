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

        string finalPath = Source;
        if (!Path.IsPathRooted(finalPath) && !string.IsNullOrEmpty(basePath))
        {
            finalPath = Path.Combine(basePath, "components", Source);
            if (!File.Exists(finalPath)) finalPath = Path.Combine(basePath, Source);
        }

        if (!File.Exists(finalPath)) throw new FileNotFoundException($"SUIM markup file not found: {finalPath}");

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
                var val = attr.Value as string;

                if (val != null && val.StartsWith('@'))
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
