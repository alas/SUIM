namespace SUIM.Tests.Layout;

using Xunit;
using SUIM.Layout;
using SUIM.Parse.Components;

public class FlexLayoutTests
{
    [Fact]
    public void FlexLayout_RowDirection_DistributesSpace()
    {
        var div = new Div { Display = "flex", FlexDirection = "row", Width = "300px", Height = "100px" };
        var child1 = new Div { Width = "100px", Height = "50px" };
        var child2 = new Div { Width = "1fr", Height = "50px" };
        
        div.AddChild(child1, null);
        div.AddChild(child2, null);
        
        LayoutEngine.Layout(div, 16, 300, 100);
        
        Assert.Equal(100, child1.ActualWidth);
        Assert.Equal(200, child2.ActualWidth); // 300 - 100
        Assert.Equal(0, child1.ActualX);
        Assert.Equal(100, child2.ActualX);
    }

    [Fact]
    public void FlexLayout_JustifyContent_Center()
    {
        var div = new Div { Display = "flex", FlexDirection = "row", JustifyContent = "center", Width = "300px", Height = "100px" };
        var child1 = new Div { Width = "100px", Height = "50px" };
        
        div.AddChild(child1, null);
        
        LayoutEngine.Layout(div, 16, 300, 100);
        
        Assert.Equal(100, child1.ActualX); // (300 - 100) / 2
    }

    [Fact]
    public void FlexLayout_JustifyContent_SpaceBetween()
    {
        var div = new Div { Display = "flex", FlexDirection = "row", JustifyContent = "space-between", Width = "300px", Height = "100px" };
        var child1 = new Div { Width = "50px", Height = "50px" };
        var child2 = new Div { Width = "50px", Height = "50px" };
        
        div.AddChild(child1, null);
        div.AddChild(child2, null);
        
        LayoutEngine.Layout(div, 16, 300, 100);
        
        Assert.Equal(0, child1.ActualX);
        Assert.Equal(250, child2.ActualX); // 300 - 50
    }

    [Fact]
    public void FlexLayout_AlignItems_Stretch()
    {
        var div = new Div { Display = "flex", FlexDirection = "row", AlignItems = "stretch", Width = "300px", Height = "100px" };
        var child1 = new Div { Width = "100px" }; // Height is auto/none
        
        div.AddChild(child1, null);
        
        LayoutEngine.Layout(div, 16, 300, 100);
        
        Assert.Equal(100, child1.ActualHeight);
    }

    [Fact]
    public void FlexLayout_ColumnDirection()
    {
        var div = new Div { Display = "flex", FlexDirection = "column", Width = "100px", Height = "300px" };
        var child1 = new Div { Width = "50px", Height = "100px" };
        var child2 = new Div { Width = "50px", Height = "1fr" };
        
        div.AddChild(child1, null);
        div.AddChild(child2, null);
        
        LayoutEngine.Layout(div, 16, 100, 300);
        
        Assert.Equal(100, child1.ActualHeight);
        Assert.Equal(200, child2.ActualHeight);
        Assert.Equal(0, child1.ActualY);
        Assert.Equal(100, child2.ActualY);
    }
}
