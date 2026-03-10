namespace SUIM.Tests.Parsing;

using Xunit;
using SUIM.Parse;
using SUIM.Parse.Components;

public class CodeBehindTests
{
    public class MyTestComponent : UIComponent
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
        var (rootElement, _) = MarkupParser.Parse(markup);
        component.Children.Add(rootElement);
        var result = component.Expand();

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
}
