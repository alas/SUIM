namespace SUIM.Tests;

using Xunit;
using SUIM.Layout;

public class IntegrationTests
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

        var label1 = (Components.Label)element.Children[0];
        Assert.Equal(32, label1.ActualWidth);
        Assert.Equal(16, label1.ActualHeight);

        var label2 = (Components.Label)element.Children[1];
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

        var label1 = (Components.Label)element.Children[0];
        Assert.Equal(32, label1.ActualWidth);
        Assert.Equal(16, label1.ActualHeight);

        var label2 = (Components.Label)element.Children[1];
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
        var firstStack = (Components.Stack)element.Children[0];
        var secondStack = (Components.Stack)element.Children[1];

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
}