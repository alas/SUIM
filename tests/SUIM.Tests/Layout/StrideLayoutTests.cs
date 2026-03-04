namespace SUIM.Tests.Layout;

using System.Linq;
using Xunit;
using Stride.UI.Panels;
using SUIM.Parse;
using SUIM.Parse.Components;
using SUIMStride;

public class StrideLayoutTests
{
    [Fact]
    public void MarkupParser_WithLayoutEngine_CreatesLayout()
    {
        var markup = @"
            <div style=""display:flex; align-items:flex-start;"">
                <vstack style=""gap:10; width:auto; height:auto"">
                    <label style=""width:100; height:50""></label>
                    <label style=""width:100; height:30""></label>
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
        var markup1 = @"
            <div style=""display:flex; align-items:flex-start; justify-items:flex-start;"">
                <hstack style=""gap:0; height:auto; flex:1;"">
                    <label style=""height:50; flex:1;""></label>
                    <label style=""height:50; flex:1;""></label>
                </hstack>
            </div>";
        var markup2 = @"
            <div style=""display:flex; align-items:flex-start; justify-content:flex-start; width:100%; height:100%"">
                <div style=""display:flex; align-items:flex-start; justify-content:flex-start; flex-direction:row; height:auto; flex:1;"">
                    <label style=""height:50; flex:1;""></label>
                    <label style=""height:50; flex:1;""></label>
                </div>
            </div>";

        var trees = new[] { markup1, markup2 }.Select(markup =>
        {
            var (element, _) = MarkupParser.Parse(markup);
            element.CalculateLayout(300, 100);
            var stack = element.Children[0];

            Assert.Equal("auto", stack.GetAttribute("height"));
            Assert.Equal(300, stack.GetWidth());
            Assert.Equal(50, stack.GetHeight());

            return element;
        }).ToList();

        Assert.True(IsSameLayout(trees[0], trees[1]), "Both markup variations should produce the same layout results.");

        static bool IsSameLayout(UIElement x, UIElement y)
        {
            Assert.Equal(x.GetWidth(), y.GetWidth());
            Assert.Equal(x.GetHeight(), y.GetHeight());
            Assert.Equal(x.GetLeft(), y.GetLeft());
            Assert.Equal(x.GetTop(), y.GetTop());

            Assert.Equal(x.Children.Count, y.Children.Count);
            for (int i = 0; i < x.Children.Count; i++)
            {
                return IsSameLayout(x.Children[i], y.Children[i]);
            }

            return true;
        }
    }

    [Fact]
    public void MarkupParser_WithPixelUnits_EquivalentToRem_CreatesScaledLayout()
    {
        var markup = @"
            <div>
                <vstack style=""gap:0; height:auto;"">
                    <label style=""width:32px; height:16px;""></label>
                    <label style=""width:16px; height:32px;""></label>
                </vstack>
            </div>";
            
        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);
        
        Assert.Equal(200, element.GetWidth());
        Assert.Equal(200, element.GetHeight());

        var div = (Stack)element.Children[0];
        Assert.Equal("auto", div.GetAttribute("height"));
        Assert.Equal(32, div.GetWidth());
        Assert.Equal(200, div.GetHeight());

        var label1 = (Label)div.Children[0];
        Assert.Equal(32, label1.GetWidth());
        Assert.Equal(16, label1.GetHeight());

        var label2 = (Label)div.Children[1];
        Assert.Equal(16, label2.GetWidth());
        Assert.Equal(32, label2.GetHeight());
    }

    [Fact]
    public void MarkupParser_WithPixelUnits_EquivalentToRem_CreatesNonScaledLayout()
    {
        var markup = @"
            <div>
                <vstack style=""gap:0; align-self:flex-start;"">
                    <label style=""width:32px; height:16px;""></label>
                    <label style=""width:16px; height:32px;""></label>
                </vstack>
            </div>";

        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);
        
        Assert.Equal(200, element.GetWidth());
        Assert.Equal(200, element.GetHeight());

        var div = (Stack)element.Children[0];
        Assert.Equal("auto", div.GetAttribute("height"));
        Assert.Equal(32, div.GetWidth());
        Assert.Equal(48, div.GetHeight()); // 16 + 32

        var label1 = (Label)div.Children[0];
        Assert.Equal(32, label1.GetWidth());
        Assert.Equal(16, label1.GetHeight());

        var label2 = (Label)div.Children[1];
        Assert.Equal(16, label2.GetWidth());
        Assert.Equal(32, label2.GetHeight());
    }

    [Fact]
    public void MarkupParser_WithPixelUnits_EquivalentToRemAndAutoParent_CreatesScaledLayout()
    {
        var markup = @"
            <div>
                <vstack style=""width:auto; height:auto; align-self:flex-start;"">
                    <label style=""width:32px; height:16px;""></label>
                    <label style=""width:16px; height:32px;""></label>
                </vstack>
            </div>";

        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(200, 200);

        var stack = (Stack)element.Children[0];
        Assert.Equal("auto", stack.GetAttribute("width"));
        Assert.Equal("auto", stack.GetAttribute("height"));

        Assert.Equal(32, stack.GetWidth());
        Assert.Equal(48, stack.GetHeight()); // 16 + 32

        var label1 = (Label)stack.Children[0];
        Assert.Equal(32, label1.GetWidth());
        Assert.Equal(16, label1.GetHeight());

        var label2 = (Label)stack.Children[1];
        Assert.Equal(16, label2.GetWidth());
        Assert.Equal(32, label2.GetHeight());
    }

    [Fact]
    public void MarkupParser_FractionalUnitsLabels_CreatesFractionalUnitsLayout()
    {
        //  width:50%; height:100%;
        var markup = @"
            <hstack style=""gap:10px;"">
                <vstack style=""gap:5px; width:50%; height:100%;"">
                    <label style=""width:100%; height:25px;""></label>
                    <label style=""width:100%; height:25px;""></label>
                    <label style=""width:100%; height:25px;""></label>
                    <label style=""width:100%; height:25px;""></label>
                    <label style=""width:100%; height:25px;""></label>
                </vstack>
                <vstack style=""gap:15px; width:50%; height:100%;"">
                    <label style=""width:100%; height:25px;""></label>
                    <label style=""width:100%; height:25px;""></label>
                    <label style=""width:100%; height:25px;""></label>
                </vstack>
            </hstack>";

        var (element, _) = MarkupParser.Parse(markup);
        element.CalculateLayout(640, 480);

        Assert.Equal(640, element.GetWidth());
        Assert.Equal(480, element.GetHeight());
        // The horizontal stack should be 640 wide (no size defined, assume 100%, it takes all available width).
        // The heights of the labels should be determined by the default font size (no size defined, assume auto, root font size: 25),
        // The widths of the labels should be determined by the available width (640) minus the gap between the two vertical stacks (10), divided by 2,
        // so each label in the first vertical stack should be 315 pixels wide, and each label in the second vertical stack should be 315 pixels wide.

        var firstStack = (Stack)element.Children[0];
        var secondStack = (Stack)element.Children[1];

        int expectedStackWidth = (640 - 10) / 2; // 315
        int expectedLabelHeight = 25;

        Assert.Equal(expectedStackWidth, firstStack.GetWidth());
        Assert.Equal(480, firstStack.GetHeight());
        Assert.Equal(expectedStackWidth, secondStack.GetWidth());
        Assert.Equal(480, secondStack.GetHeight());

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
            <div class=""centeredcontent"">
                <style>
                    .centeredcontent { justify-content: center; align-content: center; }
                    .overlay { visibility: collapsed; }
                </style>
                <vstack  style=""width:400; height:300"">
                    <label value=""Main UI""></label>
                </vstack>
                
                <overlay class=""overlay centeredcontent"" id=""popup"">
                    <div style=""width:360; height:180"">
                        <label value=""Popup""></label>
                    </div>
                </overlay>
                
                <overlay class=""overlay centeredcontent"" id=""screenOverlay"">
                    <div>
                        <label value=""Blocker""></label>
                    </div>
                </overlay>
            </div>";
        
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
            <div>
                <vstack style=""width:400; height:300"">
                    <label value=""Main UI""></label>
                </vstack>
                
                <overlay id=""popup"">
                    <div style=""width:360; height:180"">
                        <label value=""Popup""></label>
                    </div>
                </overlay>
                
                <overlay id=""screenOverlay"" style=""visibility:collapsed"">
                    <div>
                        <label value=""Blocker""></label>
                    </div>
                </overlay>
            </div>";
        
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