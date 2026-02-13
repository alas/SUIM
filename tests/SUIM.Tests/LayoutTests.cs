namespace SUIM.Tests;

using Xunit;
using SUIM.Components;
using SUIM.Layout;

public class LayoutTests
{
    [Fact]
    public void LayoutEngine_MeasuresStackWithPixels()
    {
        var stack = new Stack { Orientation = Orientation.Vertical, Spacing = 10, Width = UnitValue.Auto, Height = UnitValue.Auto };
        var child1 = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(50, UnitType.Pixels) };
        var child2 = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(30, UnitType.Pixels) };
        
        stack.AddChild(child1, null);
        stack.AddChild(child2, null);
        
        LayoutEngine.Layout(stack, 16, 200, 200);
        
        Assert.Equal(100, stack.ActualWidth);
        Assert.Equal(90, stack.ActualHeight); // 50 + 30 + 10 spacing
    }
    
    [Fact]
    public void LayoutEngine_MeasuresStackWithStarUnits()
    {
        var stack = new Stack { Orientation = Orientation.Horizontal, Spacing = 0, Width = UnitValue.OneFR, Height = UnitValue.Auto };
        var child1 = new Label { Width = new UnitValue(1, UnitType.Fr), Height = new UnitValue(50, UnitType.Pixels) };
        var child2 = new Label { Width = new UnitValue(2, UnitType.Fr), Height = new UnitValue(50, UnitType.Pixels) };
        
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
        var child1 = new Label { Width = new UnitValue(50, UnitType.Pixels), Height = new UnitValue(25, UnitType.Pixels) };
        var child2 = new Label { Width = new UnitValue(1, UnitType.Fr), Height = new UnitValue(1, UnitType.Fr) };
        
        grid.AddChild(child1, null);
        grid.AddChild(child2, null);
        
        LayoutEngine.Layout(grid, 16, 300, 200);
        
        Assert.Equal(300, grid.ActualWidth);
        Assert.Equal(200, grid.ActualHeight);
    }
    
    [Fact]
    public void LayoutEngine_MeasuresDivWithAnchor()
    {
        var div = new Div { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(50, UnitType.Pixels), Anchor = Anchor.Top };
        var child = new Label { Width = new UnitValue(50, UnitType.Pixels), Height = new UnitValue(25, UnitType.Pixels) };
        
        div.AddChild(child, null);
        
        LayoutEngine.Layout(div, 16, 400, 300);
        
        Assert.Equal(100, div.ActualWidth);
        Assert.Equal(50, div.ActualHeight);
    }
    
    [Fact]
    public void LayoutEngine_MeasuresWindow()
    {
        var window = new Window { Width = UnitValue.Auto, Height = UnitValue.Auto };
        var child = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(50, UnitType.Pixels) };
        
        window.AddChild(child, null);
        
        LayoutEngine.Layout(window, 16, 800, 600);
        
        Assert.Equal(100, window.ActualWidth);
        Assert.Equal(50, window.ActualHeight);
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
            Width = new UnitValue(2, UnitType.Rem),
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
            Width = new UnitValue(1.5f, UnitType.Em),
        };
        var pixels = div.ToPixels(div.Width);
        Assert.Equal(30, pixels); // 1.5 * 20
    }
    
    [Fact]
    public void StarUnitResolver_ResolveSimpleStars()
    {
        var starValues = new float[] { 1, 2 };
        var result = StarUnitResolver.ResolveStarUnits(starValues, 300);
        Assert.Equal(100, result[0]); // 1 * (300 / 3)
        Assert.Equal(200, result[1]); // 2 * (300 / 3)
    }
    
    [Fact]
    public void StarUnitResolver_ResolveSingleStar()
    {
        var starValues = new float[] { 1 };
        var result = StarUnitResolver.ResolveStarUnits(starValues, 200);
        Assert.Equal(200, result[0]); // 1 * (200 / 1)
    }
}