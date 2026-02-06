namespace SUIM;

using System.Diagnostics.CodeAnalysis;
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
            if (attr.Value.StartsWith('@'))
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

        // Handle both text nodes and element children
        foreach (var node in element.Nodes())
        {
            if (node is XText textNode)
            {
                var text = textNode.Value.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    var textElement = new Label { Text = text };
                    uiElement.AddChild(textElement, element);
                }
            }
            else if (node is XElement childXElement)
            {
                var childElement = ParseElement(childXElement, model);
                uiElement.AddChild(childElement, childXElement);
            }
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
            "image" => new Image(),
            "input" => new Input(),
            "select" => new Select(),
            "option" => new Option(),
            "textarea" => new TextArea(),
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
                element.Margin = value;
                break;
            case "padding":
                element.Padding = value;
                break;
            case "bg" or "background":
                element.Background = value;
                break;
            case "width":
                element.Width = value;
                break;
            case "height":
                element.Height = value;
                break;
            case "anchor":
                element.Anchor = Enum.Parse<Anchor>(value, true);
                break;
            case "class":
                element.Class = value;
                break;
            case "placeholder":
                (element as IPlaceholder)?.Placeholder = value;
                break;
            case string x when x.StartsWith("on"):
                element.On(x.Substring(2), GetHandler(value));
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
                        case "clip":
                            stack.Clip = bool.Parse(value);
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
                else if (element is LayoutElement le)
                {
                    switch (name)
                    {
                        case "spacing":
                            le.Spacing = int.Parse(value);
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
                        case "font":
                            text.Font = value;
                            break;
                        case "fontsize":
                            text.FontSize = int.Parse(value);
                            break;
                        case "color":
                            text.Color = value;
                            break;
                        case "wrap":
                            text.Wrap = bool.Parse(value);
                            break;
                    }
                }
                else if (element is Input input)
                {
                    switch (name)
                    {
                        case "type":
                            input.Type = Enum.Parse<InputType>(value, true);
                            break;
                        case "value":
                            input.Value = value;
                            break;
                        case "min":
                            input.Min = int.Parse(value);
                            break;
                        case "max":
                            input.Max = int.Parse(value);
                            break;
                        case "step":
                            input.Step = int.Parse(value);
                            break;
                        case "mask":
                            input.Mask = value;
                            break;
                    }
                }
                else if (element is Image image)
                {
                    switch (name)
                    {
                        case "source":
                            image.Source = value;
                            break;
                        case "stretch":
                            image.Stretch = Enum.Parse<ImageStretch>(value, true);
                            break;
                    }
                }
                else if (element is Select select)
                {
                    switch (name)
                    {
                        case "multiple":
                            select.Multiple = bool.Parse(value);
                            break;
                    }
                }
                else if (element is Option option)
                {
                    switch (name)
                    {
                        case "value":
                            option.Value = value;
                            break;
                    }
                }
                else if (element is TextArea textarea)
                {
                    switch (name)
                    {
                        case "rows":
                            textarea.Rows = int.Parse(value);
                            break;
                        case "columns":
                            textarea.Columns = int.Parse(value);
                            break;
                    }
                }
                break;
        }
    }

    private static Action<UIElement>? GetHandler(string value)
    {
        //throw new NotImplementedException();
        return null;
    }
}