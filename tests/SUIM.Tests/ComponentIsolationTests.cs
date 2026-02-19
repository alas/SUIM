namespace SUIM.Tests;

using Xunit;
using SUIM;
using SUIM.Components;
using SUIM.StrideIntegration;
using System.IO;
using System.Collections.Generic;
using Stride.Engine;
using System;

public class ComponentIsolationTests
{
    private string GetTestPath(string filename)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
        return path;
    }

    [Fact]
    public void Component_HasIsolatedModel()
    {
        var compPath = GetTestPath("TestComponent.suim");
        var componentMarkup = @"<div id=""compRoot"">
            <model>{ ""compProp"": 123.0 }</model>
            <label value=""@compProp"" />
        </div>";
        File.WriteAllText(compPath, componentMarkup);

        // Register the custom tag with absolute path
        ComponentRegistry.Register("TestComponent", compPath);

        // Arrange
        var markup = @"<div id=""root"">
            <TestComponent parentProp=""@rootProp"" />
        </div>";

        var model = new ObservableObject();
        model.SetValue("rootProp", "hello");

        // Act
        var (root, _) = MarkupParser.Parse(markup, model, basePath: AppDomain.CurrentDomain.BaseDirectory);

        // Assert
        var div = root as Div;
        Assert.NotNull(div);
        var component = div.Children[0] as CustomComponent;
        Assert.NotNull(component);
        
        Assert.NotNull(component.Model);
        Assert.NotSame(model, component.Model);
        
        var compModel = component.Model as ObservableObject;
        Assert.Equal(123.0, (double)compModel.GetValue("compProp"));
        
        // Verify parent property was mapped (2-way proxy)
        Assert.Equal("hello", compModel.GetValue("parentProp"));

        // Verify 2-way update
        compModel.SetValue("parentProp", "changed");
        Assert.Equal("changed", model.GetValue("rootProp"));
    }

    [Fact]
    public void RootTag_MatchingFilename_BypassesWrapper()
    {
        var compPath = GetTestPath("TestComp.suim");
        // Arrange
        var componentMarkup = @"<TestComp>
            <model>{ ""val"": 10.0 }</model>
            <label value=""@val"" />
        </TestComp>";
        File.WriteAllText(compPath, componentMarkup);

        // Act
        var component = new CustomComponent("TestComp") { Source = compPath };
        component.Expand(new ObservableObject(), basePath: AppDomain.CurrentDomain.BaseDirectory);

        // Assert
        // component (CustomComponent) -> Div (from TestComp tag) -> Label
        Assert.Single(component.Children);
        var div = component.Children[0] as Div;
        Assert.NotNull(div);
        Assert.Equal("testcomp", div.TagName);
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
    }

    [Fact]
    public void NestedBinding_ThrowsException_IfModelMissingInComponent()
    {
        var compPath = GetTestPath("NoModelComp.suim");
        // Arrange
        var componentMarkup = @"<div id=""comp"">
            <label value=""@missingProp"" />
        </div>";

        File.WriteAllText(compPath, componentMarkup);
        var tagName = "NoModelComp_" + Guid.NewGuid().ToString("N");
        ComponentRegistry.Register(tagName, compPath);

        var suim = new SUIMStride();
        suim.RootPath = AppDomain.CurrentDomain.BaseDirectory;
        var markup = $"<{tagName} />";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => {
            var (root, _) = suim.Parse(markup, new Game());
        });
        Assert.Contains("no model context is available", ex.Message);
    }
}
