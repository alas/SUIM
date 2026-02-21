namespace SUIM.Tests;

using System.Linq;
using Xunit;
using SUIM.Components;
using SUIM.Layout;

public class DebuggerTests
{
    [Fact]
    public void LayoutEngine_DebugMode_GeneratesOverlay()
    {
        // 1. Setup
        LayoutEngine.DebugMode = true;
        
        // Use explicit sizes and no constraints to ensure predictable ActualWidth
        var stack = new Stack { Width = "100px", Height = "100px", Margin = "10", Padding = "5" };
        var label = new Label { Width = "50px", Height = "30px" };
        stack.AddChild(label, null);

        // 2. Layout - Give plenty of space so nothing is constrained
        LayoutEngine.Layout(stack, 16, 1000, 1000);

        // 3. Generate Overlay
        var debugOverlay = LayoutEngine.GenerateDebugOverlay(stack) as Overlay;

        // 4. Assert
        Assert.NotNull(debugOverlay);
        
        var borders = debugOverlay.Children.OfType<Border>().ToList();
        
        // Stack borders
        var stackMarginBorder = borders.FirstOrDefault(b => b.Color == "Orange");
        var stackPaddingBorder = borders.FirstOrDefault(b => b.Color == "Green");
        var stackContentBorder = borders.FirstOrDefault(b => b.Color == "Blue");

        // Verify that the debug borders match the element's calculated properties
        Assert.NotNull(stackMarginBorder);
        Assert.Equal((stack.ActualWidth + stack.ComputedMarginLeft + stack.ComputedMarginRight).ToString(), stackMarginBorder.Width);

        Assert.NotNull(stackPaddingBorder);
        Assert.Equal(stack.ActualWidth.ToString(), stackPaddingBorder.Width);

        Assert.NotNull(stackContentBorder);
        Assert.Equal(stack.MeasuredContentWidth.ToString(), stackContentBorder.Width);

        // Verify diagonal lines
        var diagImages = debugOverlay.Children.OfType<Image>().ToList();
        Assert.Equal(4, diagImages.Count); // 2 for stack, 2 for label
        
        // Find diagonal lines with the same position as stackContentBorder
        var stackDiags = diagImages.Where(img => img.X == stackContentBorder.X && img.Y == stackContentBorder.Y).ToList();
        Assert.Equal(2, stackDiags.Count);
        Assert.Contains(stackDiags, img => img.Source == "__diag_tlbr__");
        Assert.Contains(stackDiags, img => img.Source == "__diag_trbl__");

        // Cleanup
        LayoutEngine.DebugMode = false;
    }

    [Fact]
    public void LayoutEngine_DebugModeOff_ReturnsNull()
    {
        LayoutEngine.DebugMode = false;
        var stack = new Stack();
        var overlay = LayoutEngine.GenerateDebugOverlay(stack);
        Assert.Null(overlay);
    }
}
