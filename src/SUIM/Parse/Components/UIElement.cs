namespace SUIM.Parse.Components;

using System.Text;
using System.Xml.Linq;
using SUIM.Flexbox;

public abstract class UIElement
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public string? Anchor { get; set; }
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public string? Opacity { get; set; }
    public string? ReadOnly { get; set; }
    public string? StopClicks { get; set; }
    public string? Visibility { get; set; }

    // Text related properties (for elements that support text)
    public string? Font { get; set; }
    public string? FontSize { get; set; }

    // Internal properties to the engine - not directly settable via markup attributes
    public string TagName { get; }
    public bool IsComponentRoot { get; set; }
    public UIElement? Parent { get; set; }
    public List<UIElement> Children { get; } = [];
    public List<BindingDefinition> Bindings { get; } = [];
    public dynamic? Model { get; set; }
    public Dictionary<string, string> Events { get; set; } = [];
    internal Node Node { get; }

    public UIElement(string tagName)
    {
        TagName = tagName.ToLowerInvariant();
        Node = new Node
        {
            Context = this
        };
    }

    #region Children

    public virtual void AddChild(UIElement child, XElement? element)
    {
        child.Parent = this;
        Children.Add(child);

        Node.AddChild(child.Node);
    }

    public virtual void RemoveChild(UIElement child)
    {
        child.Parent = null;
        Children.Remove(child);

        Node.RemoveChild(child.Node);
    }

    public virtual void ClearChildren()
    {
        foreach (var child in Children)
        {
            child.Parent = null;
        }
        Children.Clear();

        Node.Children.Clear();
    }

    #endregion

    #region Layout

    public void CalculateLayout(int parentWidth, int parentHeight, Direction parentDirection = Direction.LTR)
    {
        ApplySUIMLayout();
        Node.CalculateLayout(parentWidth, parentHeight, parentDirection);
    }
    
    internal virtual void ApplySUIMLayout()
    {
        ValidateLayout();

        foreach (var child in Children)
        {
            child.ApplySUIMLayout();
        }
    }

    private void ValidateLayout()
    {
        // Warn about deep percentages
        var attrs = new string[] { "width", "height" };
        foreach (var attr in attrs)
        {
            var attrValue = Node.nodeStyle[attr];
            if (attrValue.Contains('%') == true && GetDepth() > 3)
            {
                Console.WriteLine($"WARNING: <{TagName}> uses percentage {attr} at depth {GetDepth()}. Deep percentage {attr}s may not work correctly.");
            }
        }
    }

    private int GetDepth()
    {
        int depth = 0;
        var current = Parent;
        while (current != null)
        {
            depth++;
            current = current.Parent;
        }
        return depth;
    }

    #endregion

    #region Attributes

    public virtual void SetAttribute(string name, object? value)
    {
        if (name.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            Id = value as string;
        }
        else if (name.Equals("visibility", StringComparison.OrdinalIgnoreCase))
        {
            Visibility = value as string;
        }
        else if (name.Equals("anchor", StringComparison.OrdinalIgnoreCase))
        {
            Anchor = value as string;
        }
        else if (name.Equals("bg", StringComparison.OrdinalIgnoreCase) || name.Equals("background", StringComparison.OrdinalIgnoreCase) || name.Equals("backgroundcolor", StringComparison.OrdinalIgnoreCase))
        {
            BackgroundColor = value as string ?? value?.ToString();
        }
        else if (name.Equals("class", StringComparison.OrdinalIgnoreCase))
        {
            Class = value as string;
        }
        else if (name.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            Color = value as string;
        }
        else if (name.Equals("font", StringComparison.OrdinalIgnoreCase))
        {
            Font = value as string;
        }
        else if (name.Equals("fontsize", StringComparison.OrdinalIgnoreCase) || name.Equals("font-size", StringComparison.OrdinalIgnoreCase))
        {
            FontSize = value as string;
        }
        else if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            var handlerName = value as string;
            var eventName = name[2..];
            Events[eventName] = handlerName ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("opacity", StringComparison.OrdinalIgnoreCase))
        {
            Opacity = value as string;
        }
        else if (name.Equals("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            (this as IPlaceholder)?.Placeholder = value as string;
        }
        else if (name.Equals("readonly", StringComparison.OrdinalIgnoreCase))
        {
            ReadOnly = value as string;
        }
        else if (name.Contains('.'))
        {
            // ignore parent properties
        }
        else if (value is string s)
        {
            if (s.StartsWith('@'))
            {
                // ignore
            }
            else if (AllProperties.TryGetValue(name, out var normalized))
            {
                Node.nodeStyle[normalized] = s;
            }
            else
            {
                Console.WriteLine($"Not recognized property: ${s}");
            }
        }
        else
        {
            Console.WriteLine($"Not string property value: ${value}");
        }
    }

    public string? GetAttribute(string attribute)
    {
        if (!AllProperties.TryGetValue(attribute, out var normalized)) return null;

        return Node.nodeStyle[normalized];
    }

    public float GetLeft()
    {
        return Node.LayoutGetLeft();
    }

    public float GetX()
    {
        return Node.LayoutGetX();
    }

    public float GetTop()
    {
        return Node.LayoutGetTop();
    }

    public float GetY()
    {
        return Node.LayoutGetY();
    }

    public float GetWidth()
    {
        return Node.LayoutGetWidth();
    }

    public float GetWidth2()
    {
        return Node.Layout.width;
    }

    public float GetHeight()
    {
        return Node.LayoutGetHeight();
    }

    public float GetHeight2()
    {
        return Node.Layout.height;
    }

    public dynamic? GetEffectiveModel()
    {
        if (Model != null) return Model;
        if (IsComponentRoot) return null;
        return Parent?.GetEffectiveModel();
    }

    public static readonly Dictionary<string, string> AllProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            // Flex container                            
            { "flex-direction",                                 "flex-direction" },
            { "flex-wrap",                                      "flex-wrap" },
            { "flex-flow",                                      "flex-flow" },
            { "justify-content",                                "justify-content" },
            { "align-items",                                    "align-items" },
            { "align-content",                                  "align-content" },
            { "gap",                                            "gap" },
            { "row-gap",                                        "row-gap" },
            { "column-gap",                                     "column-gap" },
            //
            { "flexdirection",                                 "flex-direction" },
            { "flexwrap",                                      "flex-wrap" },
            { "flexflow",                                      "flex-flow" },
            { "justifycontent",                                "justify-content" },
            { "alignitems",                                    "align-items" },
            { "aligncontent",                                  "align-content" },
            { "rowgap",                                        "row-gap" },
            { "columngap",                                     "column-gap" },

            // Flex items
            { "flex",                                           "flex" },
            { "flex-grow",                                      "flex-grow" },
            { "flex-shrink",                                    "flex-shrink" },
            { "flex-basis",                                     "flex-basis" },
            { "align-self",                                     "align-self" },
            { "order",                                          "order" },
            //
            { "flexgrow",                                      "flex-grow" },
            { "flexshrink",                                    "flex-shrink" },
            { "flexbasis",                                     "flex-basis" },
            { "alignself",                                     "align-self" },

            // Positioning
            { "position",                                       "position" },
            { "top",                                            "top" },
            { "right",                                          "right" },
            { "bottom",                                         "bottom" },
            { "left",                                           "left" },
            { "inset",                                          "inset" },
            { "inset-block",                                    "inset-block" },
            { "inset-inline",                                   "inset-inline" },
            //
            { "insetblock",                                    "inset-block" },
            { "insetinline",                                   "inset-inline" },

            // Margin
            { "margin",                                         "margin" },
            { "margin-top",                                     "margin-top" },
            { "margin-right",                                   "margin-right" },
            { "margin-bottom",                                  "margin-bottom" },
            { "margin-left",                                    "margin-left" },
            { "margin-inline",                                  "margin-inline" },
            { "margin-inline-start",                            "margin-inline-start" },
            { "margin-inline-end",                              "margin-inline-end" },
            { "margin-block",                                   "margin-block" },
            { "margin-block-start",                             "margin-block-start" },
            { "margin-block-end",                               "margin-block-end" },
            //
            { "margintop",                                     "margin-top" },
            { "marginright",                                   "margin-right" },
            { "marginbottom",                                  "margin-bottom" },
            { "marginleft",                                    "margin-left" },
            { "margininline",                                  "margin-inline" },
            { "margininlinestart",                            "margin-inline-start" },
            { "margininlineend",                              "margin-inline-end" },
            { "marginblock",                                   "margin-block" },
            { "marginblockstart",                             "margin-block-start" },
            { "marginblockend",                               "margin-block-end" },

            // Padding
            { "padding",                                        "padding" },
            { "padding-top",                                    "padding-top" },
            { "padding-right",                                  "padding-right" },
            { "padding-bottom",                                 "padding-bottom" },
            { "padding-left",                                   "padding-left" },
            { "padding-inline",                                 "padding-inline" },
            { "padding-inline-start",                           "padding-inline-start" },
            { "padding-inline-end",                             "padding-inline-end" },
            { "padding-block",                                  "padding-block" },
            { "padding-block-start",                            "padding-block-start" },
            { "padding-block-end",                              "padding-block-end" },
            //
            { "paddingtop",                                    "padding-top" },
            { "paddingright",                                  "padding-right" },
            { "paddingbottom",                                 "padding-bottom" },
            { "paddingleft",                                   "padding-left" },
            { "paddinginline",                                 "padding-inline" },
            { "paddinginlinestart",                           "padding-inline-start" },
            { "paddinginlineend",                             "padding-inline-end" },
            { "paddingblock",                                  "padding-block" },
            { "paddingblockstart",                            "padding-block-start" },
            { "paddingblockend",                              "padding-block-end" },

            // Border (width only matters)
            { "border",                                         "border" },
            { "border-top",                                     "border-top" },
            { "border-right",                                   "border-right" },
            { "border-bottom",                                  "border-bottom" },
            { "border-left",                                    "border-left" },
            { "border-width",                                   "border-width" },
            { "border-top-width",                               "border-top-width" },
            { "border-right-width",                             "border-right-width" },
            { "border-bottom-width",                            "border-bottom-width" },
            { "border-left-width",                              "border-left-width" },
            //
            { "bordertop",                                     "border-top" },
            { "borderright",                                   "border-right" },
            { "borderbottom",                                  "border-bottom" },
            { "borderleft",                                    "border-left" },
            { "borderwidth",                                   "border-width" },
            { "bordertopwidth",                               "border-top-width" },
            { "borderrightwidth",                             "border-right-width" },
            { "borderbottomwidth",                            "border-bottom-width" },
            { "borderleftwidth",                              "border-left-width" },

            // Size
            { "width",                                          "width" },
            { "height",                                         "height" },
            { "min-width",                                      "min-width" },
            { "min-height",                                     "min-height" },
            { "max-width",                                      "max-width" },
            { "max-height",                                     "max-height" },
            { "aspect-ratio",                                   "aspect-ratio" },
            //
            { "minwidth",                                      "min-width" },
            { "minheight",                                     "min-height" },
            { "maxwidth",                                      "max-width" },
            { "maxheight",                                     "max-height" },
            { "aspectratio",                                   "aspect-ratio" },

            // Layout behavior
            { "display",                                        "display" },
            { "overflow",                                       "overflow" },
            { "box-sizing",                                     "box-sizing" },
            //
            { "boxsizing",                                     "box-sizing" },

            // Direction
            { "direction",                                      "direction" },
        };

    #endregion

    public void AppendDebugString(StringBuilder sb, int indent = 0)
    {
        sb.Append(new string(' ', indent * 2));
        sb.AppendLine($"{TagName}{(Id != null ? ":" + Id : "")} [{GetWidth()}x{GetHeight()}] @ ({GetLeft()}, {GetTop()}){(this is Text t ? " " + t.Value : "")}");

        foreach (var child in Children)
        {
            child.AppendDebugString(sb, indent + 1);
        }
    }
}

public record BindingDefinition(string TargetPropertyName, string ModelPropertyName);

public abstract class LayoutElement(string tagName) : UIElement(tagName)
{
    public string? Gap { get; set; }
    public string? RowGap { get; set; }
    public string? ColumnGap { get; set; }
    public bool Clip { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("gap", StringComparison.OrdinalIgnoreCase))
        {
            Gap = value as string;
        }
        else if (name.Equals("row-gap", StringComparison.OrdinalIgnoreCase) || name.Equals("rowgap", StringComparison.OrdinalIgnoreCase))
        {
            RowGap = value as string;
        }
        else if (name.Equals("column-gap", StringComparison.OrdinalIgnoreCase) || name.Equals("columngap", StringComparison.OrdinalIgnoreCase))
        {
            ColumnGap = value as string;
        }
        else if (name.Equals("clip", StringComparison.OrdinalIgnoreCase))
        {
            Clip = value is bool b ? b : Convert.ToBoolean(value);
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}

public interface IPlaceholder
{
    string? Placeholder { get; set; }
}
