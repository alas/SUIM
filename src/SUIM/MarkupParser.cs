namespace SUIM;

using System;
using System.Xml.Linq;
using SUIM.Components;

public class MarkupParser(object? model = null)
{
    private readonly dynamic? model = model == null ? null : Create(model);

    public (UIElement, dynamic?) Parse(string markup)
    {
        var controlFlowParser = new ControlFlowParser(model);
        var expandedMarkup = controlFlowParser.ExpandDirectives(markup);

        var doc = XDocument.Parse(expandedMarkup);
        return (ParseElement(doc.Root!), model);
    }
    
    private static dynamic Create(object model)
    {
        if (model is ObservableObject oo) return oo;

        var observable = new ObservableObject();
        observable.Initialize(model);
        return observable;
    }

    private UIElement ParseElement(XElement element)
    {
        var innerElement = ParseElementTag(element);
        var rootElement = innerElement;

        var attributes = element.Attributes().ToList();
        var scrollAttr = attributes.FirstOrDefault(a => a.Name.LocalName.Equals("scroll", StringComparison.OrdinalIgnoreCase));
        var borderAttr = attributes.FirstOrDefault(a => a.Name.LocalName.Equals("border", StringComparison.OrdinalIgnoreCase));

        if (scrollAttr != null)
        {
            var scroll = new Scroll();
            if (Enum.TryParse<ScrollDirection>(scrollAttr.Value, true, out var dir))
            {
                scroll.Direction = dir;
            }
            scroll.AddChild(rootElement, element);
            rootElement = scroll;
        }

        if (borderAttr != null)
        {
            var border = new Border();
            border.SetAttribute("border", borderAttr.Value);
            border.AddChild(rootElement, element);
            rootElement = border;
        }

        foreach (var attr in attributes)
        {
            var name = attr.Name.LocalName.ToLower();
            if (name == "scroll" || name == "border") continue;

            var target = IsLayoutAttribute(name) ? rootElement : innerElement;

            if (attr.Value.StartsWith('@'))
            {
                // Dynamic Binding: <grid width="@myVar" />
                string modelPropName = attr.Value.Substring(1);
                var binding = new PropertyBinding(model, modelPropName, target, name);
                target.Bindings.Add(binding);
                binding.Apply();
            }
            else
            {
                target.SetAttribute(name, attr.Value);
            }
        }

        // Handle both text nodes and element children
        // Use innerElement for children as it is the content container
        if (innerElement is Grid grid)
        {
            int rowIndex = 0;
            int columnIndex = 0;
            foreach (var node in element.Elements())
            {
                if (node.Name.LocalName.Equals("row", StringComparison.OrdinalIgnoreCase))
                {
                    var heightAttr = node.Attribute("height");
                    if (heightAttr != null)
                    {
                        grid.Rows = string.IsNullOrEmpty(grid.Rows) ? heightAttr.Value : grid.Rows + ", " + heightAttr.Value;
                    }

                    int colIdx = 0;
                    foreach (var child in node.Elements())
                    {
                        child.SetAttributeValue("grid.row", rowIndex.ToString());
                        child.SetAttributeValue("grid.column", colIdx.ToString());
                        var childElement = ParseElement(child);
                        grid.AddChild(childElement, child);
                        colIdx++;
                    }

                    rowIndex++;
                }
                else if (node.Name.LocalName.Equals("column", StringComparison.OrdinalIgnoreCase))
                {
                    var widthAttr = node.Attribute("width");
                    if (widthAttr != null)
                    {
                        grid.Columns = string.IsNullOrEmpty(grid.Columns) ? widthAttr.Value : grid.Columns + ", " + widthAttr.Value;
                    }

                    int rowIdx = 0;
                    foreach (var child in node.Elements())
                    {
                        child.SetAttributeValue("grid.column", columnIndex.ToString());
                        child.SetAttributeValue("grid.row", rowIdx.ToString());
                        var childElement = ParseElement(child);
                        grid.AddChild(childElement, child);
                        rowIdx++;
                    }

                    columnIndex++;
                }
                else
                {
                    var childElement = ParseElement(node);
                    grid.AddChild(childElement, node);
                }
            }
        }
        else
        {
            foreach (var node in element.Nodes())
            {
                if (node is XText textNode)
                {
                    var text = textNode.Value.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        var textElement = new Label { Text = text };
                        innerElement.AddChild(textElement, element);
                    }
                }
                else if (node is XElement childXElement)
                {
                    var childElement = ParseElement(childXElement);
                    innerElement.AddChild(childElement, childXElement);
                }
            }
        }

        return rootElement;
    }

    private static bool IsLayoutAttribute(string name)
    {
        return name switch
        {
            "id" or "width" or "height" or "padding" or "margin" or
            "halign" or "horizontalalignment" or "valign" or "verticalalignment" or
            "visibility" or "opacity" or "background" or "bg" or "class" or
            "x" or "y" or "z-index" or "anchor" => true,
            _ => false
        };
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
            "border" => new Border(),
            _ => throw new NotSupportedException($"Unknown tag: {element.Name.LocalName}")
        };
    }
}