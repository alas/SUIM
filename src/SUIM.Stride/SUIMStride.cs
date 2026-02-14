namespace SUIM.StrideIntegration;

using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using System;
using StrideGrid = Stride.UI.Panels.Grid;

public class SUIMStride
{
    public ContentManager? ContentManager { get; init; }
    private readonly Dictionary<string, SpriteFont> Fonts = [];

    private readonly Dictionary<string, (Components.UIElement SuimRoot, UIElement StrideRoot, dynamic? Model)> _parseCache = [];

    public (UIElement, dynamic?) Parse(string markup, bool createNewInstance = false)
    {
        ArgumentNullException.ThrowIfNull(markup);

        // Return cached instance when available and caller doesn't request a new one
        lock (_parseCache)
        {
            if (_parseCache.TryGetValue(markup, out var cached))
            {
                if (!createNewInstance)
                {
                    return (cached.StrideRoot, cached.Model);
                }

                // createNewInstance==true -> return a fresh Stride tree by remapping the cached SUIM tree
                return (MapElement(cached.SuimRoot), cached.Model);
            }
        }

        // Not cached: parse markup, map and store the canonical instance
        var (suimRoot, model) = MarkupParser.Parse(markup);
        var strideRoot = MapElement(suimRoot);

        lock (_parseCache)
        {
            _parseCache[markup] = (suimRoot, strideRoot, model);
        }

        return createNewInstance ? (MapElement(suimRoot), model) : (strideRoot, model);
    }

    private UIElement MapElement(Components.UIElement element)
    {
        UIElement strideElement = element switch
        {
            Components.Button b => MapButton(b),
            Components.Text t => MapText(t),
            Components.Stack s => MapStack(s),
            Components.Grid g => MapGrid(g),
            Components.Input i => MapInput(i),
            Components.Image img => MapImage(img),
            Components.Border br => MapBorder(br),
            _ => new StrideGrid() // Fallback
        };

        ApplyCommonProperties(element, strideElement);
        
        // Handle Children for generic containers if not already handled
        if (strideElement is Panel panel && element.Children.Count > 0 && element is not Components.Grid) // Grid handles its own children
        {
            foreach (var child in element.Children)
            {
                panel.Children.Add(MapElement(child));
            }
        }
        else if (strideElement is ContentControl contentControl && element.Children.Count > 0)
        {
             if (element.Children.Count == 1)
             {
                 contentControl.Content = MapElement(element.Children[0]);
             }
             else
             {
                 // Create a stack panel to hold multiple children if the control can only hold one
                 var stack = new StackPanel { Orientation = Orientation.Vertical };
                 foreach (var child in element.Children)
                 {
                     stack.Children.Add(MapElement(child));
                 }
                 contentControl.Content = stack;
             }
        }

        return strideElement;
    }

    private static Button MapButton(Components.Button button)
    {
        var btn = new Button();
        return btn;
    }

    private TextBlock MapText(Components.Text text)
    {
        var tb = new TextBlock
        {
            Text = text.Value ?? "",
            TextSize = text.FontSize > 0f ? text.FontSize : 14f,
        };

        // Resolve SpriteFont by name (from style/attribute) using optional resolver.
        // Consumers (e.g. example app) should set SUIMStride.FontResolver = name => game.Content.Load<SpriteFont>(name);
        try
        {
            var fontName = text.Font ?? "StrideDefaultFont";
            SpriteFont? sf = Fonts.TryGetValue(fontName, out SpriteFont? value) ? value : null;
            if (sf == null && !Fonts.ContainsKey(fontName) && ContentManager != null)
            {
                sf = ContentManager?.Load<SpriteFont>(fontName);
                if (sf != null)
                {
                    Fonts[fontName] = sf;
                }
            }

            if (sf != null)
            {
                tb.Font = sf;
            }
        }
        catch
        {
            // If resolver fails, fall back to Stride default font silently.
        }
        
        if (text.Wrap)
        {
            tb.WrapText = true;
        }

        if (text.Color != null)
        {
             tb.TextColor = ParseColor(text.Color);
        }

        return tb;
    }

    private static UIElement MapInput(Components.Input input)
    {
        // Map based on the input type
        return input.Type switch
        {
            Components.InputType.Checkbox => new ToggleButton(),
            // Components.InputType.Radio => new ToggleButton(), // Stride doesn't have a direct RadioButton in all versions; use ToggleButton for now
            _ => new EditText
            {
                Text = input.Value ?? ""
            },
        };
    }

    private static ImageElement MapImage(Components.Image image)
    {
        var img = new ImageElement();

        if (!string.IsNullOrEmpty(image.Source))
        {
            //img.Source = image.Source;
        }

        img.StretchType = image.Stretch switch
        {
            Components.ImageStretch.Uniform => StretchType.Uniform,
            Components.ImageStretch.UniformToFill => StretchType.UniformToFill,
            Components.ImageStretch.Fill => StretchType.Fill,
            Components.ImageStretch.FillOnStretch => StretchType.FillOnStretch,
            _ => StretchType.None
        };

        return img;
    }

    private Border MapBorder(Components.Border border)
    {
        var borderElem = new Border
        {
            BorderThickness = ComponentsThicknessToStride(border.Thickness, border)
        };

        if (!string.IsNullOrEmpty(border.Color))
        {
            borderElem.BorderColor = ParseColor(border.Color);
        }

        // Handle Children
        if (border.Children.Count > 0)
        {
            if (border.Children.Count == 1)
            {
                borderElem.Content = MapElement(border.Children[0]);
            }
            else
            {
                throw new NotSupportedException("Border element in SUIM supports only one child element for now.");
            }
        }

        return borderElem;
    }

    private StrideGrid MapGrid(Components.Grid grid)
    {
        var g = new StrideGrid();

        foreach (var childContainer in grid.GridChildren)
        {
            var childStride = MapElement(childContainer.Element);
            childStride.SetGridRow(childContainer.Row);
            childStride.SetGridColumn(childContainer.Column);
            childStride.SetGridRowSpan(childContainer.RowSpan);
            childStride.SetGridColumnSpan(childContainer.ColumnSpan);
            g.Children.Add(childStride);
        }

        return g;
    }

    private static StackPanel MapStack(Components.Stack stack)
    {
        return new StackPanel
        {
            Orientation = stack.Orientation == Components.Orientation.Horizontal 
                ? Orientation.Horizontal 
                : Orientation.Vertical
        };
    }

    private static void ApplyCommonProperties(Components.UIElement suim, UIElement stride)
    {
        stride.Name = suim.Id;
        stride.Opacity = suim.Opacity;
        stride.Visibility = suim.Visibility == "hidden" ? Visibility.Hidden : (suim.Visibility == "collapse" ? Visibility.Collapsed : Visibility.Visible);
        
        // Start simple with margins/padding parsing
        stride.Margin = ComponentsThicknessToStride(suim.Margin, suim);
        if (stride is ContentControl cc)
        {
            cc.Padding = ComponentsThicknessToStride(suim.Padding, suim); // Only ContentControl has padding in Stride basic? No, wrappers do.
        }

        // Alignment
        stride.HorizontalAlignment = suim.HorizontalAlignment switch
        {
            Components.HorizontalAlignment.Left => HorizontalAlignment.Left,
            Components.HorizontalAlignment.Center => HorizontalAlignment.Center,
            Components.HorizontalAlignment.Right => HorizontalAlignment.Right,
            Components.HorizontalAlignment.Stretch => HorizontalAlignment.Stretch,
            _ => HorizontalAlignment.Left
        };

        stride.VerticalAlignment = suim.VerticalAlignment switch
        {
            Components.VerticalAlignment.Top => VerticalAlignment.Top,
            Components.VerticalAlignment.Center => VerticalAlignment.Center,
            Components.VerticalAlignment.Bottom => VerticalAlignment.Bottom,
            Components.VerticalAlignment.Stretch => VerticalAlignment.Stretch,
            _ => VerticalAlignment.Top
        };

        var width = suim.ToPixels(suim.Width);
        if (width != 0f)
        {
             stride.Width = width;
        }

        var height = suim.ToPixels(suim.Height);
        if (height != 0f)
        {
             stride.Height = height;
        }

        if (suim.BackgroundColor != null)
        {
            stride.BackgroundColor = ParseColor(suim.BackgroundColor);
        }

        if (suim.StopClicks)
        {
            stride.CanBeHitByUser = true;
        }
    }

    private static Thickness ComponentsThicknessToStride(Layout.Thickness thickness, Components.UIElement suim)
    {
        return new Thickness(
            suim.ToPixels(thickness.Left),
            suim.ToPixels(thickness.Top),
            suim.ToPixels(thickness.Right),
            suim.ToPixels(thickness.Bottom));
    }

    private static Color ParseColor(string colorStr)
    {
        // Helper to parse hex or named colors
        if (colorStr.StartsWith('#'))
        {
             // #RRGGBB or #AARRGGBB
             string hex = colorStr.Substring(1);
             if (hex.Length == 6)
             {
                 return new Color(
                     Convert.ToByte(hex.Substring(0, 2), 16),
                     Convert.ToByte(hex.Substring(2, 2), 16),
                     Convert.ToByte(hex.Substring(4, 2), 16),
                     255);
             }
             else if (hex.Length == 8)
             {
                 return new Color(
                     Convert.ToByte(hex.Substring(2, 2), 16),
                     Convert.ToByte(hex.Substring(4, 2), 16),
                     Convert.ToByte(hex.Substring(6, 2), 16),
                     Convert.ToByte(hex.Substring(0, 2), 16));
             }
        }

        if (string.Equals(colorStr, "red", StringComparison.OrdinalIgnoreCase)) return Color.Red;
        if (string.Equals(colorStr, "green", StringComparison.OrdinalIgnoreCase)) return Color.Green;
        if (string.Equals(colorStr, "blue", StringComparison.OrdinalIgnoreCase)) return Color.Blue;
        if (string.Equals(colorStr, "black", StringComparison.OrdinalIgnoreCase)) return Color.Black;
        if (string.Equals(colorStr, "yellow", StringComparison.OrdinalIgnoreCase)) return Color.Yellow;
        if (string.Equals(colorStr, "cyan", StringComparison.OrdinalIgnoreCase)) return Color.Cyan;
        if (string.Equals(colorStr, "magenta", StringComparison.OrdinalIgnoreCase)) return Color.Magenta;
        if (string.Equals(colorStr, "transparent", StringComparison.OrdinalIgnoreCase)) return Color.Transparent;
        //if (string.Equals(colorStr, "white", StringComparison.OrdinalIgnoreCase)
        return Color.White;
    }
}
