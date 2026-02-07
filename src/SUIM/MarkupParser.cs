namespace SUIM;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Xml.Linq;
using SUIM.Components;

public class MarkupParser(object? model = null)
{
    private dynamic? model = model == null ? null : Create(model);

    public (UIElement, dynamic?) Parse(string markup)
    {
        var controlFlowParser = new ControlFlowParser(model);
        var expandedMarkup = controlFlowParser.ExpandDirectives(markup);

        var doc = XDocument.Parse(expandedMarkup);
        var root = doc.Root!;

        // If root element is "suim", extract the model and actual root element
        if (root.Name.LocalName.Equals("suim", StringComparison.OrdinalIgnoreCase))
        {
            var modelJson = ExtractModelFromSuimWrapper(root);
            if (!string.IsNullOrEmpty(modelJson))
            {
                model = MergeModels(model, modelJson);
            }
            root = ExtractRealRootFromSuimWrapper(root);
        }

        return (ParseElement(root), model);
    }

    private dynamic? MergeModels(dynamic? existingModel, string modelJson)
    {
        try
        {
            // Parse JSON into a dictionary
            var jsonObject = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(modelJson);
            if (jsonObject == null)
            {
                return existingModel;
            }

            // Convert JsonElement objects to standard .NET types
            var modelDict = ConvertJsonElementDictionary(jsonObject);

            // If no existing model, create from JSON
            if (existingModel == null)
            {
                return CreateDynamicFromDictionary(modelDict);
            }

            // Merge: extract properties from existing model, then update with JSON values
            var mergedDict = ExtractPropertiesAsDictionary(existingModel);
            foreach (var kvp in modelDict)
            {
                mergedDict[kvp.Key] = kvp.Value;
            }

            return CreateDynamicFromDictionary(mergedDict);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse model JSON: {ex.Message}", ex);
        }
    }

    private Dictionary<string, object?> ConvertJsonElementDictionary(Dictionary<string, JsonElement> jsonObject)
    {
        var result = new Dictionary<string, object?>();
        foreach (var kvp in jsonObject)
        {
            result[kvp.Key] = ConvertJsonElement(kvp.Value);
        }
        return result;
    }

    private object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Object => ConvertJsonElementDictionary(
                element.EnumerateObject().ToDictionary(p => p.Name, p => p.Value)
            ),
            _ => null
        };
    }

    private Dictionary<string, object?> ExtractPropertiesAsDictionary(dynamic? model)
    {
        var dict = new Dictionary<string, object?>();
        if (model == null)
        {
            return dict;
        }

        // If it's an ObservableObject, try to extract its properties
        if (model is ObservableObject oo)
        {
            var modelType = model.GetType();
            var propertiesField = modelType.GetField("_properties", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (propertiesField?.GetValue(model) is Dictionary<string, object?> properties)
            {
                return new Dictionary<string, object?>(properties);
            }
        }

        // Otherwise, extract using reflection
        foreach (var prop in model.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (prop.CanRead)
            {
                dict[prop.Name] = prop.GetValue(model);
            }
        }

        return dict;
    }

    private dynamic CreateDynamicFromDictionary(Dictionary<string, object?> dict)
    {
        var observable = new ObservableObject();
        // Set properties directly into the observable
        foreach (var kvp in dict)
        {
            observable.SetValue(kvp.Key, kvp.Value);
        }
        return observable;
    }

    private static string? ExtractModelFromSuimWrapper(XElement suimElement)
    {
        var modelElement = suimElement.Elements()
            .FirstOrDefault(e => e.Name.LocalName.Equals("model", StringComparison.OrdinalIgnoreCase));
        
        if (modelElement == null)
        {
            return null;
        }

        // Get the content of the model element
        var content = modelElement.Value.Trim();
        return string.IsNullOrEmpty(content) ? null : content;
    }

    private static XElement ExtractRealRootFromSuimWrapper(XElement suimElement)
    {
        var children = suimElement.Elements().ToList();
        
        if (children.Count == 0)
        {
            throw new InvalidOperationException("suim element must contain at least one child element (the visual tree root)");
        }

        // Filter out "model" and "style" elements
        var visualElements = children
            .Where(e => !e.Name.LocalName.Equals("model", StringComparison.OrdinalIgnoreCase) &&
                       !e.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (visualElements.Count == 0)
        {
            throw new InvalidOperationException("suim element must contain at least one visual tree element after model and style");
        }

        // Return the last visual element as the root
        return visualElements.Last();
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