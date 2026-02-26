namespace SUIM.Tests.Binding;

using Xunit;
using SUIM;
using SUIM.Components;

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
        var markup = @"<button id=""btn"" onclick=""OnClick()"" />";
        
        var (root, _) = MarkupParser.Parse(markup, model);
        
        Assert.NotNull(root);
        Assert.IsType<Button>(root);
        Assert.True(root.Events.ContainsKey("click"));
        Assert.Equal("OnClick()", root.Events["click"]);
    }

    [Fact]
    public void MarkupParser_MapsEvent_ToDictionary_WithSender()
    {
        var model = new TestModel();
        var markup = @"<button id=""testBtn"" onclick=""OnMessage(this)"" />";
        
        var (root, _) = MarkupParser.Parse(markup, model);
        
        Assert.NotNull(root);
        
        Assert.True(root.Events.ContainsKey("click"));
        Assert.Equal("OnMessage(this)", root.Events["click"]);
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
        var handler = observableModel.GetHandler("QuitHandler()");

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
        var markup = @"<button id=""quitBtn"" onclick=""QuitHandler()"" />";
        
        var (root, _) = MarkupParser.Parse(markup, model);
        
        Assert.NotNull(root);
        Assert.IsType<Button>(root);
        Assert.True(root.Events.ContainsKey("click"));
        Assert.Equal("QuitHandler()", root.Events["click"]);
    }

    public class ModelWithArguments
    {
        public UIElement? Caller { get; private set; }
        public string? Info { get; private set; }
        public int Count { get; private set; }
        public bool Flag { get; private set; }

        public void MyFunction(UIElement caller, string info)
        {
            Caller = caller;
            Info = info;
        }

        public void MultiArgs(string s, int i, bool b)
        {
            Info = s;
            Count = i;
            Flag = b;
        }

        public Action<string>? DelegateHandler { get; set; }
        public string? DelegateMessage { get; set; }
    }

    [Fact]
    public void ResolveEventAction_WithMethodCall_AndThis_Works()
    {
        var model = new ModelWithArguments();
        var element = new Button { Id = "testBtn" };
        var expression = "MyFunction(this, 'clicked')";
        
        var handler = BackendHelpers.ResolveEventAction(expression, model, element);
        
        Assert.NotNull(handler);
        Assert.IsType<Action>(handler);
        
        ((Action)handler).Invoke();
        
        Assert.Equal(element, model.Caller);
        Assert.Equal("clicked", model.Info);
    }

    [Fact]
    public void ResolveEventAction_WithMultipleArguments_Works()
    {
        var model = new ModelWithArguments();
        var element = new Button();
        var expression = "MultiArgs('hello', 42, true)";
        
        var handler = BackendHelpers.ResolveEventAction(expression, model, element);
        
        Assert.NotNull(handler);
        ((Action)handler).Invoke();
        
        Assert.Equal("hello", model.Info);
        Assert.Equal(42, model.Count);
        Assert.True(model.Flag);
    }

    [Fact]
    public void ResolveEventAction_WithDelegateProperty_Works()
    {
        var model = new ModelWithArguments();
        model.DelegateHandler = (s) => model.DelegateMessage = s;
        var element = new Button();
        var expression = "DelegateHandler('from delegate')";
        
        var handler = BackendHelpers.ResolveEventAction(expression, model, element);
        
        Assert.NotNull(handler);
        ((Action)handler).Invoke();
        
        Assert.Equal("from delegate", model.DelegateMessage);
    }

    [Fact]
    public void ResolveEventAction_WithoutParentheses_ReturnsNull()
    {
        var model = new ModelWithArguments();
        var element = new Button();
        var expression = "MyFunction"; // Missing (...)
        
        var handler = BackendHelpers.ResolveEventAction(expression, model, element);
        
        Assert.Null(handler);
    }
}
