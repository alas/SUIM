namespace SUIM.Parse;

using System;
using System.Xml.Linq;
using SUIM.Model;
using SUIM.Parse.Components;

public static partial class MarkupParser
{
    public static UIElement Parse(string markup, object? model = null, Dictionary<string, Dictionary<string, string>>? inheritedStyles = null, string? basePath = null, string? componentName = null, bool isView = false)
    {
        dynamic? model2 = model == null ? null : ModelLogic.Create(model);
        var controlFlowParser = new ControlFlowParser(model2);
        var expandedMarkup = controlFlowParser.ExpandDirectives(markup);

        var doc = XDocument.Parse(expandedMarkup);
        var root = doc.Root!;

        Dictionary<string, Dictionary<string, string>> styles = inheritedStyles != null ? new(inheritedStyles, SpanStringIgnoreCaseComparer.Instance) : [];
        Dictionary<string, Dictionary<string, string>> leakableStyles = inheritedStyles != null ? new(inheritedStyles, SpanStringIgnoreCaseComparer.Instance) : [];

        model2 = ModelLogic.ExtractModel(root, model2) ?? new ObservableObject();

        // Root tag matches view/component name or "root": bypass redundant wrapper and process real root
        if (string.Equals(root.Name.LocalName, componentName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(root.Name.LocalName, "root", StringComparison.OrdinalIgnoreCase))
        {
            var styleElements = root.Elements().Where(x => x.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var styleElement in styleElements)
            {
                ParseStyle(styleElement, styles, leakableStyles, basePath);
            }

            root = root.Elements().Single(x => !x.Name.LocalName.Equals("model", StringComparison.OrdinalIgnoreCase)
                && !x.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase));
        }

        var element = ParseElement(root, styles, leakableStyles, model2, basePath)
            ?? throw new InvalidOperationException("Root element not found.");

        element.Model = model2;
        if (componentName != null) element.IsComponentRoot = true;

        if (!string.IsNullOrWhiteSpace(componentName) && ComponentRegistry.IsRegisteredFactory(componentName))
        {
            var codeBehind = ComponentRegistry.Create(componentName, true, isView);

            codeBehind.Model = model2;
            EventHandlerResolver.BindEventsRecursive(element, codeBehind);
        }

        return element;
    }

    public static bool IsBuiltInTag(string tag)
    {
        var builtIn = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "div", "stack", "hbox", "vbox", "hstack", "vstack", "stackh", "stackv", "stack-h", "stack-v",
            "grid", "dock", "overlay", "border", "label", "button", "image", "input", "select", "option",
            "textarea", "style", "model", "h1", "h2", "h3", "h4", "h5", "h6", "h7", "h8",
            "root", // virtual container tag
            "row", "column" // Grid specials
        };
        return builtIn.Contains(tag);
    }

    private static void ParseStyle(XElement element, Dictionary<string, Dictionary<string, string>> styles, Dictionary<string, Dictionary<string, string>> leakableStyles, string? basePath)
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
            ParseStyle(element, styles, leakableStyles, basePath);
            return null;
        }

        var rootElement = innerElement;

        var attributes = element.Attributes().ToList();

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
                        if (!child.Attributes("grid.row").Any())
                        {
                            child.SetAttributeValue("grid.row", rowIndex.ToString());
                        }

                        if (!child.Attributes("grid.column").Any())
                        {
                            child.SetAttributeValue("grid.column", colIdx.ToString());
                        }
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

                        if (!child.Attributes("grid.column").Any())
                        {
                            child.SetAttributeValue("grid.column", columnIndex.ToString());
                        }

                        if (!child.Attributes("grid.row").Any())
                        {
                            child.SetAttributeValue("grid.row", rowIdx.ToString());
                        }
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

                        if (chunks.Count > 0)
                        {
                            // Mixed static text and dynamic tokens: "Hello @name!" -> ["Hello ", "@name", "!"]
                            foreach (var chunk in chunks)
                            {
                                var textElement = new Text();
                                textElement.SetAttribute("value", chunk);
                                textElement.SetAttribute("font", innerElement.Font);
                                textElement.SetAttribute("font-size", innerElement.FontSize);
                                if (SUIM.Binding.BindingExpression.TryGetModelPropertyName(chunk, out var modelPropName))
                                {
                                    textElement.Bindings.Add(new BindingDefinition("value", modelPropName));
                                }

                                var uiElement = CssStyle.ApplyToElement(textElement, styles, null);

                                innerElement.AddChild(uiElement, null);
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

        rootElement = CssStyle.ApplyToElement(rootElement, styles, attributes);
        
        if (rootElement is VirtualComponent custom)
        {
            return custom.Expand(model, basePath, leakableStyles);
        }

        return rootElement;
    }

    private static UIElement? ParseElementTag(XElement element)
    {
        var tag = element.Name.LocalName;

        // Layout/Structural tags
        if (tag.Equals("div", StringComparison.OrdinalIgnoreCase)) return new Div();
        if (tag.Equals("stack", StringComparison.OrdinalIgnoreCase)) return new Stack();
        if (tag.Equals("hstack", StringComparison.OrdinalIgnoreCase) || tag.Equals("hbox", StringComparison.OrdinalIgnoreCase)
            || tag.Equals("stackh", StringComparison.OrdinalIgnoreCase) || tag.Equals("stack-h", StringComparison.OrdinalIgnoreCase)) 
            return new Stack { Orientation = Orientation.Horizontal };
        if (tag.Equals("vstack", StringComparison.OrdinalIgnoreCase) || tag.Equals("vbox", StringComparison.OrdinalIgnoreCase)
            || tag.Equals("stackv", StringComparison.OrdinalIgnoreCase) || tag.Equals("stack-v", StringComparison.OrdinalIgnoreCase)) 
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
            return ComponentRegistry.Create(tag, false, false);
        }

        throw new NotSupportedException($"Unknown tag: {element.Name.LocalName}");
    }

    private static List<string> SplitAtSingleAtTokens(ReadOnlySpan<char> input)
    {
        var result = new List<string>();

        int i = 0;
        int segmentStart = 0;

        while (i < input.Length)
        {
            // Escaped @@ → normal text
            if (i + 1 < input.Length && input[i] == '@' && input[i + 1] == '@')
            {
                i += 2;
                continue;
            }

            // Single @ that forms a valid token (@ + non-whitespace)
            if (input[i] == '@' &&
                i + 1 < input.Length &&
                !char.IsWhiteSpace(input[i + 1]))
            {
                // Flush preceding text
                if (i > segmentStart)
                {
                    result.Add(input[segmentStart..i].ToString());
                }

                int tokenStart = i;
                i++; // skip '@'

                while (i < input.Length && !char.IsWhiteSpace(input[i]))
                {
                    // Stop if escaped @@ appears
                    if (i + 1 < input.Length && input[i] == '@' && input[i + 1] == '@')
                        break;

                    i++;
                }

                result.Add(input[tokenStart..i].ToString());

                segmentStart = i;
                continue;
            }

            i++;
        }

        // Flush remaining text
        if (segmentStart < input.Length)
        {
            result.Add(input[segmentStart..].ToString());
        }

        return result;
    }
}
