namespace SUIM.Tests;

using System;
using System.IO;
using Xunit;
using SUIM.Layout;
using SUIM.Parse;
using SUIM.Parse.Components;

public class ComponentStylingTests
{
    private static string GetTestPath(string filename)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "styling_tests", filename);
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
        return path;
    }

    [Fact]
    public void DefaultBehavior_StylesLeakDownwardToChildren()
    {
        var childPath = GetTestPath("ChildComp.suim");
        var childMarkup = @"<div id=""childLabel"" class=""highlight"" />";
        File.WriteAllText(childPath, childMarkup);
        ComponentRegistry.Register("ChildComp", childPath);

        var parentMarkup = @"<div id=""parent"">
            <style>
                .highlight { background: blue; }
            </style>
            <ChildComp />
        </div>";

        var (root, _) = MarkupParser.Parse(parentMarkup, basePath: AppDomain.CurrentDomain.BaseDirectory);

        var div = root as Div;
        var childComp = div!.Children[0] as CustomComponent;
        var childDiv = childComp!.Children[0] as Div;

        // Styles defined in parent should leak to child
        Assert.Equal("blue", childDiv!.BackgroundColor);
    }

    [Fact]
    public void DefaultBehavior_StylesDoNotLeakUpwardToParent()
    {
        var childPath = GetTestPath("ChildCompUp.suim");
        var childMarkup = @"<div id=""child"">
            <style>
                div { background: red; }
            </style>
        </div>";
        File.WriteAllText(childPath, childMarkup);
        ComponentRegistry.Register("ChildCompUp", childPath);

        var parentMarkup = @"<div id=""parent"">
            <ChildCompUp />
        </div>";

        var (root, _) = MarkupParser.Parse(parentMarkup, basePath: AppDomain.CurrentDomain.BaseDirectory);

        var parentDiv = root as Div;
        // Parent should NOT be red
        Assert.Null(parentDiv!.BackgroundColor);

        var childComp = parentDiv.Children[0] as CustomComponent;
        var childDiv = childComp!.Children[0] as Div;
        // Child SHOULD be red
        Assert.Equal("red", childDiv!.BackgroundColor);
    }

    [Fact]
    public void DefaultBehavior_StylesDoNotLeakToSiblings()
    {
        var child1Path = GetTestPath("Child1.suim");
        var child1Markup = @"<div id=""child1"">
            <style>
                .siblingStyle { background: green; }
            </style>
        </div>";
        File.WriteAllText(child1Path, child1Markup);
        ComponentRegistry.Register("Child1", child1Path);

        var child2Path = GetTestPath("Child2.suim");
        var child2Markup = @"<div id=""child2"" class=""siblingStyle"" />";
        File.WriteAllText(child2Path, child2Markup);
        ComponentRegistry.Register("Child2", child2Path);

        var parentMarkup = @"<div id=""parent"">
            <Child1 />
            <Child2 />
        </div>";

        var (root, _) = MarkupParser.Parse(parentMarkup, basePath: AppDomain.CurrentDomain.BaseDirectory);

        var parentDiv = (Div)root;
        var c1Comp = (CustomComponent)parentDiv.Children[0];
        var c2Comp = (CustomComponent)parentDiv.Children[1];

        var c2Div = (Div)c2Comp.Children[0];
        // Child2 should NOT be green (styles from Child1 should not leak to sibling)
        Assert.NotEqual("green", c2Div.Color);
    }

    [Fact]
    public void ScopedStyle_DoesNotLeakDownwardToChildren()
    {
        var childPath = GetTestPath("ChildCompScoped.suim");
        var childMarkup = @"<div id=""childLabel"" class=""scopedHighlight"" />";
        File.WriteAllText(childPath, childMarkup);
        ComponentRegistry.Register("ChildCompScoped", childPath);

        var parentMarkup = @"<div id=""parent"">
            <style scoped=""true"">
                .scopedHighlight { background: yellow; }
            </style>
            <ChildCompScoped />
        </div>";

        var (root, _) = MarkupParser.Parse(parentMarkup, basePath: AppDomain.CurrentDomain.BaseDirectory);

        var div = (Div)root;
        var childComp = (CustomComponent)div.Children[0];
        var childDiv = (Div)childComp.Children[0];

        // Style is scoped, so child should NOT have the background color
        Assert.Null(childDiv.BackgroundColor);
    }

    [Fact]
    public void ScopedStyle_StillAppliesToLocalElements()
    {
        var parentMarkup = @"<div id=""parent"">
            <style scoped=""true"">
                .localHighlight { background: purple; }
            </style>
            <div id=""local"" class=""localHighlight"" />
        </div>";

        var (root, _) = MarkupParser.Parse(parentMarkup, basePath: AppDomain.CurrentDomain.BaseDirectory);

        var div = (Div)root;
        var localDiv = (Div)div.Children[0];

        // Style is scoped, but it SHOULD apply to local elements
        Assert.Equal("purple", localDiv.BackgroundColor);
    }

    [Fact]
    public void Styles_LeakThroughMultipleLevels()
    {
        var grandchildPath = GetTestPath("Grandchild.suim");
        var grandchildMarkup = @"<div id=""gc"" class=""gpStyle"" />";
        File.WriteAllText(grandchildPath, grandchildMarkup);
        ComponentRegistry.Register("Grandchild", grandchildPath);

        var childPath = GetTestPath("ChildForGP.suim");
        var childMarkup = @"<Grandchild />";
        File.WriteAllText(childPath, childMarkup);
        ComponentRegistry.Register("ChildForGP", childPath);

        var gpMarkup = @"<div id=""gp"">
            <style>
                .gpStyle { background: cyan; }
            </style>
            <ChildForGP />
        </div>";

        var (root, _) = MarkupParser.Parse(gpMarkup, basePath: AppDomain.CurrentDomain.BaseDirectory);

        var gpDiv = (Div)root;
        var childComp = (CustomComponent)gpDiv.Children[0];
        // CustomComponent.Children[0] is the expanded element root (which is gcComp if Grandchild is a component)
        var gcComp = (CustomComponent)childComp.Children[0];
        var gcDiv = (Div)gcComp.Children[0];

        // Grandparent style should leak all the way to grandchild
        Assert.Equal("cyan", gcDiv.BackgroundColor);
    }

    [Fact]
    public void Button_WithPixelSizeFromCSS_ShouldMeasureCorrectly()
    {
        // Create a simple button with CSS-style sizing
        var markup = @"
            <grid>
                <style>
                    button {
                        width: 200px;
                        height: 50px;
                        margin: 5px;
                    }
                </style>
                <vstack>
                    <button>Test</button>
                </vstack>
            </grid>";

        var (suimElement, _) = MarkupParser.Parse(markup);

        // Check that the button has the right width/height attributes
        var vstack = suimElement.Children[0];
        var button = vstack.Children[0];

        Assert.NotNull(button);
        Assert.Equal("200px", button.Width);
        Assert.Equal("50px", button.Height);
        Assert.Equal("5px", button.Margin);

        // Now layout it
        LayoutEngine.Layout(suimElement, 16, 1280, 720);

        // Check actual dimensions
        var buttonActual = vstack.Children[0];
        Assert.Equal(200, buttonActual.ActualWidth);
        Assert.Equal(50, buttonActual.ActualHeight);
    }

    [Fact]
    public void MultipleSelectorRules_MergePropertiesWithCascading()
    {
        // Test that multiple rules with the same selector merge properties,
        // with later rules overriding earlier ones
        var markup = @"
            <grid>
                <style>
                    button {
                        width: 100px;
                        height: 40px;
                        margin: 2px;
                        color: red;
                    }
                </style>
                <style>
                    button {
                        width: 200px;
                        height: 50px;
                        margin: 5px;
                    }
                </style>
                <button>Test</button>
            </grid>";

        var (suimElement, _) = MarkupParser.Parse(markup);
        var button = suimElement.Children[0];

        // The second button rule should merge with and override the first
        // Expected: width=200px, height=50px, margin=5px, color=red (from first rule, not overridden)
        Assert.Equal("200px", button.Width);
        Assert.Equal("50px", button.Height);
        Assert.Equal("5px", button.Margin);
        Assert.Equal("red", button.Color);
    }

    [Fact]
    public void StyleFileAndInlineStyles_MergeWithCascading()
    {
        // Test that styles from external files merge with inline styles
        var cssPath = GetTestPath("MergeTest.css");
        var cssContent = @"button {
    width: 100px;
    height: 40px;
    color: blue;
}";
        File.WriteAllText(cssPath, cssContent);

        var markup = $@"
            <grid>
                <style src=""{Path.GetFileName(cssPath)}"" />
                <style>
                    button {{
                        width: 200px;
                        height: 50px;
                        margin: 5px;
                    }}
                </style>
                <button>Test</button>
            </grid>";

        var (suimElement, _) = MarkupParser.Parse(markup, basePath: GetTestPath(""));
        var button = suimElement.Children[0];

        // External CSS should be merged with inline style
        // Inline style (later) should override CSS file values for width/height
        // Color should come from CSS (not overridden by inline)
        Assert.Equal("200px", button.Width);
        Assert.Equal("50px", button.Height);
        Assert.Equal("5px", button.Margin);
        Assert.Equal("blue", button.Color);
    }
}
