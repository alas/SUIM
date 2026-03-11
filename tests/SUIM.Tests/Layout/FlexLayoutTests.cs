namespace SUIM.Tests.Layout;

using Xunit;
using SUIM.Parse;
using SUIM.Parse.Components;

public class FlexLayoutTests
{
    [Fact]
    public void FlexLayout_RowDirection_DistributesSpace()
    {
        var markup = """
        <div style="width:300px; height:100px; display:flex; flex-direction:row;">
            <div style="width:100px; height:50px"></div>
            <div style="height:50px; flex: 1;"></div>
        </div>
        """;//-grow: 1; flex-shrink: 1; flex-basis: 0%;
        var element = MarkupParser.Parse(markup);
        element.CalculateLayout(300, 100);
        var child1 = (Div)element.Children[0];
        var child2 = (Div)element.Children[1];

        Assert.Equal(0, child1.GetLeft());
        Assert.Equal(100, child2.GetLeft());
        Assert.Equal(200, child2.GetWidth());
    }

    [Fact]
    public void FlexLayout_JustifyContent_Center()
    {
        var markup = """
            <div style="width:300px; height:100px; justify-content:center; AlignItems:center; Display:flex; FlexDirection:row">
                <div style="width:100px; height:50px"></div>
            </div>
            """;
        var element = MarkupParser.Parse(markup);
        element.CalculateLayout(300, 100);
        var child1 = (Div)element.Children[0];

        Assert.Equal(100, child1.GetLeft()); // (300 - 100) / 2
    }

    [Fact]
    public void FlexLayout_JustifyContent_SpaceBetween()
    {
        var markup = """
            <div style="width:300px; height:100px; justifycontent:space-between; AlignItems:center; Display:flex; FlexDirection:row">
                <div style="width:50px; height:50px"></div>
                <div style="width:50px; height:50px"></div>
            </div>
            """;
        var element = MarkupParser.Parse(markup);
        element.CalculateLayout(300, 100);
        var child1 = (Div)element.Children[0];
        var child2 = (Div)element.Children[1];
        
        Assert.Equal(0, child1.GetLeft());
        Assert.Equal(250, child2.GetLeft()); // 300 - 50
    }

    [Fact]
    public void FlexLayout_AlignItems_Stretch()
    {
        var markup = """
            <div style="width:300px; height:100px; AlignItems:stretch; Display:flex; FlexDirection:row">
                <div style="width:100px; height:auto"></div>
            </div>
            """;
        var element = MarkupParser.Parse(markup);
        element.CalculateLayout(300, 100);
        var child1 = element.Children[0]; // Height is auto/none
        
        Assert.Equal(100, child1.GetHeight());
    }

    [Fact]
    public void FlexLayout_ColumnDirection()
    {
        var markup = """
            <div style="width:100px; height:300px; Display:flex; FlexDirection:column">
                <div style="width:50px; height:100px"></div>
                <div style="width:50px; flex: 1;"></div>
            </div>
            """;
        var element = MarkupParser.Parse(markup);
        element.CalculateLayout(100, 300);
        var child1 = (Div)element.Children[0];
        var child2 = (Div)element.Children[1];
        
        Assert.Equal(100, child1.GetHeight());
        Assert.Equal(200, child2.GetHeight());
        Assert.Equal(0, child1.GetTop());
        Assert.Equal(100, child2.GetTop());
    }
}
