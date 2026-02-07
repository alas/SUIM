namespace SUIM;

using System;
using System.Xml.Linq;
using SUIM.Components;

public static class MarkupParser
{
    public static (UIElement, dynamic?) Parse(string markup, object? model = null)
    {
        var observableModel = model == null ? null : Create(model);
        var controlFlowParser = new ControlFlowParser(observableModel);
        var expandedMarkup = controlFlowParser.ExpandDirectives(markup);

        var doc = XDocument.Parse(expandedMarkup);
        return (ParseElement(doc.Root!, observableModel), observableModel);
    }
    
    private static dynamic Create(object model)
    {
        if (model is ObservableObject oo) return oo;

        var observable = new ObservableObject();
        observable.Initialize(model);
        return observable;
    }

    private static UIElement ParseElement(XElement element, dynamic model)
    {
        UIElement uiElement = ParseElementTag(element);

        // Parse common attributes
        foreach (var attr in element.Attributes())
        {
            if (attr.Value.StartsWith('@'))
            {
                // Dynamic Binding: <grid width="@myVar" />
                string modelPropName = attr.Value.Substring(1);
                var binding = new PropertyBinding(model, modelPropName, uiElement, attr.Name.LocalName);
                uiElement.Bindings.Add(binding);
                binding.Apply();
            }
            else
            {
                uiElement.SetAttribute(attr.Name.LocalName.ToLower(), attr.Value);
            }
        }

        // Handle both text nodes and element children
        if (uiElement is Grid grid)
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
                        var childElement = ParseElement(child, model);
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
                        var childElement = ParseElement(child, model);
                        grid.AddChild(childElement, child);
                        rowIdx++;
                    }

                    columnIndex++;
                }
                else
                {
                    var childElement = ParseElement(node, model);
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
                        uiElement.AddChild(textElement, element);
                    }
                }
                else if (node is XElement childXElement)
                {
                    var childElement = ParseElement(childXElement, model);
                    uiElement.AddChild(childElement, childXElement);
                }
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
}