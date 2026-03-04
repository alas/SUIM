namespace SUIM.Parse;

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using SUIM.Model;
using SUIM.Parse.Components;

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
            element = new CustomComponent(componentName)
            {
                Model = model2,
                IsComponentRoot = true
            };

            foreach (var node in root.Nodes())
            {
                if (node is XElement childX && (childX.Name.LocalName.Equals("model", StringComparison.OrdinalIgnoreCase) || childX.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase)))
                {
                    ParseStyleInternal(childX, styles, leakableStyles, basePath);
                    continue;
                }

                if (node is XText textNode)
                {
                    var text = textNode.Value.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        UIElement textElement = new Text { Value = text };
                        if (styles.Count > 0) textElement = CssStyle.ApplyToElement(textElement, styles);
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

    private static void ParseStyleInternal(XElement element, Dictionary<string, Dictionary<string, string>> styles, Dictionary<string, Dictionary<string, string>> leakableStyles, string? basePath)
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
                CssStyle.Parse(content, styles);
                if (!isScoped) CssStyle.Parse(content, leakableStyles);
            }

            var styleContent = element.Value.Trim();
            if (!string.IsNullOrEmpty(styleContent))
            {
                CssStyle.Parse(styleContent, styles);
                if (!isScoped) CssStyle.Parse(styleContent, leakableStyles);
            }
        }
    }

    private static UIElement? ParseElement(XElement element, Dictionary<string, Dictionary<string, string>> styles, Dictionary<string, Dictionary<string, string>> leakableStyles, dynamic? model, string? basePath)
    {
        var innerElement = ParseElementTag(element);
        if (innerElement == null)
        {
            ParseStyleInternal(element, styles, leakableStyles, basePath);
            return null;
        }

        var rootElement = innerElement;

        var attributes = element.Attributes().ToList();
        var scrollAttr = attributes.FirstOrDefault(a => a.Name.LocalName.Equals("scroll", StringComparison.OrdinalIgnoreCase));
        var borderAttr = attributes.FirstOrDefault(a => a.Name.LocalName.Equals("border", StringComparison.OrdinalIgnoreCase));
        var bgAttr = attributes.FirstOrDefault(a => a.Name.LocalName.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase));

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

        if (bgAttr != null)
        {
            var bg = new BackgroundImage();
            bg.SetAttribute("backgroundimage", bgAttr.Value);
            bg.AddChild(rootElement, element);
            rootElement = bg;
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
                        List<string> chunks = [text];
                        if (text.Contains('@'))
                        {
                            chunks = SplitAtSingleAtTokens(text);
                        }

                        if (chunks.Count > 1)
                        {
                            // Mixed static text and dynamic tokens: "Hello @name!" -> ["Hello ", "@name", "!"]
                            foreach (var chunk in chunks)
                            {
                                UIElement textElement = new Text() { Value = chunk };
                                if (chunk.Length > 1 && chunk.StartsWith('@') && !chunk.StartsWith("@@"))
                                {
                                    var modelPropName = chunk[1..];
                                    textElement.Bindings.Add(new BindingDefinition("value", modelPropName));
                                }

                                if (styles != null && styles.Count > 0)
                                {
                                    textElement = CssStyle.ApplyToElement(textElement, styles);
                                }
                                innerElement.AddChild(textElement, null);
                            }
                        }
                        else if (chunks.Count == 1)
                        {
                            var textElement = innerElement is Text t ? t : new Text();
                            textElement.Value = text;
                            if (text.Length > 1 && text.StartsWith('@') && !text.StartsWith("@@"))
                            {
                                var modelPropName = text[1..];
                                textElement.Bindings.Add(new BindingDefinition("value", modelPropName));
                            }

                            UIElement result = textElement;
                            if (styles != null && styles.Count > 0)
                            {
                                result = CssStyle.ApplyToElement(textElement, styles);
                            }

                            if (innerElement is not Text)
                            {
                                innerElement.AddChild(result, null);
                            }
                        }
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
            SetAttribute(attr, rootElement, innerElement);
        }

        if (styles != null && styles.Count > 0)
        {
            rootElement = CssStyle.ApplyToElement(rootElement, styles);
        }

        foreach (var attr in attributes)
        {
            var name = attr.Name.LocalName;
            if (name.Equals("scroll", StringComparison.OrdinalIgnoreCase) || name.Equals("border", StringComparison.OrdinalIgnoreCase) || name.Equals("class", StringComparison.OrdinalIgnoreCase)) continue;

            SetAttribute(attr, rootElement, innerElement);
        }

        if (rootElement is CustomComponent custom)
        {
            custom.Expand(model, leakableStyles, basePath);
        }

        return rootElement;
    }

    private static void SetAttribute(XAttribute attr, UIElement rootElement, UIElement innerElement)
    {
        var name = attr.Name.LocalName;
        var target = CssStyle.IsLayoutAttribute(name) ? rootElement : innerElement;

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
        if (tag.Equals("button", StringComparison.OrdinalIgnoreCase)) return new Button();
        if (tag.Equals("image", StringComparison.OrdinalIgnoreCase) || tag.Equals("img", StringComparison.OrdinalIgnoreCase)) return new Image();
        if (tag.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase) || tag.Equals("bg", StringComparison.OrdinalIgnoreCase)) return new BackgroundImage();
        if (tag.Equals("input", StringComparison.OrdinalIgnoreCase)) return new Input();
        if (tag.Equals("select", StringComparison.OrdinalIgnoreCase)) return new Select();
        if (tag.Equals("option", StringComparison.OrdinalIgnoreCase)) return new Option();
        if (tag.Equals("textarea", StringComparison.OrdinalIgnoreCase)) return new TextArea();
        if (tag.Equals("label", StringComparison.OrdinalIgnoreCase)) return new Label();
        if (tag.Equals("p", StringComparison.OrdinalIgnoreCase)) return new P();
        if (tag.Equals("h1", StringComparison.OrdinalIgnoreCase)) return new H1();
        if (tag.Equals("h2", StringComparison.OrdinalIgnoreCase)) return new H2();
        if (tag.Equals("h3", StringComparison.OrdinalIgnoreCase)) return new H3();
        if (tag.Equals("h4", StringComparison.OrdinalIgnoreCase)) return new H4();
        if (tag.Equals("h5", StringComparison.OrdinalIgnoreCase)) return new H5();
        if (tag.Equals("h6", StringComparison.OrdinalIgnoreCase)) return new H6();
        if (tag.Equals("h7", StringComparison.OrdinalIgnoreCase)) return new H7();
        if (tag.Equals("h8", StringComparison.OrdinalIgnoreCase)) return new H8();
        
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

    private static List<string> SplitAtSingleAtTokens(string input)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(input))
            return result;

        var buffer = new StringBuilder();
        int i = 0;

        while (i < input.Length)
        {
            // Check for escaped @@
            if (i + 1 < input.Length && input[i] == '@' && input[i + 1] == '@')
            {
                buffer.Append("@@");
                i += 2;
                continue;
            }

            // Check for single @ token start
            if (input[i] == '@')
            {
                // Flush previous text
                if (buffer.Length > 0)
                {
                    result.Add(buffer.ToString());
                    buffer.Clear();
                }

                var token = new StringBuilder();
                token.Append('@');
                i++;

                // Capture token characters until whitespace or end
                while (i < input.Length && !char.IsWhiteSpace(input[i]))
                {
                    // Stop if we encounter @@ (escape inside token should stay normal text)
                    if (i + 1 < input.Length && input[i] == '@' && input[i + 1] == '@')
                        break;

                    token.Append(input[i]);
                    i++;
                }

                result.Add(token.ToString());
                continue;
            }

            // Normal character
            buffer.Append(input[i]);
            i++;
        }

        // Flush remaining text
        if (buffer.Length > 0)
            result.Add(buffer.ToString());

        return result;
    }
}
