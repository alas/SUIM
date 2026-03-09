namespace SUIM;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SUIM.Parse.Components;

/// <summary>
/// Handles CSS-like style parsing and application to UI elements.
/// Supports multiple selectors separated by commas and various selector types (tag, class, id, universal).
/// </summary>
public static class CssStyle
{
    /// <summary>
    /// Parses CSS-like style content and populates the styles dictionary.
    /// Supports multiple selectors separated by commas (e.g., "text, label { ... }").
    /// Format: selector { property: value; property: value; }
    /// Supported selectors: .classname, #id, tagname, *, and comma-separated combinations.
    /// </summary>
    public static void Parse(string styleContent, Dictionary<string, Dictionary<string, string>> styles)
    {        
        styleContent = StyleParserRegexes.CommentRegex().Replace(styleContent, "");
        var selectorRegex = StyleParserRegexes.SelectorRegex();
        var matches = selectorRegex.Matches(styleContent);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var selectorsRaw = match.Groups[1].Value.Trim();
            var propertiesContent = match.Groups[2].Value;

            // Split multiple selectors by comma
            var selectors = selectorsRaw.Split(',')
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            var properties = new Dictionary<string, string>();
            // Parse properties: "property: value; property: value"
            var propertyRegex = StyleParserRegexes.PropertyRegex();
            var propMatches = propertyRegex.Matches(propertiesContent);

            foreach (System.Text.RegularExpressions.Match propMatch in propMatches)
            {
                var propName = propMatch.Groups[1].Value.Trim();
                var propValue = propMatch.Groups[2].Value.Trim();
                properties[propName] = propValue;
            }

            if (properties.Count > 0)
            {
                // Apply the same properties to all selectors, merging with existing properties
                foreach (var selector in selectors)
                {
                    if (!styles.TryGetValue(selector, out Dictionary<string, string>? value))
                    {
                        value = [];
                        styles[selector] = value;
                    }

                    // Merge properties: new properties override existing ones with the same name
                    foreach (var kvp in properties)
                    {
                        value[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies styles to an element based on CSS-like precedence rules.
    /// Precedence order: universal (*) < tag < class < id < inline style < inline attributes
    /// </summary>
    internal static UIElement ApplyToElement(UIElement element, Dictionary<string, Dictionary<string, string>> styles, List<XAttribute>? attributes)
    {
        // 1. Initial Attributes: Set Id and Class first as they are needed for selector lookups
        if (attributes != null)
        {
            foreach (var attr in attributes)
            {
                var name = attr.Name.LocalName;
                if (name.Equals("id", StringComparison.OrdinalIgnoreCase)) element.SetAttribute("id", attr.Value);
                else if (name.Equals("class", StringComparison.OrdinalIgnoreCase)) element.SetAttribute("class", attr.Value);
            }
        }

        // 2. Merge styles from all matching selectors in order of precedence (low to high)
        var mergedProperties = new Dictionary<string, string>();

        // Universal selector (lowest precedence)
        if (styles?.Count > 0 && styles!.TryGetValue("*", out var universalProps))
        {
            foreach (var kvp in universalProps)
            {
                mergedProperties[kvp.Key] = kvp.Value;
            }
        }

        // Tag selector
        var elementTag = element.TagName;
        if (styles?.Count > 0 && styles.TryGetValue(elementTag, out var tagProps))
        {
            foreach (var kvp in tagProps)
            {
                mergedProperties[kvp.Key] = kvp.Value;
            }
        }

        // Class selector(s) - support multiple space-separated classes
        var elementClass = element.Class;
        if (styles?.Count > 0 && !string.IsNullOrEmpty(elementClass))
        {
            var classes = elementClass.AsSpan().Split(' ');
            foreach (var segment in classes)
            {
                var className = elementClass.AsSpan()[segment].Trim();
                if (className.IsEmpty) continue;

                var classProps = GetProps(styles, mergedProperties, className, '.');
                if (classProps != null)
                {
                    foreach (var kvp in classProps)
                    {
                        mergedProperties[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        // ID selector
        var elementId = (element.Id ?? "").AsSpan();
        if (styles?.Count > 0  && !elementId.IsEmpty)
        {
            var idProps = GetProps(styles, mergedProperties, elementId, '#');
            if (idProps != null)
            {
                foreach (var kvp in idProps)
                {
                    mergedProperties[kvp.Key] = kvp.Value;
                }
            }
        }

        // Inline style attribute
        var styleAttribute = attributes?.FirstOrDefault(a => a.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase))?.Value;
        if (styleAttribute != null)
        {
            var stylePairs = styleAttribute.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in stylePairs)
            {
                var kv = pair.Split(':', 2);
                if (kv.Length == 2)
                {
                    var key = kv[0].Trim();
                    var val = kv[1].Trim();
                    mergedProperties[key] = val;
                }
            }
        }

        // Inline attributes (highest precedence, overrides all)
        if (attributes != null)
        {
            foreach (var attr in attributes)
            {
                var name = attr.Name.LocalName;
                if (ApplyToElement_skipped.Contains(name)) continue;
                mergedProperties[name] = attr.Value;
            }
        }

        if (mergedProperties.Count <= 0) return element;

        return ApplyPropertiesToElement(element, mergedProperties);

        static Dictionary<string, string>? GetProps(Dictionary<string, Dictionary<string, string>> styles, Dictionary<string, string> mergedProperties, ReadOnlySpan<char> elementId, char prefix)
        {
            var trimmed = elementId.Trim();
            Span<char> selectorBuffer = stackalloc char[trimmed.Length + 1];
            selectorBuffer[0] = prefix;
            trimmed.ToLowerInvariant(selectorBuffer[1..]);
            ReadOnlySpan<char> selector = selectorBuffer;
            var lookup = styles.GetAlternateLookup<ReadOnlySpan<char>>();
            if (lookup.TryGetValue(selector, out var props))
            {
                return props;
            }

            return null;
        }
    }
    private static readonly HashSet<string> ApplyToElement_skipped = new(StringComparer.OrdinalIgnoreCase) { "style", "id", "class" };

    /// <summary>
    /// Applies individual style properties to an element, handling special cases like
    /// border, scroll, and background image wrappers.
    /// </summary>
    private static UIElement ApplyPropertiesToElement(UIElement element, Dictionary<string, string> properties)
    {
        // Extract border and scroll attributes for special handling
        string? borderAttr = null;
        List<string> scrollAttr = [];
        var regularAttrs = new Dictionary<string, string>();
        var wrapperAttrs = new Dictionary<string, string>();

        // Pre-check whether this style will create a wrapper so layout attributes can be routed to it.
        bool willWrapWithBorder = properties.Keys.Any(k => k.Equals("border", StringComparison.OrdinalIgnoreCase));
        bool willWrapWithScroll = properties.Any(x => x.Key.StartsWith("overflow", StringComparison.OrdinalIgnoreCase) && x.Value.Equals("scroll", StringComparison.OrdinalIgnoreCase));

        foreach (var kvp in properties)
        {
            var propName = kvp.Key;
            var propValue = kvp.Value;

            if (propName.Equals("border", StringComparison.OrdinalIgnoreCase))
            {
                borderAttr = propValue;
            }
            else if (propName.StartsWith("overflow", StringComparison.OrdinalIgnoreCase) && propValue.Equals("scroll", StringComparison.OrdinalIgnoreCase))
            {
                scrollAttr.Add(propName);
            }
            else if (propName.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase) || propName.Equals("background-image", StringComparison.OrdinalIgnoreCase))
            {
                // Background image attribute will be handled by the wrapper creation
            }
            else if ((willWrapWithBorder || willWrapWithScroll) && IsLayoutAttribute(propName))
            {
                // If a style defines layout attributes for an element that will be wrapped (border/scroll/bg),
                // apply those layout attributes to the wrapper instead of the inner element.
                wrapperAttrs[propName] = propValue;
            }
            else
            {
                regularAttrs[propName] = propValue;
            }
        }

        // Helper to handle SetAttribute with bindings and events
        static void ApplyToTarget(UIElement target, string name, string value)
        {
            target.SetAttribute(name, value);

            if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            {
                target.Events[name[2..]] = value;
            }
            else if (value.StartsWith('@') && !value.StartsWith("@@"))
            {
                target.Bindings.Add(new BindingDefinition(name, value[1..]));
            }
        }

        // Apply regular attributes to the element (inner)
        foreach (var kvp in regularAttrs)
        {
            ApplyToTarget(element, kvp.Key, kvp.Value);
        }

        // Handle BackgroundImage, it is applied first to be the inserted as the first child of the node,
        // BackgroundImage fills the entire space occupied by its parent and its removed from the layout calculation by setting its position style as absolute
        if (element is not BackgroundImage && (properties.TryGetValue("backgroundimage", out var bgImgAttr) || properties.TryGetValue("background-image", out bgImgAttr)))
        {
            var bg = new BackgroundImage();
            ApplyToTarget(bg, "backgroundimage", bgImgAttr!);

            element.InsertChild(bg, 0, null);
        }

        // Handle scroll wrapper
        if (element is not Scroll && scrollAttr.Count > 0)
        {
            var hasScrollX = scrollAttr.Any(x => x.Equals("overflow", StringComparison.OrdinalIgnoreCase)
                || x.Equals("overflow-x", StringComparison.OrdinalIgnoreCase));
            var hasScrollY = scrollAttr.Any(x => x.Equals("overflow", StringComparison.OrdinalIgnoreCase)
                || x.Equals("overflow-y", StringComparison.OrdinalIgnoreCase));
            if (hasScrollX || hasScrollY)
            {
                var scroll = new Scroll
                {
                    Direction = hasScrollX && hasScrollY
                        ? ScrollDirection.Both : hasScrollX
                        ? ScrollDirection.Horizontal : ScrollDirection.Vertical
                };

                // Apply any layout attributes from the style to the scroll wrapper (width/height etc.)
                foreach (var kvp in wrapperAttrs)
                {
                    ApplyToTarget(scroll, kvp.Key, kvp.Value);
                }
                // Fallback: also apply explicit width/height from properties if present (defensive)
                if (properties.TryGetValue("width", out var w)) ApplyToTarget(scroll, "width", w);
                if (properties.TryGetValue("height", out var h)) ApplyToTarget(scroll, "height", h);

                element.SetAttribute("width", scroll.ScrollX);
                element.SetAttribute("height", scroll.ScrollY);

                scroll.AddChild(element, null);
                element = scroll;
            }
        }

        // Handle border wrapper (must be applied last to wrap scroll if present)
        if (element is not Border && !string.IsNullOrEmpty(borderAttr))
        {
            var border = new Border();
            ApplyToTarget(border, "border", borderAttr);

            // Apply any layout attributes from the style to the border wrapper (width/height etc.)
            foreach (var kvp in wrapperAttrs)
            {
                ApplyToTarget(border, kvp.Key, kvp.Value);
            }

            border.AddChild(element, null);
            element = border;
        }

        return element;
    }

    internal static bool IsLayoutAttribute(string name)
    {
        return IsLayoutAttribute_names.Contains(name);
    }

    private static readonly HashSet<string> IsLayoutAttribute_names = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "width", "height", "padding", "margin",
        "justify-self", "align-self", "justify-content", "align-content", "justify-items", "align-items",
        "visibility", "opacity", "background", "bg", "class",
        "left", "top", "anchor"
    };
}

internal static partial class StyleParserRegexes
{
    [System.Text.RegularExpressions.GeneratedRegex(@"/\*.*?\*/", System.Text.RegularExpressions.RegexOptions.Singleline)]
    internal static partial System.Text.RegularExpressions.Regex CommentRegex();
    
    [System.Text.RegularExpressions.GeneratedRegex(@"([#.]?[a-zA-Z0-9_*\-,\s]+)\s*\{([^}]*)\}")]
    internal static partial System.Text.RegularExpressions.Regex SelectorRegex();
    
    [System.Text.RegularExpressions.GeneratedRegex(@"([a-zA-Z0-9\-]+)\s*:\s*([^;}]+)")]
    internal static partial System.Text.RegularExpressions.Regex PropertyRegex();
}
