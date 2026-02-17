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

    public void Expand(object? model = null, Dictionary<string, Dictionary<string, string>>? inheritedStyles = null, string? basePath = null)
    {
        if (string.IsNullOrEmpty(Source)) return;

        string finalPath = Source;
        if (!Path.IsPathRooted(finalPath) && !string.IsNullOrEmpty(basePath))
        {
            // If it's a relative path, try to find it in the components folder of the project
            finalPath = Path.Combine(basePath, "components", Source);
            if (!File.Exists(finalPath))
            {
                // Fallback to just relative to basePath
                finalPath = Path.Combine(basePath, Source);
            }
        }

        if (!File.Exists(finalPath))
        {
            throw new FileNotFoundException($"SUIM markup file not found: {finalPath}");
        }

        string markup = File.ReadAllText(finalPath);
        var (element, _) = MarkupParser.Parse(markup, model, inheritedStyles, basePath);
        
        // Transfer children from the parsed root to this component
        ClearChildren();
        AddChild(element, null);
    }
}
