namespace SUIM.Tests.Layout;

using Xunit;
using SUIM.Parse;
using SUIM.Parse.Components;

public class ContentAlignmentTests
{
    [Fact]
    public void Div_CentersChildrenVerticallyAndHorizontally()
    {
        var markup = """
            <div style="width:200px; height:200px; justify-content:center; align-items:center; display:flex; flex-direction:column;">
                <label style="width:100px; height:20px;">text</label>
                <label style="width:100px; height:20px;">text</label>
                <label style="width:100px; height:20px;">text</label>
            </div>
            """;
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);
        var label1 = (Label)element.Children[0];
        var label2 = (Label)element.Children[1];
        var label3 = (Label)element.Children[2];

        // Total content height = 20 * 3 = 60
        // Vertical offset = (200 - 60) / 2 = 70
        Assert.Equal(70, label1.GetTop());
        Assert.Equal(90, label2.GetTop());
        Assert.Equal(110, label3.GetTop());

        // Horizontal offset = (200 - 100) / 2 = 50
        Assert.Equal(50, label1.GetLeft());
        Assert.Equal(50, label2.GetLeft());
        Assert.Equal(50, label3.GetLeft());
    }

    [Fact]
    public void Div_ChildAlignmentOverridesParentContentAlignment()
    {
        var markup = """
            <div style="width:200px; height:200px; align-items:center; display:flex; flex-direction:column">
                <label style="width:100px; height:20px; AlignSelf:flex-end"></label>
                <label style="width:100px; height:20px"></label>
            </div>
            """;
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);
        var label1 = (Label)element.Children[0];
        var label2 = (Label)element.Children[1]; // Should use parent Center

        // label1 is Right: 200 - 100 = 100
        Assert.Equal(100, label1.GetLeft());
        // label2 uses parent Center: (200 - 100) / 2 = 50
        Assert.Equal(50, label2.GetLeft());
    }

    [Fact]
    public void Div_SupportsCHAlignAndCVAlignAttributes()
    {
        var div = new Div();
        div.SetAttribute("justify-content", "center");
        div.SetAttribute("align-items", "flex-end");

        Assert.Equal("center", div.GetAttribute("justifycontent"));
        Assert.Equal("flex-end", div.GetAttribute("alignitems"));
    }

    [Fact]
    public void Overlay_AlignsChildren()
    {
        var markup = """
            <overlay style="width:500px; height:500px; display:flex; justify-content:center; align-items:center;">
                <label style="width:100px; height:50px;"></label>
            </overlay>
            """;
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(500, 500);
        var label = (Label)element.Children[0];
        
        // Center horizontal: (500 - 100) / 2 = 200
        Assert.Equal(200, label.GetLeft());
        // Center vertical: (500 - 50) / 2 = 225
        Assert.Equal(225, label.GetTop());
    }

    [Fact]
    public void Child_WithUnspecifiedAlignment_InheritsParentContentAlignment()
    {
        var markup = """
            <div style="width:200px; height:200px; justify-content:center; Align-Items:center">
                <label style="width:100px; height:20px"></label>
            </div>
            """;
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);
        var label = (Label)element.Children[0];

        // HorizontalAlignment and VerticalAlignment are Unspecified by default

        // (200 - 100) / 2 = 50
        Assert.Equal(50, label.GetLeft());
        // (200 - 20) / 2 = 90
        Assert.Equal(90, label.GetTop());
    }

    [Fact]
    public void Child_WithUnspecifiedAlignment_DefaultsToLeftTopIfParentHasNoContentAlignment()
    {
        var markup = """
            <div style="width:200px; height:200px">
                <label style="width:100px; height:20px"></label>
            </div>
            """;
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);
        var label = (Label)element.Children[0];

        Assert.Equal(0, label.GetLeft()); // Defaults to Left
        Assert.Equal(0, label.GetTop()); // Defaults to Top
    }
}
