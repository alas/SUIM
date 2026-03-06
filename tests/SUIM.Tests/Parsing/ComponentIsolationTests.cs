namespace SUIM.Tests.Parsing;

using System;
using System.IO;
using Xunit;
using Stride.Engine;
using Stride.UI.Events;
using Stride.UI.Panels;
using SUIM.Parse;
using SUIM.Model;
using SUIM.Parse.Components;
using SUIMStride;

public class ComponentIsolationTests
{
    private static string GetTestPath(string filename)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
        return path;
    }

    [Fact]
    public void Component_Attribute_Binding_Propagates_To_Internal_Label()
    {
        var compPath = GetTestPath("PopupTestComp.suim");
        var componentMarkup = """
            <div id="popuproot">
                <model>{ "title": "" }</model>
                <h1 id="thelabel">@title</h1>
            </div>
            """;
        File.WriteAllText(compPath, componentMarkup);

        ComponentRegistry.Register("PopupTestComp", compPath);

        var markup = "<div><PopupTestComp title=\"@PopupTitle\" /></div>";

        var suim = new Parser
        {
            RootPath = AppDomain.CurrentDomain.BaseDirectory
        };

        var parentModel = new { PopupTitle = "HelloWorld" };
        var (strideRoot, model) = suim.Parse(markup, new Game(), model: parentModel);

        // Find first TextBlock in the mapped Stride UI tree and assert its text was set from parent model
        var found = XPath.Find(strideRoot, "thelabel") as Canvas;
        var text = found?.Children[0] as Stride.UI.Controls.TextBlock;
        Assert.NotNull(text);
        Assert.Equal("HelloWorld", text!.Text);

        model!.PopupTitle = "Changed";
        Assert.Equal("Changed", text!.Text);
    }

    [Fact]
    public void Component_Attribute_Binding_Propagates_To_Internal_LabelVisibility_NullComponentModel()
    {
        var compPath = GetTestPath("PopupTestComp.suim");
        var componentMarkup = """
            <div id="popuproot">
                <model>{ "labelvisibility": null }</model>
                <h1 id="thelabel" visibility="@labelvisibility"></h1>
            </div>
            """;
        File.WriteAllText(compPath, componentMarkup);

        ComponentRegistry.Register("PopupTestComp", compPath);

        var markup = "<div><PopupTestComp labelvisibility=\"@LabelVisibility\" /></div>";

        var suim = new Parser
        {
            RootPath = AppDomain.CurrentDomain.BaseDirectory
        };

        var parentModel = new { LabelVisibility = "collapsed" };
        var (strideRoot, model) = suim.Parse(markup, new Game(), model: parentModel);

        // Find first TextBlock in the mapped Stride UI tree and assert its text was set from parent model
        var found = XPath.Find(strideRoot, "thelabel") as Canvas;
        Assert.NotNull(found);
        Assert.Equal(Stride.UI.Visibility.Collapsed, found!.Visibility);

        model!.LabelVisibility = Stride.UI.Visibility.Visible;
        Assert.Equal(Stride.UI.Visibility.Visible, found!.Visibility);
    }

    [Fact]
    public void Component_Attribute_Binding_Propagates_To_Internal_LabelVisibility_NullStartValueOnGlobalModel()
    {
        var compPath = GetTestPath("PopupTestComp.suim");
        var componentMarkup =
            """
            <div id="popuproot">
                <model>{ "labelvisibility": "block" }</model>
                <h1 id="thelabel" visibility="@labelvisibility"></h1>
            </div>
            """;
        File.WriteAllText(compPath, componentMarkup);

        ComponentRegistry.Register("PopupTestComp", compPath);

        var markup = "<div><model>{ \"LabelVisibility\": \"none\" }</model><PopupTestComp labelvisibility=\"@LabelVisibility\" /></div>";

        var suim = new Parser
        {
            RootPath = AppDomain.CurrentDomain.BaseDirectory
        };

        var parentModel = new { LabelVisibility = "collapsed" };
        var (strideRoot, model) = suim.Parse(markup, new Game(), model: parentModel);

        // Find first TextBlock in the mapped Stride UI tree and assert its text was set from parent model
        var found = XPath.Find(strideRoot, "thelabel") as Canvas;
        Assert.NotNull(found);
        Assert.Equal(Stride.UI.Visibility.Collapsed, found!.Visibility);

        model!.LabelVisibility = Stride.UI.Visibility.Visible;
        Assert.Equal(Stride.UI.Visibility.Visible, found!.Visibility);
    }

    [Fact]
    public void Component_Attribute_Binding_Propagates_To_Internal_ButtonClick()
    {
        var compPath = GetTestPath("PopupTestComp.suim");
        var componentMarkup = """
            <div id="popuproot">
                <model>{ "onbuttonclick": null }</model>
                <button id="thebutton" onclick="@onbuttonclick"></button>
            </div>
            """;
        File.WriteAllText(compPath, componentMarkup);

        ComponentRegistry.Register("PopupTestComp", compPath);

        var markup = @"<div><PopupTestComp onbuttonclick=""MyHandler()"" /></div>";

        var suim = new Parser
        {
            RootPath = AppDomain.CurrentDomain.BaseDirectory
        };

        var called = false;
        var parentModel = new { MyHandler = 
            new Action(() => 
            {
                called = true;
            }) };
        var game = new Game();
        var (strideRoot, _) = suim.Parse(markup, game, model: parentModel);

        var found = XPath.Find(strideRoot, "thebutton") as Stride.UI.Controls.Button;
        Assert.NotNull(found);
        // simulate click by invoking the bound handler (test hook)
        var handler = suim.GetBoundClickHandler(found!);
        Assert.NotNull(handler);
        // Invoke as RoutedEvent handler
        handler!.DynamicInvoke(found, new RoutedEventArgs());
        Assert.True(called);
    }

    [Fact]
    public void Component_HasIsolatedModel()
    {
        var compPath = GetTestPath("TestComponent.suim");
        var componentMarkup = @"<div id=""compRoot"">
            <model>{ ""compProp"": 123.0 }</model>
            <label>@compProp</label>
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
        Assert.Equal(123.0, (double)compModel!.GetValue("compProp")!);
        
        // Verify parent property was mapped (2-way proxy)
        Assert.Equal("hello", compModel.GetValue("parentProp"));

        // Verify 2-way update
        compModel.SetValue("parentProp", "changed");
        Assert.Equal("changed", model.GetValue("rootProp"));
    }

    [Fact]
    public void RootTag_MatchingFilename_BypassesWrapper()
    {
        // Arrange
        var compPath = GetTestPath("TestComp.suim");
        var componentMarkup = @"<TestComp>
            <model>{ ""val"": 10.0 }</model>
            <label>@val</label>
        </TestComp>";
        File.WriteAllText(compPath, componentMarkup);

        // Act
        var component = new CustomComponent("TestComp") { Source = compPath };
        component.Expand(new ObservableObject(), basePath: AppDomain.CurrentDomain.BaseDirectory);

        // Assert
        Assert.Single(component.Children);
        Assert.IsType<Label>(component.Children[0]);
        var label = component.Children[0] as Label;
        Assert.IsType<Text>(label!.Children[0]);
        var text = label!.Children[0] as Text;
        Assert.Equal("@val", text!.Value);
    }

    [Fact]
    public void NestedBinding_ThrowsException_IfModelMissingInComponent()
    {
        var compPath = GetTestPath("NoModelComp.suim");
        // Arrange
        var componentMarkup = @"<div id=""comp"">
            <label>@missingProp</label>
        </div>";

        File.WriteAllText(compPath, componentMarkup);
        var tagName = "NoModelComp_" + Guid.NewGuid().ToString("N");
        ComponentRegistry.Register(tagName, compPath);

        var suim = new Parser
        {
            RootPath = AppDomain.CurrentDomain.BaseDirectory
        };
        var markup = $"<{tagName} />";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => {
            var (root, _) = suim.Parse(markup, new Game());
        });
        Assert.Contains("no model context is available", ex.Message);
    }
}
