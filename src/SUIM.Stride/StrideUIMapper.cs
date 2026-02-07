namespace SUIM.StrideIntegration;

using System;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using StrideElement = Stride.UI.UIElement;
using StrideGrid = Stride.UI.Panels.Grid;

public class StrideUIMapper
{
    public UIPage Map(Components.UIElement root, object model)
    {
        var page = new UIPage
        {
            RootElement = MapElement(root)
        };
        return page;
    }

    private StrideElement MapElement(Components.UIElement element)
    {
        StrideElement strideElement = element switch
        {
            Components.Button b => MapButton(b),
            Components.BaseText t => MapText(t),
            Components.Stack s => MapStack(s),
            Components.Grid g => MapGrid(g),
            Components.Input i => MapInput(i),
            Components.Image img => MapImage(img),
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

    private static StrideElement MapButton(Components.Button button)
    {
        var btn = new Button();
        return btn;
    }

    private static StrideElement MapText(Components.BaseText text)
    {
        var tb = new TextBlock
        {
            Text = text.Text ?? "",
            TextSize = text.FontSize > 0 ? text.FontSize : 14
        };

        //if (!string.IsNullOrEmpty(text.Font))
        //{
        //    tb.Font = null; 
        //}
        
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

    private static StrideElement MapInput(Components.Input input)
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

        // Optionally map Stretch if Stride's Image supports a similar property later
        return img;
    }

    private StrideGrid MapGrid(Components.Grid grid)
    {
        var g = new StrideGrid();
        
        // Parse Rows/Columns
        // SUIM: "100, auto, *"
        /*
        if (!string.IsNullOrEmpty(grid.Rows))
        {
            var rows = grid.Rows.Split(',');
            foreach (var r in rows)
            {
                g.RowDefinitions.Add(new GridRowDefinition { Height = ParseGridLength(r) });
            }
        }

        if (!string.IsNullOrEmpty(grid.Columns))
        {
            var cols = grid.Columns.Split(',');
            foreach (var c in cols)
            {
                g.ColumnDefinitions.Add(new GridColumnDefinition { Width = ParseGridLength(c) });
            }
        }
        */

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

    /*
    private GridLength ParseGridLength(string val)
    {
        val = val.Trim();
        if (val == "auto") return GridLength.Auto;
        if (val.EndsWith("*"))
        {
            var starVal = val.TrimEnd('*');
            float f = string.IsNullOrEmpty(starVal) ? 1.0f : float.Parse(starVal);
            return new GridLength(f, GridUnitType.Star);
        }
        return new GridLength(float.Parse(val), GridUnitType.Absolute);
    }
    */

    private static void ApplyCommonProperties(Components.UIElement suim, StrideElement stride)
    {
        stride.Name = suim.Id;
        stride.Opacity = suim.Opacity;
        stride.Visibility = suim.Visibility == "hidden" ? Visibility.Hidden : (suim.Visibility == "collapse" ? Visibility.Collapsed : Visibility.Visible);
        
        // Start simple with margins/padding parsing
        if (suim.Margin != null) stride.Margin = ParseThickness(suim.Margin);
        if (suim.Padding != null && stride is ContentControl cc) cc.Padding = ParseThickness(suim.Padding); // Only ContentControl has padding in Stride basic? No, wrappers do.

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

        // Size
        if (suim.Width != null)
        {
             if (float.TryParse(suim.Width, out float w)) stride.Width = w;
             // Handle % or other units later
        }
        if (suim.Height != null)
        {
             if (float.TryParse(suim.Height, out float h)) stride.Height = h;
        }

        // Background
        if (suim.Background != null)
        {
            stride.BackgroundColor = ParseColor(suim.Background);
        }
    }

    private static Thickness ParseThickness(string value)
    {
        // Simplistic parser: "10" "10,20" "10,20,30,40"
        var parts = value.Split(',').Select(s => float.Parse(s.Trim())).ToArray();
        return parts.Length switch
        {
            1 => new Thickness(parts[0], parts[0], parts[0], parts[0]),
            2 => new Thickness(parts[0], parts[1], parts[0], parts[1]), // L, T, R, B
            4 => new Thickness(parts[0], parts[1], parts[2], parts[3]),
            _ => new Thickness(0, 0, 0, 0)
        };
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
        var success = Enum.TryParse<Color>(colorStr, out var namedColor);
        if (success) return namedColor;

        return Color.White;
    }
}
