namespace SUIM.Tests;

using Xunit;
using SUIM.Layout;
using SUIM.Components;

public class LayoutTests
{
    [Fact]
    public void LayoutEngine_MeasuresStackWithPixels()
    {
        var layoutEngine = new LayoutEngine();
        var stack = new Stack { Orientation = Orientation.Vertical, Spacing = 10 };
        var child1 = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(50, UnitType.Pixels) };
        var child2 = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(30, UnitType.Pixels) };
        
        stack.AddChild(child1, null);
        stack.AddChild(child2, null);
        
        var context = new LayoutContext(16, 200, 200);
        var result = layoutEngine.Layout(stack, context);
        
        Assert.Equal(100, result.Width);
        Assert.Equal(90, result.Height); // 50 + 30 + 10 spacing
    }
    
    [Fact]
    public void LayoutEngine_MeasuresStackWithStarUnits()
    {
        var layoutEngine = new LayoutEngine();
        var stack = new Stack { Orientation = Orientation.Horizontal, Spacing = 0 };
        var child1 = new Label { Width = new UnitValue(1, UnitType.Star), Height = new UnitValue(50, UnitType.Pixels) };
        var child2 = new Label { Width = new UnitValue(2, UnitType.Star), Height = new UnitValue(50, UnitType.Pixels) };
        
        stack.AddChild(child1, null);
        stack.AddChild(child2, null);
        
        var context = new LayoutContext(16, 300, 100);
        var result = layoutEngine.Layout(stack, context);
        
        Assert.Equal(300, result.Width);
        Assert.Equal(50, result.Height);
    }
    
    [Fact]
    public void LayoutEngine_MeasuresGridWithMixedUnits()
    {
        var layoutEngine = new LayoutEngine();
        var grid = new Grid { Columns = "100, *", Rows = "50, *" };
        var child1 = new Label { Width = new UnitValue(50, UnitType.Pixels), Height = new UnitValue(25, UnitType.Pixels) };
        var child2 = new Label { Width = new UnitValue(1, UnitType.Star), Height = new UnitValue(1, UnitType.Star) };
        
        grid.AddChild(child1, null);
        grid.AddChild(child2, null);
        
        var context = new LayoutContext(16, 300, 200);
        var result = layoutEngine.Layout(grid, context);
        
        Assert.Equal(300, result.Width);
        Assert.Equal(200, result.Height);
    }
    
    [Fact]
    public void LayoutEngine_MeasuresDivWithAnchor()
    {
        var layoutEngine = new LayoutEngine();
        var div = new Div { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(50, UnitType.Pixels), Anchor = Anchor.TopRight };
        var child = new Label { Width = new UnitValue(50, UnitType.Pixels), Height = new UnitValue(25, UnitType.Pixels) };
        
        div.AddChild(child, null);
        
        var context = new LayoutContext(16, 400, 300);
        var result = layoutEngine.Layout(div, context);
        
        Assert.Equal(100, result.Width);
        Assert.Equal(50, result.Height);
    }
    
    [Fact]
    public void LayoutEngine_MeasuresWindow()
    {
        var layoutEngine = new LayoutEngine();
        var window = new Window();
        var child = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(50, UnitType.Pixels) };
        
        window.AddChild(child, null);
        
        var context = new LayoutContext(16, 800, 600);
        var result = layoutEngine.Layout(window, context);
        
        Assert.Equal(100, result.Width);
        Assert.Equal(50, result.Height);
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
    public void UnitValue_ParseStar()
    {
        var unit = UnitValue.Parse("2*");
        Assert.Equal(2, unit.Value);
        Assert.Equal(UnitType.Star, unit.Type);
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
        var context = new LayoutContext(16, 100, 100);
        var unit = new UnitValue(100, UnitType.Pixels);
        var pixels = unit.ToPixels(context);
        Assert.Equal(100, pixels);
    }
    
    [Fact]
    public void UnitConverter_ConvertRem()
    {
        var context = new LayoutContext(16, 100, 100);
        var unit = new UnitValue(2, UnitType.Rem);
        var pixels = unit.ToPixels(context);
        Assert.Equal(32, pixels); // 2 * 16
    }
    
    [Fact]
    public void UnitConverter_ConvertEm()
    {
        var context = new LayoutContext(16, 100, 100) { CurrentFontSize = 20 };
        var unit = new UnitValue(1.5f, UnitType.Em);
        var pixels = unit.ToPixels(context);
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