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

    public void Expand(object? model = null, Dictionary<string, Dictionary<string, string>>? inheritedStyles = null)
    {
        if (string.IsNullOrEmpty(Source)) return;

        string markup;
        if (File.Exists(Source))
        {
            markup = File.ReadAllText(Source);
        }
        else
        {
            // Fallback for relative paths or embedded resources? 
            // For now, let's assume absolute or relative to CWD.
            // If it's a relative path, we might need a search strategy.
            throw new FileNotFoundException($"SUIM markup file not found: {Source}");
        }

        var (element, _) = MarkupParser.Parse(markup, model, inheritedStyles);
        
        // Transfer children from the parsed root to this component
        // Typically a custom component should be replaced by its content, 
        // but here we act as a container that expands into its children.
        ClearChildren();
        AddChild(element, null);
    }
}
