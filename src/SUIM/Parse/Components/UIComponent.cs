namespace SUIM.Parse.Components;

using System;
using SUIM.Parse;

public abstract class UIComponent(string tagName) : UIElement(tagName)
{
    public void LoadMarkup(string markup, object? model = null)
    {
        var (root, componentModel) = MarkupParser.Parse(markup, model);
        this.Model = componentModel;
        
        // Add all children from the parsed root to this component
        foreach (var child in root.Children.ToArray())
        {
            root.RemoveChild(child);
            this.AddChild(child, null);
        }

        BindEventsRecursive(this);
        InitializeComponent();
    }

    public void LoadMarkupFromFile(string path, object? model = null)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Markup file not found: {path}");
        LoadMarkup(File.ReadAllText(path), model);
    }

    public T? FindElement<T>(string id) where T : UIElement
    {
        return FindElementRecursive<T>(this, id);
    }

    private T? FindElementRecursive<T>(UIElement element, string id) where T : UIElement
    {
        if (string.Equals(element.Id, id, StringComparison.OrdinalIgnoreCase) && element is T t) return t;
        
        foreach (var child in element.Children)
        {
            var found = FindElementRecursive<T>(child, id);
            if (found != null) return found;
        }
        
        return null;
    }

    protected virtual void InitializeComponent() { }

    private void BindEventsRecursive(UIElement element)
    {
        foreach (var evt in element.Events)
        {
            var eventName = evt.Key.ToLowerInvariant();
            var expression = evt.Value;
            
            var handler = EventHandlerResolver.ResolveHandler(this, expression, element);
            if (handler != null)
            {
                element.ResolvedEvents[eventName] = handler;
            }
        }

        foreach (var child in element.Children)
        {
            BindEventsRecursive(child);
        }
    }
}
