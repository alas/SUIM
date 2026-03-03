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
            <vstack width="auto" height="auto" gap="10px">
                <label width="100px" height="50px" />
                <label width="100%" height="30px" />
            </vstack>
            """;
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);
        
        Assert.Equal("auto", element.GetAttribute("width"));
        Assert.Equal("auto", element.GetAttribute("height"));
        Assert.Equal(100, element.GetWidth());
        Assert.Equal(90, element.GetHeight()); // 50 + 30 + 10 gap
    }

    [Fact]
    public void Parse_Size_FractionalUnits()
    {
        var markup = "<div width=\"100%\" height=\"50%\" />";
        var (element, _) = MarkupParser.Parse(markup);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Equal("100%", div.GetAttribute("width"));
        Assert.Equal("50%", div.GetAttribute("height"));
    }

    [Fact]
    public void LayoutEngine_MeasuresStackWithFractionalUnits()
    {
        var stack = new Stack { Orientation = Orientation.Horizontal, Gap = "0" };
        stack.SetAttribute("width", "100%");
        stack.SetAttribute("height", "auto");

        var child1 = new Label();
        child1.SetAttribute("width", "30%");
        child1.SetAttribute("height", "50px");

        var child2 = new Label();
        child2.SetAttribute("width", "70%");
        child2.SetAttribute("height", "50px");
        
        stack.AddChild(child1, null);
        stack.AddChild(child2, null);
        
        stack.CalculateLayout(300, 100);
        
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
        var grid = new Grid();
        grid.SetAttribute("width", "auto");
        grid.SetAttribute("height", "auto");

        var child = new Label();
        child.SetAttribute("width", "100px");
        child.SetAttribute("height", "50px");

        grid.AddChild(child, null);
        
        grid.CalculateLayout(800, 600);
        
        Assert.Equal("auto", grid.GetAttribute("width"));
        Assert.Equal("auto", grid.GetAttribute("height"));
        Assert.Equal(100, grid.GetWidth());
        Assert.Equal(50, grid.GetHeight());
    }
    
    [Fact]
    public void LayoutEngine_OverlaysShouldFillParentSize()
    {
        // Create a grid with explicit size (simulating main UI container)
        // When overlays are inside, they should fill the grid's dimensions
        var grid = new Grid();
        
        // Create main UI container (like buttonsUI in MainView)
        var mainUI = new Stack 
        { };
        mainUI.SetAttribute("width", "400px");
        mainUI.SetAttribute("height", "300px");
        grid.AddChild(mainUI, null);
        
        // Create overlays - when grid has explicit size, overlays should fill it
        var overlay1 = new Overlay();
        var popupContent = new Grid
        { };
        popupContent.SetAttribute("width", "360px");
        popupContent.SetAttribute("height", "180px");
        overlay1.AddChild(popupContent, null);
        grid.AddChild(overlay1, null);
        
        var overlay2 = new Overlay();
        overlay2.AddChild(new Label(), null);
        grid.AddChild(overlay2, null);
        
        // Layout with screen size
        grid.CalculateLayout(1280, 720);
        
        // Grid should size to its explicit dimensions
        Assert.Equal(1280, grid.GetWidth());
        Assert.Equal(720, grid.GetHeight());
        
        // Overlays should fill the grid container when it has explicit size
        // Add some tolerance for padding
        Assert.True(overlay1.GetWidth() >= 1270 && overlay1.GetWidth() <= 1280, 
            $"overlay1.GetWidth() was {overlay1.GetWidth()}, expected ~1280");
        Assert.True(overlay1.GetHeight() >= 710 && overlay1.GetHeight() <= 720,
            $"overlay1.GetHeight() was {overlay1.GetHeight()}, expected ~720");
    }
}
