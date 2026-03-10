namespace SUIM.Parse.Components;

using SUIM.Parse;

/// <summary>
/// Base class for SUIM Components with Code Behind
/// </summary>
/// <param name="tagName"></param>
public abstract class UIComponent(string tagName) : UIElement(tagName)
{
    public void LoadViewOrComponent(string root, bool isView, string name)
    {
        var path = Path.Combine(root, isView ? "views" : "components", $"{name}.suim");

        if (!File.Exists(path)) throw new FileNotFoundException($"Markup file not found: {path}");
        LoadMarkup(File.ReadAllText(path));
    }

    public void LoadMarkup(string markup)
    {
        var (root, componentModel) = MarkupParser.Parse(markup);
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
}
