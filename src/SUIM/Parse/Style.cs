namespace SUIM;

using System;
using System.Collections.Generic;
using System.Linq;
using SUIM.Parse.Components;

/// <summary>
/// Handles CSS-like style parsing and application to UI elements.
/// Supports multiple selectors separated by commas and various selector types (tag, class, id, universal).
/// </summary>
public static class Style
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
                var propValue = propMatch.Groups[2].Value.Trim().Trim('"');
                properties[propName] = propValue;
            }

            if (properties.Count > 0)
            {
                // Apply the same properties to all selectors, merging with existing properties
                foreach (var selector in selectors)
                {
                    if (!styles.ContainsKey(selector))
                    {
                        styles[selector] = new Dictionary<string, string>();
                    }

                    // Merge properties: new properties override existing ones with the same name
                    foreach (var kvp in properties)
                    {
                        styles[selector][kvp.Key] = kvp.Value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies styles to an element based on CSS-like precedence rules.
    /// Precedence order: universal (*) < tag < class < id
    /// </summary>
    internal static UIElement ApplyToElement(UIElement element, Dictionary<string, Dictionary<string, string>> styles)
    {
        var elementTag = element.TagName;
        var elementId = element.GetAttribute("id");
        var elementClass = element.GetAttribute("class");

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

        if (mergedProperties.Count <= 0) return element;

        return ApplyPropertiesToElement(element, mergedProperties);
    }

    /// <summary>
    /// Applies individual style properties to an element, handling special cases like
    /// border, scroll, and background image wrappers.
    /// </summary>
    internal static UIElement ApplyPropertiesToElement(UIElement element, Dictionary<string, string> properties)
    {
        // Extract border and scroll attributes for special handling
        string? borderAttr = null;
        string? scrollAttr = null;
        var regularAttrs = new Dictionary<string, string>();
        var wrapperAttrs = new Dictionary<string, string>();
        // Pre-check whether this style will create a wrapper so layout attributes can be routed to it.
        bool willWrapWithBorder = properties.Keys.Any(k => k.Equals("border", StringComparison.OrdinalIgnoreCase));
        bool willWrapWithScroll = properties.Keys.Any(k => k.Equals("scroll", StringComparison.OrdinalIgnoreCase));
        bool willWrapWithBG = properties.Keys.Any(k => k.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase));

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
            else if (propName.Equals("backgroundimage", StringComparison.OrdinalIgnoreCase))
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

        // Handle BackgroundImage wrapper (applied last to be the outermost wrapper)
        if (properties.TryGetValue("backgroundimage", out var bgImgAttr))
        {
            var bg = new BackgroundImage();
            bg.SetAttribute("backgroundimage", bgImgAttr);

            foreach (var kvp in wrapperAttrs)
            {
                bg.SetAttribute(kvp.Key, kvp.Value);
            }
            if (properties.TryGetValue("width", out var w)) bg.SetAttribute("width", w);
            if (properties.TryGetValue("height", out var h)) bg.SetAttribute("height", h);

            element.Width ??= "auto";
            element.Height ??= "auto";
            bg.AddChild(element, null);
            element = bg;
        }

        return element;
    }

    private static readonly HashSet<string> LayoutAttributeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "width", "height", "padding", "margin",
        "justify-self", "align-self",
        "visibility", "opacity", "background", "bg", "class",
        "left", "top", "z-index", "anchor"
    };

    private static bool IsLayoutAttribute(string name) => LayoutAttributeNames.Contains(name);
}

internal static partial class StyleParserRegexes
{
    [System.Text.RegularExpressions.GeneratedRegex(@"([#.]?[a-zA-Z0-9_*\-,\s]+)\s*\{([^}]*)\}")]
    internal static partial System.Text.RegularExpressions.Regex SelectorRegex();
    
    [System.Text.RegularExpressions.GeneratedRegex(@"([a-zA-Z0-9\-]+)\s*:\s*([^;}]+)")]
    internal static partial System.Text.RegularExpressions.Regex PropertyRegex();
}
