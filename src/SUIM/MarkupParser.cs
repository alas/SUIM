namespace SUIM;

using System;
using System.Collections.Generic;
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

        Dictionary<string, Dictionary<string, string>>? styles = null;

        // If root element is "suim", extract the model, styles, and actual root element
        if (root.Name.LocalName.Equals("suim", StringComparison.OrdinalIgnoreCase))
        {
            var modelJson = ExtractModelFromSuimWrapper(root);
            if (!string.IsNullOrEmpty(modelJson))
            {
                model = MergeModels(model, modelJson);
            }

            var styleContent = ExtractStylesFromSuimWrapper(root);
            if (!string.IsNullOrEmpty(styleContent))
            {
                styles = ParseStyles(styleContent);
            }

            root = ExtractRealRootFromSuimWrapper(root);
        }

        var element = ParseElement(root);

        // Apply styles after parsing the tree
        if (styles != null && styles.Count > 0)
        {
            element = ApplyStylesToElement(element, styles);
        }

        return (element, model);
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

    private static string? ExtractStylesFromSuimWrapper(XElement suimElement)
    {
        var styleElement = suimElement.Elements()
            .FirstOrDefault(e => e.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase));
        
        if (styleElement == null)
        {
            return null;
        }

        // Get the content of the style element
        var content = styleElement.Value.Trim();
        return string.IsNullOrEmpty(content) ? null : content;
    }

    private static Dictionary<string, Dictionary<string, string>> ParseStyles(string styleContent)
    {
        var styles = new Dictionary<string, Dictionary<string, string>>();
        
        // Simple CSS-like parser
        // Format: .classname { property: value; property: value; }
        var classRegex = new System.Text.RegularExpressions.Regex(@"\.([a-zA-Z0-9_-]+)\s*\{([^}]*)\}");
        var matches = classRegex.Matches(styleContent);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var className = match.Groups[1].Value;
            var propertiesContent = match.Groups[2].Value;

            var properties = new Dictionary<string, string>();
            // Parse properties: "property: value, property: value"
            var propertyRegex = new System.Text.RegularExpressions.Regex(@"([a-zA-Z0-9\-]+)\s*:\s*([^,}]+)");
            var propMatches = propertyRegex.Matches(propertiesContent);

            foreach (System.Text.RegularExpressions.Match propMatch in propMatches)
            {
                var propName = propMatch.Groups[1].Value.Trim();
                var propValue = propMatch.Groups[2].Value.Trim();
                properties[propName] = propValue;
            }

            if (properties.Count > 0)
            {
                styles[className] = properties;
            }
        }

        return styles;
    }

    private static UIElement ApplyStylesToElement(UIElement element, Dictionary<string, Dictionary<string, string>> styles)
    {
        // Get the element's class
        var elementClass = element.GetAttribute("class") as string;
        
        // Check if this element has a matching style
        if (!string.IsNullOrEmpty(elementClass))
        {
            var classToCheck = elementClass.Trim();
            if (styles.TryGetValue(classToCheck, out var properties))
            {
                element = ApplyStylePropertiesToElement(element, properties, styles);
                return element;
            }
        }

        // Recursively apply to children
        return ApplyStylesToChildren(element, styles);
    }

    private static UIElement ApplyStylePropertiesToElement(UIElement element, Dictionary<string, string> properties, Dictionary<string, Dictionary<string, string>> allStyles)
    {
        // Extract border and scroll attributes for special handling
        string? borderAttr = null;
        string? scrollAttr = null;
        var regularAttrs = new Dictionary<string, string>();

        foreach (var kvp in properties)
        {
            var propName = kvp.Key;
            var propValue = kvp.Value;

            if (propName.Equals("border", StringComparison.OrdinalIgnoreCase))
            {
                borderAttr = propValue;
            }
            else if (propName.Equals("scroll", StringComparison.OrdinalIgnoreCase))
            {
                scrollAttr = propValue;
            }
            else
            {
                regularAttrs[propName] = propValue;
            }
        }

        // Apply regular attributes
        foreach (var kvp in regularAttrs)
        {
            element.SetAttribute(kvp.Key, kvp.Value);
        }

        // First, apply styles to children of the original element
        ApplyStylesToChildren(element, allStyles);

        // Handle scroll wrapper
        if (!string.IsNullOrEmpty(scrollAttr))
        {
            var scroll = new Scroll();
            if (Enum.TryParse<ScrollDirection>(scrollAttr, true, out var dir))
            {
                scroll.Direction = dir;
            }
            scroll.AddChild(element, null);
            element = scroll;
        }

        // Handle border wrapper (must be applied last to wrap scroll if present)
        if (!string.IsNullOrEmpty(borderAttr))
        {
            var border = new Border();
            border.SetAttribute("border", borderAttr);
            border.AddChild(element, null);
            element = border;
        }

        return element;
    }

    private static UIElement ApplyStylesToChildren(UIElement element, Dictionary<string, Dictionary<string, string>> styles)
    {
        // For elements that have children, recursively apply styles
        switch (element)
        {
            case Div:
            case Stack:
            case Button:
            case Overlay:
            case Border:
            case Scroll:
                for (int i = 0; i < element.Children.Count; i++)
                {
                    element.Children[i] = ApplyStylesToElement(element.Children[i], styles);
                }
                break;
            case Grid grid:
                foreach (var gridChild in grid.GridChildren)
                {
                    gridChild.Element = ApplyStylesToElement(gridChild.Element, styles);
                }
                break;
            case Dock dock:
                var newDockChildren = new List<DockChild>();
                foreach (var dockChild in dock.DockChildren)
                {
                    var styledElement = ApplyStylesToElement(dockChild.Element, styles);
                    newDockChildren.Add(new DockChild(dockChild.Edge, styledElement));
                }
                dock.DockChildren.Clear();
                foreach (var dockChild in newDockChildren)
                {
                    dock.DockChildren.Add(dockChild);
                }
                break;
        }

        return element;
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

        if (visualElements.Count != 1)
        {
            throw new InvalidOperationException("suim element must contain at least one visual tree element after model and style");
        }

        // Return the last visual element as the root
        return visualElements.Single();
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
            var name = attr.Name.LocalName;
            if (name.Equals("scroll", StringComparison.OrdinalIgnoreCase) || name.Equals("border", StringComparison.OrdinalIgnoreCase)) continue;

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

    private static readonly HashSet<string> LayoutAttributeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "width", "height", "padding", "margin",
        "halign", "horizontalalignment", "valign", "verticalalignment",
        "visibility", "opacity", "background", "bg", "class",
        "x", "y", "z-index", "anchor"
    };

    private static bool IsLayoutAttribute(string name)
    {
        return LayoutAttributeNames.Contains(name);
    }

    private static UIElement ParseElementTag(XElement element)
    {
        var tag = element.Name.LocalName;

        if (tag.Equals("div", StringComparison.OrdinalIgnoreCase)) return new Div();
        if (tag.Equals("stack", StringComparison.OrdinalIgnoreCase)) return new Stack();
        if (tag.Equals("hstack", StringComparison.OrdinalIgnoreCase) || tag.Equals("hbox", StringComparison.OrdinalIgnoreCase)) return new Stack { Orientation = Orientation.Horizontal };
        if (tag.Equals("vstack", StringComparison.OrdinalIgnoreCase) || tag.Equals("vbox", StringComparison.OrdinalIgnoreCase)) return new Stack { Orientation = Orientation.Vertical };
        if (tag.Equals("grid", StringComparison.OrdinalIgnoreCase)) return new Grid();
        if (tag.Equals("dock", StringComparison.OrdinalIgnoreCase)) return new Dock();
        if (tag.Equals("overlay", StringComparison.OrdinalIgnoreCase)) return new Overlay();
        if (tag.Equals("label", StringComparison.OrdinalIgnoreCase)) return new Label();
        if (tag.Equals("button", StringComparison.OrdinalIgnoreCase)) return new Button();
        if (tag.Equals("image", StringComparison.OrdinalIgnoreCase)) return new Image();
        if (tag.Equals("input", StringComparison.OrdinalIgnoreCase)) return new Input();
        if (tag.Equals("select", StringComparison.OrdinalIgnoreCase)) return new Select();
        if (tag.Equals("option", StringComparison.OrdinalIgnoreCase)) return new Option();
        if (tag.Equals("textarea", StringComparison.OrdinalIgnoreCase)) return new TextArea();
        if (tag.Equals("border", StringComparison.OrdinalIgnoreCase)) return new Border();

        throw new NotSupportedException($"Unknown tag: {element.Name.LocalName}");
    }
}