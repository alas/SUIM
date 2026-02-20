namespace SUIM.Tests;

using Xunit;
using SUIM.Components;
using SUIM.Layout;
using SUIM.Components.Attributes;

public class LayoutTests
{
    [Fact]
    public void LayoutEngine_MeasuresStackWithPixels()
    {
        var stack = new Stack { Orientation = Orientation.Vertical, Spacing = 10, Width = UnitValue.Auto.ToString(), Height = UnitValue.Auto.ToString() };
        var child1 = new Label { Width = new UnitValue(100, UnitType.Pixels).ToString(), Height = new UnitValue(50, UnitType.Pixels).ToString() };
        var child2 = new Label { Width = new UnitValue(100, UnitType.Pixels).ToString(), Height = new UnitValue(30, UnitType.Pixels).ToString() };
        
        stack.AddChild(child1, null);
        stack.AddChild(child2, null);
        
        LayoutEngine.Layout(stack, 16, 200, 200);
        
        Assert.Equal(100, stack.ActualWidth);
        Assert.Equal(90, stack.ActualHeight); // 50 + 30 + 10 spacing
    }
    
    [Fact]
    public void LayoutEngine_MeasuresStackWithFractionalUnits()
    {
        var stack = new Stack { Orientation = Orientation.Horizontal, Spacing = 0, Width = UnitValue.OneFR.ToString(), Height = UnitValue.Auto.ToString() };
        var child1 = new Label { Width = new UnitValue(1, UnitType.Fr).ToString(), Height = new UnitValue(50, UnitType.Pixels).ToString() };
        var child2 = new Label { Width = new UnitValue(2, UnitType.Fr).ToString(), Height = new UnitValue(50, UnitType.Pixels).ToString() };
        
        stack.AddChild(child1, null);
        stack.AddChild(child2, null);
        
        LayoutEngine.Layout(stack, 16, 300, 100);
        
        Assert.Equal(300, stack.ActualWidth);
        Assert.Equal(50, stack.ActualHeight);
    }
    
    [Fact]
    public void LayoutEngine_MeasuresGridWithMixedUnits()
    {
        var grid = new Grid { Columns = "100, fr", Rows = "50, fr" };
        var child1 = new Label { Width = new UnitValue(50, UnitType.Pixels).ToString(), Height = new UnitValue(25, UnitType.Pixels).ToString() };
        var child2 = new Label { Width = new UnitValue(1, UnitType.Fr).ToString(), Height = new UnitValue(1, UnitType.Fr).ToString() };
        
        grid.AddChild(child1, null);
        grid.AddChild(child2, null);
        
        LayoutEngine.Layout(grid, 16, 300, 200);
        
        Assert.Equal(300, grid.ActualWidth);
        Assert.Equal(200, grid.ActualHeight);
    }
    
    [Fact]
    public void LayoutEngine_MeasuresDivWithAnchor()
    {
        var div = new Div { Width = new UnitValue(100, UnitType.Pixels).ToString(), Height = new UnitValue(50, UnitType.Pixels).ToString(), Anchor = Anchor.Top.ToString() };
        var child = new Label { Width = new UnitValue(50, UnitType.Pixels).ToString(), Height = new UnitValue(25, UnitType.Pixels).ToString() };
        
        div.AddChild(child, null);
        
        LayoutEngine.Layout(div, 16, 400, 300);
        
        Assert.Equal(100, div.ActualWidth);
        Assert.Equal(50, div.ActualHeight);
    }
    
    [Fact]
    public void LayoutEngine_MeasuresWindow()
    {
        var grid = new Grid { Width = UnitValue.Auto.ToString(), Height = UnitValue.Auto.ToString() };
        var child = new Label { Width = new UnitValue(100, UnitType.Pixels).ToString(), Height = new UnitValue(50, UnitType.Pixels).ToString() };

        grid.AddChild(child, null);
        
        LayoutEngine.Layout(grid, 16, 800, 600);
        
        Assert.Equal(100, grid.ActualWidth);
        Assert.Equal(50, grid.ActualHeight);
    }
    
    [Fact]
    public void UnitValue_ParsePixels()
    {
        var unit = UnitValue.Parse("100");
        Assert.Equal(100, unit.Value);
        Assert.Equal(UnitType.Pixels, unit.Type);
    }
    
    [Fact]
    public void UnitValue_ParseRem()
    {
        var unit = UnitValue.Parse("2rem");
        Assert.Equal(2, unit.Value);
        Assert.Equal(UnitType.Rem, unit.Type);
    }
    
    [Fact]
    public void UnitValue_ParseEm()
    {
        var unit = UnitValue.Parse("1.5em");
        Assert.Equal(1.5f, unit.Value);
        Assert.Equal(UnitType.Em, unit.Type);
    }
    
    [Fact]
    public void UnitValue_ParseFractionalUnits()
    {
        var unit = UnitValue.Parse("2fr");
        Assert.Equal(2, unit.Value);
        Assert.Equal(UnitType.Fr, unit.Type);
    }
    
    [Fact]
    public void UnitValue_ParseAuto()
    {
        var unit = UnitValue.Parse("auto");
        Assert.Equal(0, unit.Value);
        Assert.Equal(UnitType.Auto, unit.Type);
    }
    
    [Fact]
    public void UnitConverter_ConvertPixels()
    {
        var unit = new UnitValue(100, UnitType.Pixels);
        Assert.Equal(100, unit.Value);
        Assert.Equal(UnitType.Pixels, unit.Type);
    }
    
    [Fact]
    public void UnitConverter_ConvertPixelsString()
    {
        var unit = UnitValue.Parse("100");
        Assert.Equal(100, unit.Value);
        Assert.Equal(UnitType.Pixels, unit.Type);
    }

    [Fact]
    public void UnitConverter_ConvertPixelsFloat()
    {
        var unit = UnitValue.FromObject(100f);
        Assert.Equal(100, unit.Value);
        Assert.Equal(UnitType.Pixels, unit.Type);
    }

    [Fact]
    public void UnitConverter_ConvertRem()
    {
        var div = new Div
        {
            RootFontSize = 16f,
            Width = new UnitValue(2, UnitType.Rem).ToString(),
        };
        var pixels = div.ToPixels(div.Width);
        Assert.Equal(32, pixels); // 2 * 16
    }
    
    [Fact]
    public void UnitConverter_ConvertEm()
    {
        var div = new Div
        {
            RootFontSize = 20f,
            Width = new UnitValue(1.5f, UnitType.Em).ToString(),
        };
        var pixels = div.ToPixels(div.Width);
        Assert.Equal(30, pixels); // 1.5 * 20
    }
    
    [Fact]
    public void FrUnitResolver_ResolveSimpleFractionalUnits()
    {
        var values = new float[] { 1, 2 };
        var result = FractionalUnitResolver.ResolveFractionalUnits(values, 300);
        Assert.Equal(100, result[0]); // 1 * (300 / 3)
        Assert.Equal(200, result[1]); // 2 * (300 / 3)
    }
    
    [Fact]
    public void FrUnitResolver_ResolveSingleFractionalUnit()
    {
        var values = new float[] { 1 };
        var result = FractionalUnitResolver.ResolveFractionalUnits(values, 200);
        Assert.Equal(200, result[0]); // 1 * (200 / 1)
    }

    [Fact]
    public void LayoutEngine_OverlaysShouldFillParentSize()
    {
        // Create a grid with explicit size (simulating main UI container)
        // When overlays are inside, they should fill the grid's dimensions
        var grid = new Grid();
        
        // Create main UI container (like buttonsUI in MainView)
        var mainUI = new Stack 
        { 
            Width = new UnitValue(400).ToString(), 
            Height = new UnitValue(300).ToString()
        };
        grid.AddChild(mainUI, null);
        
        // Create overlays - when grid has explicit size, overlays should fill it
        var overlay1 = new Overlay();
        var popupContent = new Grid
        {
            Width = new UnitValue(360).ToString(),
            Height = new UnitValue(180).ToString()
        };
        overlay1.AddChild(popupContent, null);
        grid.AddChild(overlay1, null);
        
        var overlay2 = new Overlay();
        overlay2.AddChild(new Label(), null);
        grid.AddChild(overlay2, null);
        
        // Layout with screen size
        LayoutEngine.Layout(grid, 16, 1280, 720);
        
        // Grid should size to its explicit dimensions
        Assert.Equal(1280, grid.ActualWidth);
        Assert.Equal(720, grid.ActualHeight);
        
        // Overlays should fill the grid container when it has explicit size
        // Add some tolerance for padding
        Assert.True(overlay1.ActualWidth >= 1270 && overlay1.ActualWidth <= 1280, 
            $"overlay1.ActualWidth was {overlay1.ActualWidth}, expected ~1280");
        Assert.True(overlay1.ActualHeight >= 710 && overlay1.ActualHeight <= 720,
            $"overlay1.ActualHeight was {overlay1.ActualHeight}, expected ~720");
    }
}