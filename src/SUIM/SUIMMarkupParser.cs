namespace SUIM;

using System.Xml.Linq;

public class SUIMMarkupParser(Dictionary<string, object> model)
{
    public UIElement Parse(string markup)
    {
        var controlFlowParser = new SUIMControlFlowParser(model);
        var expandedMarkup = controlFlowParser.ExpandDirectives(markup);

        var doc = XDocument.Parse(expandedMarkup);
        return ParseElement(doc.Root!, model);
    }

    private static UIElement ParseElement(XElement element, Dictionary<string, object> model)
    {
        UIElement uiElement = ParseElementTag(element);

        // Parse common attributes
        foreach (var attr in element.Attributes())
        {

            if (attr.Value.StartsWith("@"))
            {
                // Dynamic Binding: <grid width="@myVar" />
                string modelPropName = attr.Value.Substring(1);
                var binding = new PropertyBinding(model, modelPropName, element, attr.Name.LocalName);
                uiElement.Bindings.Add(binding);
                binding.Apply();
            }
            else
            {
                SetAttribute(uiElement, attr.Name.LocalName, attr.Value);
            }
        }

        foreach (var child in element.Elements())
        {
            var childElement = ParseElement(child, model);
            uiElement.AddChild(childElement, child);
        }

        return uiElement;
    }

    private static UIElement ParseElementTag(XElement element)
    {
        return element.Name.LocalName switch
        {
            "div" => new Div(),
            "stack" => new Stack(),
            "hstack" or "hbox" => new Stack { Orientation = Orientation.Horizontal },
            "vstack" or "vbox" => new Stack { Orientation = Orientation.Vertical },
            "grid" => new Grid(),
            "dock" => new Dock(),
            "overlay" => new Overlay(),
            "label" => new Label(),
            "button" => new Button(),
            _ => throw new NotSupportedException($"Unknown tag: {element.Name.LocalName}")
        };
    }

    private static void SetAttribute(UIElement element, string name, string value)
    {
        switch (name)
        {
            case "id":
                element.Id = value;
                break;
            case "halign" or "horizontalalignment":
                element.HorizontalAlignment = Enum.Parse<HorizontalAlignment>(value, true);
                break;
            case "valign" or "verticalalignment":
                element.VerticalAlignment = Enum.Parse<VerticalAlignment>(value, true);
                break;
            case "margin":
                element.Margin = int.Parse(value);
                break;
            case "padding":
                element.Padding = int.Parse(value);
                break;
            case "bg" or "background":
                element.Background = value;
                break;
            case "width":
                element.Width = int.Parse(value);
                break;
            case "height":
                element.Height = int.Parse(value);
                break;
            case "anchor":
                element.Anchor = Enum.Parse<Anchor>(value, true);
                break;
            default:
                // Specific attributes
                if (element is Stack stack)
                {
                    switch (name)
                    {
                        case "orientation":
                            stack.Orientation = Enum.Parse<Orientation>(value, true);
                            break;
                        case "spacing":
                            stack.Spacing = int.Parse(value);
                            break;
                    }
                }
                else if (element is Grid grid)
                {
                    switch (name)
                    {
                        case "columns":
                            grid.Columns = value;
                            break;
                        case "rows":
                            grid.Rows = value;
                            break;
                    }
                }
                else if (element is Dock dock)
                {
                    switch (name)
                    {
                        case "lastchildfill":
                            dock.LastChildFill = bool.Parse(value);
                            break;
                    }
                }
                else if (element is BaseText text)
                {
                    switch (name)
                    {
                        case "text":
                            text.Text = value;
                            break;
                    }
                }
                break;
        }
    }
}