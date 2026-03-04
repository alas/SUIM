namespace SUIM.Tests.Layout;

using System.Linq;
using Xunit;
using Stride.UI.Panels;
using SUIMStride;
using SUIM.Parse;
using SUIM.Parse.Components;

public class StrideLayoutTests
{
    [Fact]
    public void MarkupParser_WithLayoutEngine_CreatesLayout()
    {
        var markup = @"
            <div style=""display:flex; align-items:flex-start;"">
                <vstack style=""gap:10; width:auto; height:auto"">
                    <label style=""width:100; height:50"" />
                    <label style=""width:100; height:30"" />
                </vstack>
            </div>";
            
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);
        var stack = (Stack)element.Children[0];

        Assert.Equal("auto", stack.GetAttribute("width"));
        Assert.Equal("auto", stack.GetAttribute("height"));
        Assert.Equal(100, stack.GetWidth());
        Assert.Equal(90, stack.GetHeight()); // 50 + 30 + 10 gap
    }
    
    [Fact]
    public void MarkupParser_WithFractionalUnits_CreatesProportionalLayout()
    {
        var markup = @"
            <div style=""display:flex; align-items:flex-start; justify-items:flex-start;"">
                <hstack style=""gap:0; height:auto"">
                    <label style=""height:50; flex:1;"" />
                    <label style=""height:50; flex:1;"" />
                </hstack>
            </div>";
            
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(300, 100);
        var stack = element.Children[0];

        Assert.Equal("auto", stack.GetAttribute("height"));
        Assert.Equal(300, stack.GetWidth());
        Assert.Equal(50, stack.GetHeight());
    }
    
    [Fact]
    public void MarkupParser_WithPixelUnits_EquivalentToRem_CreatesScaledLayout()
    {
        var markup = @"
            <stack orientation=""vertical"" gap=""0"" height=""auto"">
                <label width=""32px"" height=""16px"" />
                <label width=""16px"" height=""32px"" />
            </stack>";
            
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);
        
        Assert.Equal("auto", element.GetAttribute("height"));
        Assert.Equal(32, element.GetWidth());
        Assert.Equal(48, element.GetHeight()); // 16 + 32

        var label1 = (Label)element.Children[0];
        Assert.Equal(32, label1.GetWidth());
        Assert.Equal(16, label1.GetHeight());

        var label2 = (Label)element.Children[1];
        Assert.Equal(16, label2.GetWidth());
        Assert.Equal(32, label2.GetHeight());
    }

    [Fact]
    public void MarkupParser_WithPixelUnits_EquivalentToRemAndAutoParent_CreatesScaledLayout()
    {
        var markup = @"
            <stack orientation=""vertical"" gap=""0"" width=""auto"" height=""auto"">
                <label width=""32px"" height=""16px"" />
                <label width=""16px"" height=""32px"" />
            </stack>";

        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);

        Assert.Equal("auto", element.GetAttribute("width"));
        Assert.Equal("auto", element.GetAttribute("height"));

        Assert.Equal(32, element.GetWidth());
        Assert.Equal(48, element.GetHeight()); // 16 + 32

        var label1 = (Label)element.Children[0];
        Assert.Equal(32, label1.GetWidth());
        Assert.Equal(16, label1.GetHeight());

        var label2 = (Label)element.Children[1];
        Assert.Equal(16, label2.GetWidth());
        Assert.Equal(32, label2.GetHeight());
    }

    [Fact]
    public void MarkupParser_FractionalUnitsLabels_CreatesFractionalUnitsLayout()
    {
        var markup = @"
            <hstack style=""gap:10px"">
                <vstack style=""gap:5px"">
                    <label style=""flex:1;"" />
                    <label style=""flex:1;"" />
                    <label style=""flex:1;"" />
                    <label style=""flex:1;"" />
                    <label style=""flex:1;"" />
                </vstack>
                <vstack style=""gap:15px"">
                    <label style=""flex:1;"" />
                    <label style=""flex:1;"" />
                    <label style=""flex:1;"" />
                </vstack>
            </hstack>";

        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(640, 480);

        Assert.Equal(640, element.GetWidth());
        Assert.Equal(480, element.GetHeight());
        // The horizontal stack should be 640 wide (no size defined, assume 1fr, it takes all available width).
        // The heights of the labels should be determined by the default font size (no size defined, assume auto, root font size: 25),
        // The widths of the labels should be determined by the available width (640) minus the gap between the two vertical stacks (10), divided by 2,
        // so each label in the first vertical stack should be 315 pixels wide, and each label in the second vertical stack should be 315 pixels wide.

        // Verify child label sizes using Actual* values populated by LayoutEngine
        var firstStack = (Stack)element.Children[0];
        var secondStack = (Stack)element.Children[1];

        int expectedStackWidth = (640 - 10) / 2; // 315
        int expectedLabelHeight = 25; // root font size

        foreach (var lbl in firstStack.Children)
        {
            Assert.Equal(expectedStackWidth, lbl.GetWidth());
            Assert.Equal(expectedLabelHeight, lbl.GetHeight());
        }

        foreach (var lbl in secondStack.Children)
        {
            Assert.Equal(expectedStackWidth, lbl.GetWidth());
            Assert.Equal(expectedLabelHeight, lbl.GetHeight());
        }
    }

    [Fact]
    public void MarkupParser_WithOverlayMarkup_OverlaysFillAvailableSpace()
    {
        // Simulates MainView layout: root grid with main UI and overlays
        var markup = @"
            <grid class=""centeredcontent"">
                <style>
                    .centeredcontent { justify-content: center; align-content: center; }
                    .overlay { visibility: collapsed; }
                </style>
                <vstack width=""400"" height=""300"">
                    <label value=""Main UI"" />
                </vstack>
                
                <overlay class=""overlay centeredcontent"" id=""popup"">
                    <grid width=""360"" height=""180"">
                        <label value=""Popup"" />
                    </grid>
                </overlay>
                
                <overlay class=""overlay centeredcontent"" id=""screenOverlay"">
                    <grid>
                        <label value=""Blocker"" />
                    </grid>
                </overlay>
            </grid>";
        
        var (root, _) = MarkupParser.Parse(markup);
        root.CalculateLayout(1280, 720);
        
        // Root grid should measure to 1280x720 (available space with no explicit size)
        Assert.Equal(1280, root.GetWidth());
        Assert.Equal(720, root.GetHeight());
        
        // Find the overlays in the grid's children
        var overlays = root.Children.OfType<Overlay>().ToList();
        Assert.Equal(2, overlays.Count);
        
        var popup = overlays.FirstOrDefault(o => o.Id == "popup");
        var screenOverlay = overlays.FirstOrDefault(o => o.Id == "screenOverlay");
        
        Assert.NotNull(popup);
        Assert.NotNull(screenOverlay);
        
        // Overlays should fill the root grid's available space (1280x720)
        // They should NOT be sized to their content
        Assert.True(popup.GetWidth() >= 1270 && popup.GetWidth() <= 1280,
            $"popup width {popup.GetWidth()} should be ~1280");
        Assert.True(popup.GetHeight() >= 710 && popup.GetHeight() <= 720,
            $"popup height {popup.GetHeight()} should be ~720");
        
        Assert.True(screenOverlay.GetWidth() >= 1270 && screenOverlay.GetWidth() <= 1280,
            $"screenOverlay width {screenOverlay.GetWidth()} should be ~1280");
        Assert.True(screenOverlay.GetHeight() >= 710 && screenOverlay.GetHeight() <= 720,
            $"screenOverlay height {screenOverlay.GetHeight()} should be ~720");
    }

    [Fact]
    public void SUIMStride_MappingOverlays_PreservesOverlayDimensions()
    {
        // Test that Stride mapping preserves overlay dimensions from SUIM layout
        var markup = @"
            <grid>
                <vstack width=""400"" height=""300"">
                    <label value=""Main UI"" />
                </vstack>
                
                <overlay id=""popup"">
                    <grid width=""360"" height=""180"">
                        <label value=""Popup"" />
                    </grid>
                </overlay>
                
                <overlay id=""screenOverlay"" visibility=""collapsed"">
                    <grid>
                        <label value=""Blocker"" />
                    </grid>
                </overlay>
            </grid>";
        
        // Parse and layout in SUIM
        var (suimRoot, _) = MarkupParser.Parse(markup);
        suimRoot.CalculateLayout(1280, 720);
        
        // Map to Stride
        var mapper = new Parser();
        var strideRoot = mapper.MapElement(suimRoot, null);
        
        // Verify Stride root is a canvas
        Assert.NotNull(strideRoot);
        Assert.IsType<Canvas>(strideRoot);
        
        var strideCanvas = (Canvas)strideRoot;
        
        // Check that the Stride canvas has children (overlays and main UI mapped)
        Assert.NotEmpty(strideCanvas.Children);

        // The canvas should have mapped children including overlays
        // Stride overlays should preserve the SUIM dimensions
        // This verifies the mapping doesn't lose dimension data
        var strideChildren = strideCanvas.Children.ToList();
        Assert.True(strideChildren.Count >= 2, $"Expected at least 2 children, got {strideChildren.Count}");

        var popup = strideChildren.FirstOrDefault(c => c.Name == "popup");
        Assert.NotNull(popup);
        Assert.Equal(1280, popup.Width);
        Assert.Equal(720, popup.Height);

        var screenOverlay = strideChildren.FirstOrDefault(c => c.Name == "screenOverlay");
        Assert.NotNull(screenOverlay);
        Assert.Equal(1280, screenOverlay.Width);
        Assert.Equal(720, screenOverlay.Height);
    }
}