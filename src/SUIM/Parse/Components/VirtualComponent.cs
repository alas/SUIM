namespace SUIM.Parse.Components;

using System;
using System.IO;
using SUIM.Binding;
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

    public UIElement? Expand(dynamic parentModel, string? basePath, Dictionary<string, Dictionary<string, string>>? inheritedStyles = null)
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

        // Map attributes and bindings from this tag to the component model
        ComponentBindingMapper.ApplyBindings(this, parentModel);

        ClearChildren();
        
        return element;
    }
}
