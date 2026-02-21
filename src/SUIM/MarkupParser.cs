namespace SUIM;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SUIM.Components;
using SUIM.Components.Attributes;

public static partial class MarkupParser
{
    public static (UIElement, dynamic?) Parse(string markup, object? model = null, Dictionary<string, Dictionary<string, string>>? inheritedStyles = null, string? basePath = null, string? componentName = null)
    {
        dynamic? model2 = model == null ? null : ModelLogic.Create(model);
        var controlFlowParser = new ControlFlowParser(model2);
        var expandedMarkup = controlFlowParser.ExpandDirectives(markup);

        var doc = XDocument.Parse(expandedMarkup);
        var root = doc.Root!;

        Dictionary<string, Dictionary<string, string>> styles = inheritedStyles != null ? new(inheritedStyles) : [];
        Dictionary<string, Dictionary<string, string>> leakableStyles = inheritedStyles != null ? new(inheritedStyles) : [];

        // Extract model from root children (flexible position)
        model2 = ModelLogic.ExtractModel(root, model2);

        UIElement element;
        if (componentName != null && string.Equals(root.Name.LocalName, componentName, StringComparison.OrdinalIgnoreCase))
        {
            // Root tag matches component name: bypass redundant wrapper and process children
            element = new Div(componentName)
            {
                Model = model2,
                IsComponentRoot = true
            };

            foreach (var node in root.Nodes())
            {
                if (node is XElement childX && (childX.Name.LocalName.Equals("model", StringComparison.OrdinalIgnoreCase) || childX.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase)))
                {
                    if (childX.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase))
                    {
                        var isScoped = childX.Attribute("scoped") != null;
                        // Process styles as usual
                        var sourceAttr = childX.Attribute("source") ?? childX.Attribute("src");
                        if (sourceAttr != null && !string.IsNullOrEmpty(basePath))
                        {
                            var stylePath = Path.Combine(basePath, sourceAttr.Value);
                            if (File.Exists(stylePath)) 
                            {
                                var content = File.ReadAllText(stylePath);
                                ParseStyles(content, styles);
                                if (!isScoped) ParseStyles(content, leakableStyles);
                            }
                        }
                        var styleContent = childX.Value.Trim();
                        if (!string.IsNullOrEmpty(styleContent))
                        {
                            ParseStyles(styleContent, styles);
                            if (!isScoped) ParseStyles(styleContent, leakableStyles);
                        }
                    }
                    continue;
                }

                if (node is XText textNode)
                {
                    var text = textNode.Value.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        var textElement = new Text { Value = text };
                        if (styles.Count > 0) textElement = (Text)ApplyStylesToElement(textElement, styles);
                        element.AddChild(textElement, root);
                    }
                }
                else if (node is XElement childE)
                {
                    var childElement = ParseElement(childE, styles, leakableStyles, model2, basePath);
                    if (childElement != null) element.AddChild(childElement, childE);
                }
            }
        }
        else
        {
            element = ParseElement(root, styles, leakableStyles, model2, basePath)
                ?? throw new InvalidOperationException("Root element not found.");
        }

        element.Model = model2;
        if (componentName != null) element.IsComponentRoot = true;

        return (element, model2);
    }

    private static void ParseStyles(string styleContent, Dictionary<string, Dictionary<string, string>> styles)
    {        
        // CSS-like parser supporting: .classname, #id, tagname, and *
        // Format: selector { property: value; property: value; }
        var selectorRegex = MyRegex();
        var matches = selectorRegex.Matches(styleContent);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var selector = match.Groups[1].Value.Trim().ToLowerInvariant();
            var propertiesContent = match.Groups[2].Value;

            var properties = new Dictionary<string, string>();
            // Parse properties: "property: value, property: value"
            var propertyRegex = MyRegex1();
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

    private static UIElement ApplyStylesToElement(UIElement element, Dictionary<string, Dictionary<string, string>> styles)
    {
        var elementTag = element.TagName;
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
                var classSelector = "." + className.ToLowerInvariant();
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
            var idSelector = "#" + elementId.Trim().ToLowerInvariant();
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
            element = ApplyStylePropertiesToElement(element, mergedProperties);
        }
        return element;
    }

    private static UIElement ApplyStylePropertiesToElement(UIElement element, Dictionary<string, string> properties)
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

            // When a style creates a scroll wrapper, the inner element should default to `auto` if it was unspecified.
            element.Width ??= "auto";
            element.Height ??= "auto";

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
                border.Width = pw;
            }
            if (properties.TryGetValue("height", out var ph) && !string.IsNullOrWhiteSpace(ph))
            {
                border.Height = ph;
            }

            element.Width ??= "auto";
            element.Height ??= "auto";
            border.AddChild(element, null);
            element = border;
        }

        return element;
    }

    private static UIElement? ParseElement(XElement element, Dictionary<string, Dictionary<string, string>> styles, Dictionary<string, Dictionary<string, string>> leakableStyles, dynamic? model, string? basePath = null)
    {
        var innerElement = ParseElementTag(element);
        if (innerElement == null)
        {
            if (element.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase))
            {
                var isScoped = element.Attribute("scoped") != null;
                var sourceAttr = element.Attribute("source") ?? element.Attribute("src");
                if (sourceAttr != null && !string.IsNullOrEmpty(basePath))
                {
                    var stylePath = Path.Combine(basePath, sourceAttr.Value);
                    if (!File.Exists(stylePath))
                    {
                        throw new FileNotFoundException($"Style file not found: {stylePath}");
                    }

                    var content = File.ReadAllText(stylePath);
                    ParseStyles(content, styles);
                    if (!isScoped) ParseStyles(content, leakableStyles);
                }

                var styleContent = element.Value.Trim();
                if (!string.IsNullOrEmpty(styleContent))
                {
                    ParseStyles(styleContent, styles);
                    if (!isScoped) ParseStyles(styleContent, leakableStyles);
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
            // If the inner element was unspecified, change it to auto when wrapped by a scroll-viewport.
            rootElement.Width ??= "auto";
            rootElement.Height ??= "auto";

            scroll.AddChild(rootElement, element);
            rootElement = scroll;
        }

        if (borderAttr != null)
        {
            var border = new Border();
            border.SetAttribute("border", borderAttr.Value);
            // Similar behavior for border wrapper: inner element should become `auto` for sizing if it was unspecified.
            rootElement.Width ??= "auto";
            rootElement.Height ??= "auto";

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
                        var childElement = ParseElement(child, styles, leakableStyles, model, basePath);
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
                        var childElement = ParseElement(child, styles, leakableStyles, model, basePath);
                        if (childElement == null) continue;

                        grid.AddChild(childElement, child);
                        rowIdx++;
                    }

                    columnIndex++;
                }
                else
                {
                    var childElement = ParseElement(node, styles, leakableStyles, model, basePath);
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
                            textElement = (Text)ApplyStylesToElement(textElement, styles);
                        }
                        innerElement.AddChild(textElement, element);
                    }
                }
                else if (node is XElement childXElement)
                {
                    var childElement = ParseElement(childXElement, styles, leakableStyles, model, basePath);
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
            rootElement = ApplyStylesToElement(rootElement, styles);
        }

        foreach (var attr in attributes)
        {
            var name = attr.Name.LocalName;
            if (name.Equals("scroll", StringComparison.OrdinalIgnoreCase) || name.Equals("border", StringComparison.OrdinalIgnoreCase) || name.Equals("class", StringComparison.OrdinalIgnoreCase)) continue;

            SetAttribute(attr, model, rootElement, innerElement);
        }

        if (rootElement is CustomComponent custom)
        {
            custom.Expand(model, leakableStyles, basePath);
        }

        return rootElement;
    }

    private static void SetAttribute(XAttribute attr, ObservableObject? model, UIElement rootElement, UIElement innerElement)
    {
        var name = attr.Name.LocalName;
        var target = IsLayoutAttribute(name) ? rootElement : innerElement;

        // Store raw attribute for CustomComponent expansion or other metadata
        var value = attr.Value;
        target.SetAttribute(name, value);

        if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            // Event Binding: onclick="MethodName()"
            var handlerName = value;
            target.Events[name[2..]] = handlerName;
            return;
        }

        if (value.StartsWith('@'))
        {
            // Dynamic Binding: <grid width="@myVar" />
            var modelPropName = value[1..];
            target.Bindings.Add(new BindingDefinition(name, modelPropName));
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

        // Custom tags
        if (ComponentRegistry.IsRegistered(tag))
        {
            return ComponentRegistry.Create(tag);
        }

        throw new NotSupportedException($"Unknown tag: {element.Name.LocalName}");
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"([#.]?[a-zA-Z0-9_*-]+)\s*\{([^}]*)\}")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"([a-zA-Z0-9\-]+)\s*:\s*([^;}]+)")]
    private static partial System.Text.RegularExpressions.Regex MyRegex1();
}
