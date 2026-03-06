namespace SUIM.Tests.Parsing;

using Xunit;
using Stride.Engine;
using Stride.UI.Panels;
using SUIMStride;

public class StrideParserTests
{
    private static Game CreateTestGame()
    {
        var game = new Game();
        game.GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
        game.GraphicsDeviceManager.PreferredBackBufferHeight = 720;
        return game;
    }

    [Fact]
    public void Div_MixedAutoFixed_MeasuresChildren()
    {
        // Simple View: Width="auto", Height="200"
        var markup = @"
<div style=""width:400px; height:200px;"">
    <div style=""width:auto; height:200px;"">
        <label style=""width:100px; height:50px;"">Hello World</label>
    </div>
</div>";
        var game = CreateTestGame();
        var (strideRoot, _) = new Parser().Parse(markup, game);

        // Find the Div in the Stride tree
        var div1 = (Canvas)strideRoot;
        var div2 = (Canvas)div1.Children[0];
        // Div should have Width=100 (from child) and Height=200 (fixed)
        Assert.Equal(400f, div2.Width);
        Assert.Equal(200f, div2.Height);
    }

    [Fact]
    public void Div_MixedAutoFixed_ShrinkToContent()
    {
        // Simple View: Width="auto", Height="200"
        var markup = @"
<div style=""align-items: flex-start; width:400px; height:200px;"">
    <div style=""width:auto; height:200;"">
        <label style=""width:100px; height:50px;"">Hello World</label>
    </div>
</div>";
        var game = CreateTestGame();
        var (strideRoot, _) = new Parser().Parse(markup, game);

        // Find the Div in the Stride tree
        var div1 = (Canvas)strideRoot;
        var div2 = (Canvas)div1.Children[0];
        // Div should have Width=100 (from child) and Height=200 (fixed)
        Assert.Equal(100f, div2.Width);
        Assert.Equal(200f, div2.Height);
    }
}
