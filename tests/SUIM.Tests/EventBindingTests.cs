namespace SUIM.Tests;

using SUIM;
using SUIM.Components;
using Xunit;

public class EventBindingTests
{
    public class TestModel
    {
        public bool Clicked { get; private set; }
        public string LastMessage { get; private set; } = "";

        public void OnClick()
        {
            Clicked = true;
        }

        public void OnMessage(UIElement sender)
        {
            LastMessage = sender.Id ?? "Unknown";
        }
    }

    [Fact]
    public void MarkupParser_MapsEvent_ToDictionary()
    {
        var model = new TestModel();
        var markup = @"<button id=""btn"" onclick=""OnClick"" />";
        
        var (root, _) = MarkupParser.Parse(markup, model);
        
        Assert.NotNull(root);
        Assert.IsType<Button>(root);
        Assert.True(root.Events.ContainsKey("click"));
        Assert.Equal("OnClick", root.Events["click"]);
    }

    [Fact]
    public void MarkupParser_MapsEvent_ToDictionary_WithSender()
    {
        var model = new TestModel();
        var markup = @"<button id=""testBtn"" onclick=""OnMessage"" />";
        
        var (root, _) = MarkupParser.Parse(markup, model);
        
        Assert.NotNull(root);
        
        Assert.True(root.Events.ContainsKey("click"));
        Assert.Equal("OnMessage", root.Events["click"]);
    }

    [Fact]
    public void MarkupParser_BindsEvent_ToDelegate()
    {
        var model = new {
            MyHandler = new Action(() => { })
        };

        var markup = @"<button onclick=""MyHandler"" />";
        
        var (root, _) = MarkupParser.Parse(markup, model);
        
        Assert.True(root.Events.ContainsKey("click"));
        Assert.Equal("MyHandler", root.Events["click"]);
    }
}
