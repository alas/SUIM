namespace SUIM.Tests;

using Xunit;
using SUIM.Components;
using SUIM.Layout;

public class ContentAlignmentTests
{
    [Fact]
    public void Div_CentersChildrenVerticallyAndHorizontally()
    {
        var div = new Div { Width = new UnitValue(200, UnitType.Pixels), Height = new UnitValue(200, UnitType.Pixels) };
        div.ContentHorizontalAlignment = HorizontalAlignment.Center;
        div.ContentVerticalAlignment = VerticalAlignment.Center;

        var label1 = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(20, UnitType.Pixels) };
        var label2 = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(20, UnitType.Pixels) };
        var label3 = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(20, UnitType.Pixels) };

        div.AddChild(label1, null);
        div.AddChild(label2, null);
        div.AddChild(label3, null);

        LayoutEngine.Layout(div, 16, 200, 200);

        // Total content height = 20 * 3 = 60
        // Vertical offset = (200 - 60) / 2 = 70
        Assert.Equal(70, label1.ActualY);
        Assert.Equal(90, label2.ActualY);
        Assert.Equal(110, label3.ActualY);

        // Horizontal offset = (200 - 100) / 2 = 50
        Assert.Equal(50, label1.ActualX);
        Assert.Equal(50, label2.ActualX);
        Assert.Equal(50, label3.ActualX);
    }

    [Fact]
    public void Div_ChildAlignmentOverridesParentContentAlignment()
    {
        var div = new Div { Width = new UnitValue(200, UnitType.Pixels), Height = new UnitValue(200, UnitType.Pixels) };
        div.ContentHorizontalAlignment = HorizontalAlignment.Center;

        var label1 = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(20, UnitType.Pixels), HorizontalAlignment = HorizontalAlignment.Right };
        var label2 = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(20, UnitType.Pixels) }; // Should use parent Center

        div.AddChild(label1, null);
        div.AddChild(label2, null);

        LayoutEngine.Layout(div, 16, 200, 200);

        // label1 is Right: 200 - 100 = 100
        Assert.Equal(100, label1.ActualX);
        // label2 uses parent Center: (200 - 100) / 2 = 50
        Assert.Equal(50, label2.ActualX);
    }

    [Fact]
    public void Div_SupportsCHAlignAndCVAlignAttributes()
    {
        var div = new Div();
        div.SetAttribute("chalign", "center");
        div.SetAttribute("cvalign", "bottom");

        Assert.Equal(HorizontalAlignment.Center, div.ContentHorizontalAlignment);
        Assert.Equal(VerticalAlignment.Bottom, div.ContentVerticalAlignment);
    }
    
    [Fact]
    public void Overlay_AlignsChildren()
    {
        var overlay = new Overlay { Width = new UnitValue(500, UnitType.Pixels), Height = new UnitValue(500, UnitType.Pixels) };
        var label = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(50, UnitType.Pixels), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        
        overlay.AddChild(label, null);
        
        LayoutEngine.Layout(overlay, 16, 500, 500);
        
        // Center horizontal: (500 - 100) / 2 = 200
        Assert.Equal(200, label.ActualX);
        // Center vertical: (500 - 50) / 2 = 225
        Assert.Equal(225, label.ActualY);
    }
    [Fact]
    public void Child_WithUnspecifiedAlignment_InheritsParentContentAlignment()
    {
        var div = new Div { Width = new UnitValue(200, UnitType.Pixels), Height = new UnitValue(200, UnitType.Pixels) };
        div.ContentHorizontalAlignment = HorizontalAlignment.Center;
        div.ContentVerticalAlignment = VerticalAlignment.Center;

        var label = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(20, UnitType.Pixels) };
        // HorizontalAlignment and VerticalAlignment are Unspecified by default
        
        div.AddChild(label, null);
        LayoutEngine.Layout(div, 16, 200, 200);

        // (200 - 100) / 2 = 50
        Assert.Equal(50, label.ActualX);
        // (200 - 20) / 2 = 90
        Assert.Equal(90, label.ActualY);
    }

    [Fact]
    public void Child_WithUnspecifiedAlignment_DefaultsToLeftTopIfParentHasNoContentAlignment()
    {
        var div = new Div { Width = new UnitValue(200, UnitType.Pixels), Height = new UnitValue(200, UnitType.Pixels) };
        // div.ContentHorizontalAlignment and ContentVerticalAlignment are Unspecified by default

        var label = new Label { Width = new UnitValue(100, UnitType.Pixels), Height = new UnitValue(20, UnitType.Pixels) };
        
        div.AddChild(label, null);
        LayoutEngine.Layout(div, 16, 200, 200);

        Assert.Equal(0, label.ActualX); // Defaults to Left
        Assert.Equal(0, label.ActualY); // Defaults to Top
    }
}
