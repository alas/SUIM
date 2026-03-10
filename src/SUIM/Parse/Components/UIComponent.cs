namespace SUIM.Parse.Components;

using SUIM.Parse;

/// <summary>
/// Base class for SUIM Components with Code Behind
/// </summary>
/// <param name="tagName"></param>
public abstract class UIComponent(string tagName) : VirtualComponent(tagName)
{
    public override UIElement? Expand(object? parentModel = null, Dictionary<string, Dictionary<string, string>>? inheritedStyles = null, string? basePath = null)
    {
        BindEventsRecursive(this);
        return base.Expand(parentModel, inheritedStyles, basePath);
    }

    public void BindEventsToTree(UIElement root)
    {
        BindEventsRecursive(root);
    }

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
