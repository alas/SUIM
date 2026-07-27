namespace SUIM.Tests.Parsing;

using SUIM.Model;
using SUIM.Parse;
using SUIM.Parse.Components;
using Xunit;

public class CodeBehindTests
{
    public class MyTestComponent : VirtualComponent
    {
        public bool Clicked { get; private set; }
        public object? ClickParam { get; private set; }
        public int IntParam { get; private set; }

        public MyTestComponent() : base(nameof(MyTestComponent)) { }

        public void OnClick()
        {
            Clicked = true;
        }

        public void OnClickWithParam(string p)
        {
            ClickParam = p;
        }

        public void OnClickWithMultiple(int i, string s, bool b)
        {
            IntParam = i;
            ClickParam = s;
        }

        public void OnClickWithThis(UIElement element)
        {
            ClickParam = element.Id;
        }
    }

    [Fact]
    public void UIComponent_HandlesMarkupParameters()
    {
        var markup = @"<stack>
    <button id=""btn1"" onclick=""OnClickWithParam('hello')"">Click Me</button>
    <button id=""btn2"" onclick=""OnClickWithMultiple(42, 'world', true)"">Click Me 2</button>
    <button id=""btn3"" onclick=""OnClickWithThis(this)"">Click Me 3</button>
</stack>";

        var component = new MyTestComponent();
        var rootElement = MarkupParser.Parse(markup);
        component.Children.Add(rootElement);
        EventHandlerResolver.BindEventsRecursive(rootElement, component);

        var btn1 = XPathHelper.FindElementByPath(component, "btn1") as UIElement;
        btn1!.TriggerEvent("click");
        Assert.Equal("hello", component.ClickParam);

        var btn2 = XPathHelper.FindElementByPath(component, "btn2") as UIElement;
        btn2!.TriggerEvent("click");
        Assert.Equal(42, component.IntParam);
        Assert.Equal("world", component.ClickParam);

        var btn3 = XPathHelper.FindElementByPath(component, "btn3") as UIElement;
        btn3!.TriggerEvent("click");
        Assert.Equal("btn3", component.ClickParam);
    }

    [Fact]
    public void ViewCodeBehind_ResolvesClickHandlers_WhenParsingMarkup()
    {
        var createdView = new TestView();
        ComponentRegistry.Register("ViewCodeBehindTest", () => createdView);

        var markup = @"<root><button id=""btn"" onclick=""OnClick()"" /></root>";

        var root = MarkupParser.Parse(markup, componentName: "ViewCodeBehindTest");
        var button = root as Button;

        button!.TriggerEvent("click");

        Assert.True(createdView.Clicked);
    }

    public class TestView : VirtualComponent
    {
        public bool Clicked { get; private set; }

        public TestView() : base(nameof(TestView)) { }

        public void OnClick() => Clicked = true;
    }
}
