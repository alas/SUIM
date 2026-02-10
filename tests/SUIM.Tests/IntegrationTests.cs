namespace SUIM.Tests;

using Xunit;
using SUIM.Layout;

public class IntegrationTests
{
    [Fact]
    public void MarkupParser_WithLayoutEngine_CreatesLayout()
    {
        var markup = @"
            <stack orientation=""vertical"" spacing=""10"">
                <label width=""100"" height=""50"" />
                <label width=""100"" height=""30"" />
            </stack>";
            
        var (element, _) = new MarkupParser().Parse(markup);
        var layoutEngine = new LayoutEngine();
        var context = new LayoutContext(16, 200, 200);
        var result = layoutEngine.Layout(element, context);
        
        Assert.Equal(100, result.Width);
        Assert.Equal(90, result.Height); // 50 + 30 + 10 spacing
    }
    
    [Fact]
    public void MarkupParser_WithStarUnits_CreatesProportionalLayout()
    {
        var markup = @"
            <stack orientation=""horizontal"" spacing=""0"">
                <label width=""1*"" height=""50"" />
                <label width=""2*"" height=""50"" />
            </stack>";
            
        var (element, _) = new MarkupParser().Parse(markup);
        var layoutEngine = new LayoutEngine();
        var context = new LayoutContext(16, 300, 100);
        var result = layoutEngine.Layout(element, context);
        
        Assert.Equal(300, result.Width);
        Assert.Equal(50, result.Height);
    }
    
    [Fact]
    public void MarkupParser_WithRemUnits_CreatesScaledLayout()
    {
        var markup = @"
            <stack orientation=""vertical"" spacing=""0"">
                <label width=""2rem"" height=""1rem"" />
                <label width=""1rem"" height=""2rem"" />
            </stack>";
            
        var (element, _) = new MarkupParser().Parse(markup);
        var layoutEngine = new LayoutEngine();
        var context = new LayoutContext(16, 200, 200);
        var result = layoutEngine.Layout(element, context);
        
        // 2rem = 32px, 1rem = 16px
        Assert.Equal(32, result.Width);
        Assert.Equal(48, result.Height); // 16 + 32
    }

    [Fact]
    public void MarkupParser_Withoutsize_CreatesStarLayout()
    {
        var markup = @"
            <stack orientation=""horizontal"" spacing=""10"">
                <stack orientation=""vertical"" spacing=""5"">
                    <label />
                    <label />
                    <label />
                    <label />
                    <label />
                </stack>
                <stack orientation=""vertical"" spacing=""15"">
                    <label />
                    <label />
                    <label />
                </stack>
            </stack>";

        var (element, _) = new MarkupParser().Parse(markup);
        var layoutEngine = new LayoutEngine();
        var context = new LayoutContext(25, 640, 480);
        var result = layoutEngine.Layout(element, context);

        Assert.Equal(640, result.Width);
        Assert.Equal(480, result.Height);
        // The horizontal stack should be 640 wide (no size defined, assume *, it takes all available width).
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