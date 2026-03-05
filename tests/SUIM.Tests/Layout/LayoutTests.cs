namespace SUIM.Tests.Layout;

using Xunit;
using SUIM.Parse;
using SUIM.Parse.Components;
using SUIM.Parse.Components.Attributes;

public class LayoutTests
{
    [Fact]
    public void LayoutEngine_MeasuresStackWithPixels()
    {
        var markup = """
        <div style="align-items:flex-start;">
            <vstack style="width:auto; height:auto; gap:10px; flex:0 0 auto;">
                <label style="width:100px; height:50px"></label>
                <label style="align-self:stretch; height:30px"></label>
            </vstack>
        </div>
        """;
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);
        var vstack = element.Children[0];

        Assert.Equal("auto", vstack.GetAttribute("width"));
        Assert.Equal("auto", vstack.GetAttribute("height"));
        Assert.Equal(100, vstack.GetWidth());
        Assert.Equal(90, vstack.GetHeight()); // 50 + 30 + 10 gap
    }

    [Fact]
    public void Parse_Size_FractionalUnits()
    {
        var markup = "<div style=\"width:100%; height:50%\"></div>";
        var (element, _) = MarkupParser.Parse(markup);

        var div = element;
        Assert.Equal("100%", div.GetAttribute("width"));
        Assert.Equal("50%", div.GetAttribute("height"));
    }

    [Fact]
    public void LayoutEngine_MeasuresStackWithFractionalUnits()
    {
        var div = new Div();
        div.SetAttribute("align-items", "flex-start");

        var stack = new Stack { Orientation = Orientation.Horizontal, Gap = "0" };
        stack.SetAttribute("width", "100%");
        stack.SetAttribute("height", "auto");

        var child1 = new Label();
        child1.SetAttribute("width", "30%");
        child1.SetAttribute("height", "50px");

        var child2 = new Label();
        child2.SetAttribute("width", "70%");
        child2.SetAttribute("height", "50px");
        
        div.AddChild(stack, null);
        stack.AddChild(child1, null);
        stack.AddChild(child2, null);
        
        div.CalculateLayout(300, 100);
        
        Assert.Equal("100%", stack.GetAttribute("width"));
        Assert.Equal("auto", stack.GetAttribute("height"));
        Assert.Equal(300, stack.GetWidth());
        Assert.Equal(50, stack.GetHeight());
    }
    
    [Fact]
    public void LayoutEngine_MeasuresGridWithMixedUnits()
    {
        var grid = new Grid { Columns = "100, 100%", Rows = "50, 100%" };
        var child1 = new Label();
        child1.SetAttribute("width", "50px");
        child1.SetAttribute("height", "25px");

        var child2 = new Label();
        child2.SetAttribute("width", "auto");
        child2.SetAttribute("height", "auto");
        
        grid.AddChild(child1, null);
        grid.AddChild(child2, null);
        
        grid.CalculateLayout(300, 200);
        
        Assert.Equal(300, grid.GetWidth());
        Assert.Equal(200, grid.GetHeight());
    }
    
    [Fact]
    public void LayoutEngine_MeasuresDivWithAnchor()
    {
        var div = new Div { Anchor = Anchor.Top.ToString() };
        div.SetAttribute("width", "100px");
        div.SetAttribute("height", "50px");

        var child = new Label();
        child.SetAttribute("width", "50px");
        child.SetAttribute("height", "25px");
        
        div.AddChild(child, null);
        
        div.CalculateLayout(400, 300);
        
        Assert.Equal(100, div.GetWidth());
        Assert.Equal(50, div.GetHeight());
    }
    
    [Fact]
    public void LayoutEngine_MeasuresWindow()
    {
        var div1 = new Div();
        div1.SetAttribute("align-items", "flex-start");
        div1.SetAttribute("display", "flex");

        var div2 = new Div();
        div2.SetAttribute("width", "auto");
        div2.SetAttribute("height", "auto");

        var child = new Label();
        child.SetAttribute("width", "100px");
        child.SetAttribute("height", "50px");

        div1.AddChild(div2, null);
        div2.AddChild(child, null);

        div1.CalculateLayout(800, 600);
        
        Assert.Equal("auto", div2.GetAttribute("width"));
        Assert.Equal("auto", div2.GetAttribute("height"));
        Assert.Equal(100, div2.GetWidth());
        Assert.Equal(50, div2.GetHeight());
    }
    
    [Fact]
    public void LayoutEngine_OverlaysShouldFillParentSize()
    {
        // Create a div with explicit size (simulating main UI container)
        // When overlays are inside, they should fill the grid's dimensions
        var div = new Div();
        
        // Create main UI container (like buttonsUI in MainView)
        var mainUI = new Stack 
        { };
        mainUI.SetAttribute("width", "400px");
        mainUI.SetAttribute("height", "300px");
        div.AddChild(mainUI, null);
        
        // Create overlays - when grid has explicit size, overlays should fill it
        var overlay1 = new Overlay();
        var popupContent = new Div
        { };
        popupContent.SetAttribute("width", "360px");
        popupContent.SetAttribute("height", "180px");
        overlay1.AddChild(popupContent, null);
        div.AddChild(overlay1, null);
        
        var overlay2 = new Overlay();
        overlay2.AddChild(new Label(), null);
        div.AddChild(overlay2, null);
        
        // Layout with screen size
        div.CalculateLayout(1280, 720);
        
        // Grid should size to its explicit dimensions
        Assert.Equal(1280, div.GetWidth());
        Assert.Equal(720, div.GetHeight());
        
        // Overlays should fill the grid container when it has explicit size
        // Add some tolerance for padding
        Assert.True(overlay1.GetWidth() >= 1270 && overlay1.GetWidth() <= 1280, 
            $"overlay1.GetWidth() was {overlay1.GetWidth()}, expected ~1280");
        Assert.True(overlay1.GetHeight() >= 710 && overlay1.GetHeight() <= 720,
            $"overlay1.GetHeight() was {overlay1.GetHeight()}, expected ~720");
    }
}
