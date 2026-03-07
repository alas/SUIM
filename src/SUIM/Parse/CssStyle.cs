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
        // Merge styles from all matching selectors in order of precedence (low to high)
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
            var classes = elementClass.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var className in classes)
            {
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

        // Inline sttributes (highest precedence, overrides all)        
        foreach (var attr in attributes?.Where(x => !ApplyToElement_skipped.Contains(x.Name.LocalName)).ToList() ?? [])
        {
            mergedProperties[attr.Name.LocalName] = attr.Value;
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
        bool willWrapWithBG = properties.Keys.Any(k => k.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase) || k.Equals("background-image", StringComparison.OrdinalIgnoreCase));

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
            else if ((willWrapWithBorder || willWrapWithScroll || willWrapWithBG) && IsLayoutAttribute(propName))
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

        // Apply regular attributes to the element (inner)
        foreach (var kvp in regularAttrs)
        {
            element.SetAttribute(kvp.Key, kvp.Value);
        }

        // Handle scroll wrapper
        if (scrollAttr.Count > 0)
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
                    scroll.SetAttribute(kvp.Key, kvp.Value);
                }
                // Fallback: also apply explicit width/height from properties if present (defensive)
                if (properties.TryGetValue("width", out var w)) scroll.SetAttribute("width", w);
                if (properties.TryGetValue("height", out var h)) scroll.SetAttribute("height", h);

                scroll.AddChild(element, null);
                element = scroll;
            }
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

            border.AddChild(element, null);
            element = border;
        }

        // Handle BackgroundImage wrapper (applied last to be the outermost wrapper)
        if (properties.TryGetValue("backgroundimage", out var bgImgAttr) || properties.TryGetValue("background-image", out bgImgAttr))
        {
            var bg = new BackgroundImage();
            bg.SetAttribute("backgroundimage", bgImgAttr);

            foreach (var kvp in wrapperAttrs)
            {
                bg.SetAttribute(kvp.Key, kvp.Value);
            }
            if (properties.TryGetValue("width", out var w)) bg.SetAttribute("width", w);
            if (properties.TryGetValue("height", out var h)) bg.SetAttribute("height", h);

            bg.AddChild(element, null);
            element = bg;
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
    [System.Text.RegularExpressions.GeneratedRegex(@"([#.]?[a-zA-Z0-9_*\-,\s]+)\s*\{([^}]*)\}")]
    internal static partial System.Text.RegularExpressions.Regex SelectorRegex();
    
    [System.Text.RegularExpressions.GeneratedRegex(@"([a-zA-Z0-9\-]+)\s*:\s*([^;}]+)")]
    internal static partial System.Text.RegularExpressions.Regex PropertyRegex();
}
