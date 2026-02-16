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

    public class TestModelWithOverloading
    {
        public bool ParameterlessClicked { get; private set; }
        public string UIElementSenderName { get; private set; } = "";
        public bool EventHandlerClicked { get; private set; }

        // Overloaded QuitHandler methods with different priorities
        // Priority 1: Parameterless (should be selected first)
        public void QuitHandler()
        {
            ParameterlessClicked = true;
        }

        // Priority 2: Takes UIElement (should be selected if parameterless unavailable)
        public void QuitHandler(UIElement sender)
        {
            UIElementSenderName = sender.Id ?? "Unknown";
        }

        // Priority 3: EventHandler pattern (should be selected if above unavailable)
        public void QuitHandler(object sender, EventArgs e)
        {
            EventHandlerClicked = true;
        }
    }

    [Fact]
    public void GetHandler_WithOverloadedMethods_SelectsParameterlessFirst()
    {
        var model = new TestModelWithOverloading();
        var observableModel = new ObservableObject();
        observableModel.Initialize(model);

        // Get the parameterless handler
        var handler = observableModel.GetHandler("QuitHandler");

        Assert.NotNull(handler);
        Assert.IsType<Action>(handler);

        // Invoke and verify the parameterless method was called
        ((Action)handler)();
        Assert.True(model.ParameterlessClicked);
        Assert.Empty(model.UIElementSenderName);
        Assert.False(model.EventHandlerClicked);
    }

    [Fact]
    public void MarkupParser_BindsOverloadedEventHandler_ToParameterlessMethod()
    {
        var model = new TestModelWithOverloading();
        var markup = @"<button id=""quitBtn"" onclick=""QuitHandler"" />";
        
        var (root, _) = MarkupParser.Parse(markup, model);
        
        Assert.NotNull(root);
        Assert.IsType<Button>(root);
        Assert.True(root.Events.ContainsKey("click"));
        Assert.Equal("QuitHandler", root.Events["click"]);
    }
}
