namespace SUIM.Tests;

using Xunit;
using SUIM.Layout;
using SUIM.Components;

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
}