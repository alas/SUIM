namespace SUIM;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SUIM.Components;
using SUIM.Layout;

public static class MarkupParser
{
    public static (UIElement, dynamic?) Parse(string markup, object? model = null)
    {
        dynamic? model2 = model == null ? null : ModelLogic.Create(model);
        var controlFlowParser = new ControlFlowParser(model2);
        var expandedMarkup = controlFlowParser.ExpandDirectives(markup);

        var doc = XDocument.Parse(expandedMarkup);
        var root = doc.Root!;

        Dictionary<string, Dictionary<string, string>> styles = [];

        model2 = ModelLogic.ExtractModel(root, model2);

        var element = ParseElement(root, styles, model2)
            ?? throw new InvalidOperationException("Root element not found.");

        return (element, model2);
    }

    private static void ParseStyles(string styleContent, Dictionary<string, Dictionary<string, string>> styles)
    {        
        // CSS-like parser supporting: .classname, #id, tagname, and *
        // Format: selector { property: value; property: value; }
        var selectorRegex = new System.Text.RegularExpressions.Regex(@"([#.]?[a-zA-Z0-9_*-]+)\s*\{([^}]*)\}");
        var matches = selectorRegex.Matches(styleContent);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var selector = match.Groups[1].Value.Trim();
            var propertiesContent = match.Groups[2].Value;

            var properties = new Dictionary<string, string>();
            // Parse properties: "property: value, property: value"
            var propertyRegex = new System.Text.RegularExpressions.Regex(@"([a-zA-Z0-9\-]+)\s*:\s*([^;}]+)");
            var propMatches = propertyRegex.Matches(propertiesContent);

            foreach (System.Text.RegularExpressions.Match propMatch in propMatches)
            {
                var propName = propMatch.Groups[1].Value.Trim();
                var propValue = propMatch.Groups[2].Value.Trim();
                properties[propName] = propValue;
            }

            if (properties.Count > 0)
            {
                styles[selector] = properties;
            }
        }
    }

    private static UIElement ApplyStylesToElement(UIElement element, Dictionary<string, Dictionary<string, string>> styles, dynamic? model)
    {
        var elementTag = element.GetType().Name.ToLowerInvariant();
        var elementId = element.GetAttribute("id") as string;
        var elementClass = element.GetAttribute("class") as string;
        
        // Merge styles from all matching selectors in order of precedence (low to high)
        var mergedProperties = new Dictionary<string, string>();

        // Universal selector (lowest precedence)
        if (styles.TryGetValue("*", out var universalProps))
        {
            foreach (var kvp in universalProps)
            {
                mergedProperties[kvp.Key] = kvp.Value;
            }
        }

        // Tag selector
        if (styles.TryGetValue(elementTag, out var tagProps))
        {
            foreach (var kvp in tagProps)
            {
                mergedProperties[kvp.Key] = kvp.Value;
            }
        }

        // Class selector(s) - support multiple space-separated classes
        if (!string.IsNullOrEmpty(elementClass))
        {
            var classes = elementClass.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var className in classes)
            {
                var classSelector = "." + className;
                if (styles.TryGetValue(classSelector, out var classProps))
                {
                    foreach (var kvp in classProps)
                    {
                        mergedProperties[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        // ID selector (highest precedence)
        if (!string.IsNullOrEmpty(elementId))
        {
            var idSelector = "#" + elementId.Trim();
            if (styles.TryGetValue(idSelector, out var idProps))
            {
                foreach (var kvp in idProps)
                {
                    mergedProperties[kvp.Key] = kvp.Value;
                }
            }
        }

        if (mergedProperties.Count > 0)
        {
            element = ApplyStylePropertiesToElement(element, mergedProperties, styles, model);
        }
        return element;
    }

    private static UIElement ApplyStylePropertiesToElement(UIElement element, Dictionary<string, string> properties, Dictionary<string, Dictionary<string, string>> allStyles, dynamic? model)
    {
        // Extract border and scroll attributes for special handling
        string? borderAttr = null;
        string? scrollAttr = null;
        var regularAttrs = new Dictionary<string, string>();
        var wrapperAttrs = new Dictionary<string, string>();
        // Pre-check whether this style will create a wrapper so layout attributes can be routed to it.
        bool willWrapWithBorder = properties.Keys.Any(k => k.Equals("border", StringComparison.OrdinalIgnoreCase));
        bool willWrapWithScroll = properties.Keys.Any(k => k.Equals("scroll", StringComparison.OrdinalIgnoreCase));

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
            else if ((willWrapWithBorder || willWrapWithScroll) && IsLayoutAttribute(propName))
            {
                // If a style defines layout attributes for an element that will be wrapped (border/scroll),
                // apply those layout attributes to the wrapper instead of the inner element.
                wrapperAttrs[propName] = propValue;
            }
            else
            {
                regularAttrs[propName] = propValue;
            }
        }

        // Apply regular attributes to the element (inner)
        foreach (var kvp in regularAttrs)
        {
            element.SetAttribute(kvp.Key, kvp.Value);
        }

        // Handle scroll wrapper
        if (!string.IsNullOrEmpty(scrollAttr))
        {
            var scroll = new Scroll();
            if (Enum.TryParse<ScrollDirection>(scrollAttr, true, out var dir))
            {
                scroll.Direction = dir;
            }

            // Apply any layout attributes from the style to the scroll wrapper (width/height etc.)
            foreach (var kvp in wrapperAttrs)
            {
                scroll.SetAttribute(kvp.Key, kvp.Value);
            }
            // Fallback: also apply explicit width/height from properties if present (defensive)
            if (properties.TryGetValue("width", out var w)) scroll.SetAttribute("width", w);
            if (properties.TryGetValue("height", out var h)) scroll.SetAttribute("height", h);
            // Ensure numeric width/height are parsed/applied directly
            if (properties.TryGetValue("width", out var pw) && !string.IsNullOrWhiteSpace(pw)) scroll.Width = UnitValue.Parse(pw);
            if (properties.TryGetValue("height", out var ph) && !string.IsNullOrWhiteSpace(ph)) scroll.Height = UnitValue.Parse(ph);

            // When a style creates a scroll wrapper, the inner element should default to `auto` if it was the structural (1fr) default.
            if (element.Width.Type == UnitType.Fr) element.Width = UnitValue.Auto;
            if (element.Height.Type == UnitType.Fr) element.Height = UnitValue.Auto;

            scroll.AddChild(element, null);
            element = scroll;
        }

        // Handle border wrapper (must be applied last to wrap scroll if present)
        if (!string.IsNullOrEmpty(borderAttr))
        {
            var border = new Border();
            border.SetAttribute("border", borderAttr);

            // Apply any layout attributes from the style to the border wrapper (width/height etc.)
            foreach (var kvp in wrapperAttrs)
            {
                border.SetAttribute(kvp.Key, kvp.Value);
            }
            // Fallback: also apply explicit width/height from properties if present (defensive)
            if (properties.TryGetValue("width", out var w)) border.SetAttribute("width", w);
            if (properties.TryGetValue("height", out var h)) border.SetAttribute("height", h);

            // Ensure numeric width/height in styles are parsed and applied directly (defensive - avoids any SetAttribute parsing quirks)
            if (properties.TryGetValue("width", out var pw) && !string.IsNullOrWhiteSpace(pw))
            {
                border.Width = UnitValue.Parse(pw);
            }
            if (properties.TryGetValue("height", out var ph) && !string.IsNullOrWhiteSpace(ph))
            {
                border.Height = UnitValue.Parse(ph);
            }

            if (element.Width.Type == UnitType.Fr) element.Width = UnitValue.Auto;
            if (element.Height.Type == UnitType.Fr) element.Height = UnitValue.Auto;
            border.AddChild(element, null);
            element = border;
        }

        return element;
    }

    private static UIElement? ParseElement(XElement element, Dictionary<string, Dictionary<string, string>> styles, dynamic? model)
    {
        var innerElement = ParseElementTag(element);
        if (innerElement == null)
        {
            if (element.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase))
            {
                var content = element.Value.Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    ParseStyles(content, styles);
                }
            }
            return null;
        }

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
            // If the inner element was using the parser default (1fr), change it to auto when wrapped by a scroll-viewport.
            if (rootElement.Width.Type == UnitType.Fr) rootElement.Width = UnitValue.Auto;
            if (rootElement.Height.Type == UnitType.Fr) rootElement.Height = UnitValue.Auto;

            scroll.AddChild(rootElement, element);
            rootElement = scroll;
        }

        if (borderAttr != null)
        {
            var border = new Border();
            border.SetAttribute("border", borderAttr.Value);
            // Similar behavior for border wrapper: inner element should become `auto` for sizing if it was the parser default (1fr).
            if (rootElement.Width.Type == UnitType.Fr) rootElement.Width = UnitValue.Auto;
            if (rootElement.Height.Type == UnitType.Fr) rootElement.Height = UnitValue.Auto;

            border.AddChild(rootElement, element);
            rootElement = border;
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
                        var childElement = ParseElement(child, styles, model);
                        if (childElement == null) continue;

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
                        var childElement = ParseElement(child, styles, model);
                        if (childElement == null) continue;

                        grid.AddChild(childElement, child);
                        rowIdx++;
                    }

                    columnIndex++;
                }
                else
                {
                    var childElement = ParseElement(node, styles, model);
                    if (childElement == null) continue;

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
                        var textElement = new Text { Value = text };
                        if (styles != null && styles.Count > 0)
                        {
                            textElement = ApplyStylesToElement(textElement, styles, model);
                        }
                        innerElement.AddChild(textElement, element);
                    }
                }
                else if (node is XElement childXElement)
                {
                    var childElement = ParseElement(childXElement, styles, model);
                    if (childElement == null) continue;

                    innerElement.AddChild(childElement, childXElement);
                }
            }
        }

        foreach (var attr in attributes.Where(x => IsStyleApplicationAttribute(x.Name.LocalName)))
        {
            SetAttribute(attr, model, rootElement, innerElement);
        }

        if (styles != null && styles.Count > 0)
        {
            rootElement = ApplyStylesToElement(rootElement, styles, model);
        }

        foreach (var attr in attributes)
        {
            var name = attr.Name.LocalName;
            if (name.Equals("scroll", StringComparison.OrdinalIgnoreCase) || name.Equals("border", StringComparison.OrdinalIgnoreCase) || name.Equals("class", StringComparison.OrdinalIgnoreCase)) continue;

            SetAttribute(attr, model, rootElement, innerElement);
        }

        return rootElement;
    }

    private static void SetAttribute(XAttribute attr, dynamic? model, UIElement rootElement, UIElement innerElement)
    {
        var name = attr.Name.LocalName;
        var target = IsLayoutAttribute(name) ? rootElement : innerElement;

        if (attr.Value.StartsWith('@'))
        {
            // Dynamic Binding: <grid width="@myVar" />
            var modelPropName = attr.Value.Substring(1);
            var binding = new PropertyBinding(model, modelPropName, target, name);
            target.Bindings.Add(binding);
            binding.Apply();
        }
        else
        {
            target.SetAttribute(name, attr.Value);
        }
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

    private static readonly HashSet<string> StyleApplicationAttributeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "class"
    };

    private static bool IsStyleApplicationAttribute(string name)
    {
        return StyleApplicationAttributeNames.Contains(name);
    }

    private static UIElement? ParseElementTag(XElement element)
    {
        var tag = element.Name.LocalName;

        // Layout/Structural tags
        if (tag.Equals("div", StringComparison.OrdinalIgnoreCase)) return new Div();
        if (tag.Equals("stack", StringComparison.OrdinalIgnoreCase)) return new Stack();
        if (tag.Equals("hstack", StringComparison.OrdinalIgnoreCase) || tag.Equals("hbox", StringComparison.OrdinalIgnoreCase)) 
            return new Stack { Orientation = Orientation.Horizontal };
        if (tag.Equals("vstack", StringComparison.OrdinalIgnoreCase) || tag.Equals("vbox", StringComparison.OrdinalIgnoreCase)) 
            return new Stack { Orientation = Orientation.Vertical };
        if (tag.Equals("grid", StringComparison.OrdinalIgnoreCase)) return new Grid();
        if (tag.Equals("dock", StringComparison.OrdinalIgnoreCase)) return new Dock();
        if (tag.Equals("overlay", StringComparison.OrdinalIgnoreCase)) return new Overlay();
        if (tag.Equals("border", StringComparison.OrdinalIgnoreCase)) return new Border();
        
        // Content tags
        if (tag.Equals("label", StringComparison.OrdinalIgnoreCase)) return new Label();
        if (tag.Equals("button", StringComparison.OrdinalIgnoreCase)) return new Button();
        if (tag.Equals("image", StringComparison.OrdinalIgnoreCase)) return new Image();
        if (tag.Equals("input", StringComparison.OrdinalIgnoreCase)) return new Input();
        if (tag.Equals("select", StringComparison.OrdinalIgnoreCase)) return new Select();
        if (tag.Equals("option", StringComparison.OrdinalIgnoreCase)) return new Option();
        if (tag.Equals("textarea", StringComparison.OrdinalIgnoreCase)) return new TextArea();
        
        // Special tags
        if (tag.Equals("style", StringComparison.OrdinalIgnoreCase)) return null;
        if (tag.Equals("model", StringComparison.OrdinalIgnoreCase)) return null;

        throw new NotSupportedException($"Unknown tag: {element.Name.LocalName}");
    }
}
