namespace SUIM;

using System;
using System.IO;
using System.Linq;
using SUIM.Parse;
using SUIM.Parse.Components;

public partial class SUIMProject(string rootPath)
{
    public string RootPath { get; } = rootPath;

    public (UIElement, dynamic?) GetView(string viewName, object? model = null)
    {
        string viewPath = Path.Combine(RootPath, "views", $"{viewName}.suim");
        if (!File.Exists(viewPath))
        {
            throw new FileNotFoundException($"View file not found: {viewPath}");
        }

        string markup = File.ReadAllText(viewPath);
        
        // 1. Recursively resolve and register dependencies
        ResolveDependencies(markup);

        // 2. Parse the view
        return MarkupParser.Parse(markup, model, basePath: RootPath);
    }

    public void ResolveDependencies(string markup)
    {
        // Use XDocument to find all tags
        try
        {
            // We need to expand directives first because they might contain tags
            // But for dependency resolution, a simple scan of the raw markup might be enough 
            // if we are looking for anything that looks like a tag.
            // However, to be safe, let's use a regex to find potential tags first, 
            // then verify if they correspond to component files.
            
            var potentialTags = MyRegex().Matches(markup)
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            foreach (var tag in potentialTags)
            {
                // Skip built-in tags and already registered tags
                if (MarkupParser.IsBuiltInTag(tag) || ComponentRegistry.IsRegistered(tag)) continue;

                // Check if a component file exists for this tag
                string componentPath = Path.Combine(RootPath, "components", $"{tag}.suim");
                if (File.Exists(componentPath))
                {
                    // Register the component
                    ComponentRegistry.Register(tag, componentPath);

                    // Recursively resolve dependencies for this component
                    string componentMarkup = File.ReadAllText(componentPath);
                    ResolveDependencies(componentMarkup);
                }
                else
                {
                    Console.WriteLine($"Warning: Tag '{tag}' is not a built-in tag and no component file was found for it.");
                }
            }
        }
        catch (Exception ex)
        {
            // Log or handle error? For now, just continue if parsing fails.
            Console.WriteLine($"Error resolving dependencies: {ex.Message}");
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"<([a-zA-Z0-9_]+)")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}
