namespace SUIM.Tests.Layout;

using Xunit;
using Stride.UI.Panels;
using SUIM.Layout;
using SUIMStride;
using SUIM.Parse;
using SUIM.Parse.Components;

public class StrideLayoutTests
{
    [Fact]
    public void MarkupParser_WithLayoutEngine_CreatesLayout()
    {
        var markup = @"
            <stack orientation=""vertical"" spacing=""10"" width=""auto"" height=""auto"">
                <label width=""100"" height=""50"" />
                <label width=""100"" height=""30"" />
            </stack>";
            
        var (element, _) = MarkupParser.Parse(markup);
        LayoutEngine.Layout(element, 16, 200, 200);
        
        Assert.Equal(100, element.ActualWidth);
        Assert.Equal(90, element.ActualHeight); // 50 + 30 + 10 spacing
    }
    
    [Fact]
    public void MarkupParser_WithFractionalUnits_CreatesProportionalLayout()
    {
        var markup = @"
            <stack orientation=""horizontal"" spacing=""0"" height=""auto"">
                <label width=""1fr"" height=""50"" />
                <label width=""2fr"" height=""50"" />
            </stack>";
            
        var (element, _) = MarkupParser.Parse(markup);
        LayoutEngine.Layout(element, 16, 300, 100);
        
        Assert.Equal(300, element.ActualWidth);
        Assert.Equal(50, element.ActualHeight);
    }
    
    [Fact]
    public void MarkupParser_WithRemUnits_CreatesScaledLayout()
    {
        var markup = @"
            <stack orientation=""vertical"" spacing=""0"" height=""auto"">
                <label width=""2rem"" height=""1rem"" />
                <label width=""1rem"" height=""2rem"" />
            </stack>";
            
        var (element, _) = MarkupParser.Parse(markup);
        LayoutEngine.Layout(element, 16, 200, 200);
        
        // 2rem = 32px, 1rem = 16px
        Assert.Equal(200, element.ActualWidth);
        Assert.Equal(48, element.ActualHeight); // 16 + 32

        var label1 = (Label)element.Children[0];
        Assert.Equal(32, label1.ActualWidth);
        Assert.Equal(16, label1.ActualHeight);

        var label2 = (Label)element.Children[1];
        Assert.Equal(16, label2.ActualWidth);
        Assert.Equal(32, label2.ActualHeight);
    }

    [Fact]
    public void MarkupParser_WithRemUnitsAndAutoParent_CreatesScaledLayout()
    {
        var markup = @"
            <stack orientation=""vertical"" spacing=""0"" width=""auto"" height=""auto"">
                <label width=""2rem"" height=""1rem"" />
                <label width=""1rem"" height=""2rem"" />
            </stack>";

        var (element, _) = MarkupParser.Parse(markup);
        LayoutEngine.Layout(element, 16, 200, 200);

        // 2rem = 32px, 1rem = 16px
        Assert.Equal(32, element.ActualWidth);
        Assert.Equal(48, element.ActualHeight); // 16 + 32

        var label1 = (Label)element.Children[0];
        Assert.Equal(32, label1.ActualWidth);
        Assert.Equal(16, label1.ActualHeight);

        var label2 = (Label)element.Children[1];
        Assert.Equal(16, label2.ActualWidth);
        Assert.Equal(32, label2.ActualHeight);
    }

    [Fact]
    public void MarkupParser_FractionalUnitsLabels_CreatesFractionalUnitsLayout()
    {
        var markup = @"
            <stack orientation=""horizontal"" spacing=""10"">
                <stack orientation=""vertical"" spacing=""5"">
                    <label width=""fr"" />
                    <label width=""fr"" />
                    <label width=""fr"" />
                    <label width=""fr"" />
                    <label width=""fr"" />
                </stack>
                <stack orientation=""vertical"" spacing=""15"">
                    <label width=""fr"" />
                    <label width=""fr"" />
                    <label width=""fr"" />
                </stack>
            </stack>";

        var (element, _) = MarkupParser.Parse(markup);
        LayoutEngine.Layout(element, 25, 640, 480);

        Assert.Equal(640, element.ActualWidth);
        Assert.Equal(480, element.ActualHeight);
        // The horizontal stack should be 640 wide (no size defined, assume 1fr, it takes all available width).
        // The heights of the labels should be determined by the default font size (no size defined, assume auto, root font size: 25),
        // The widths of the labels should be determined by the available width (640) minus the spacing between the two vertical stacks (10), divided by 2,
        // so each label in the first vertical stack should be 315 pixels wide, and each label in the second vertical stack should be 315 pixels wide.

        // Verify child label sizes using Actual* values populated by LayoutEngine
        var firstStack = (Stack)element.Children[0];
        var secondStack = (Stack)element.Children[1];

        int expectedStackWidth = (640 - 10) / 2; // 315
        int expectedLabelHeight = 25; // root font size

        foreach (var lbl in firstStack.Children)
        {
            Assert.Equal(expectedStackWidth, lbl.ActualWidth);
            Assert.Equal(expectedLabelHeight, lbl.ActualHeight);
        }

        foreach (var lbl in secondStack.Children)
        {
            Assert.Equal(expectedStackWidth, lbl.ActualWidth);
            Assert.Equal(expectedLabelHeight, lbl.ActualHeight);
        }
    }

    [Fact]
    public void MarkupParser_WithOverlayMarkup_OverlaysFillAvailableSpace()
    {
        // Simulates MainView layout: root grid with main UI and overlays
        var markup = @"
            <grid>
                <style>
                    .container { HorizontalAlignment: Center; VerticalAlignment: Center; }
                    .overlay { visibility: collapsed; }
                </style>
                <vstack class=""container"" width=""400"" height=""300"">
                    <label value=""Main UI"" />
                </vstack>
                
                <overlay class=""overlay"" id=""popup"">
                    <grid width=""360"" height=""180"" halign=""center"" valign=""center"">
                        <label value=""Popup"" />
                    </grid>
                </overlay>
                
                <overlay class=""overlay"" id=""screenOverlay"">
                    <grid halign=""center"" valign=""center"">
                        <label value=""Blocker"" />
                    </grid>
                </overlay>
            </grid>";
        
        var (root, _) = MarkupParser.Parse(markup);
        LayoutEngine.Layout(root, 16, 1280, 720);
        
        // Root grid should measure to 1280x720 (available space with no explicit size)
        Assert.Equal(1280, root.ActualWidth);
        Assert.Equal(720, root.ActualHeight);
        
        // Find the overlays in the grid's children
        var overlays = root.Children.OfType<Overlay>().ToList();
        Assert.Equal(2, overlays.Count);
        
        var popup = overlays.FirstOrDefault(o => o.Id == "popup");
        var screenOverlay = overlays.FirstOrDefault(o => o.Id == "screenOverlay");
        
        Assert.NotNull(popup);
        Assert.NotNull(screenOverlay);
        
        // Overlays should fill the root grid's available space (1280x720)
        // They should NOT be sized to their content
        Assert.True(popup.ActualWidth >= 1270 && popup.ActualWidth <= 1280,
            $"popup width {popup.ActualWidth} should be ~1280");
        Assert.True(popup.ActualHeight >= 710 && popup.ActualHeight <= 720,
            $"popup height {popup.ActualHeight} should be ~720");
        
        Assert.True(screenOverlay.ActualWidth >= 1270 && screenOverlay.ActualWidth <= 1280,
            $"screenOverlay width {screenOverlay.ActualWidth} should be ~1280");
        Assert.True(screenOverlay.ActualHeight >= 710 && screenOverlay.ActualHeight <= 720,
            $"screenOverlay height {screenOverlay.ActualHeight} should be ~720");
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
        LayoutEngine.Layout(suimRoot, 16, 1280, 720);
        
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