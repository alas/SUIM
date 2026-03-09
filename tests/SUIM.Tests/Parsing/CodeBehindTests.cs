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
        public Button? MyButton { get; private set; }

        public MyTestComponent() : base("MyTestComponent") { }

        protected override void InitializeComponent()
        {
            MyButton = FindElement<Button>("btn1");
        }

        public void OnClick()
        {
            Clicked = true;
        }

        public void OnClickWithParam(object? p)
        {
            ClickParam = p;
        }
    }

    [Fact]
    public void UIComponent_LoadsMarkupAndBindsEvents()
    {
        var markup = @"<stack>
    <button id=""btn1"" onclick=""OnClick"">Click Me</button>
    <button id=""btn2"" onclick=""OnClickWithParam"">Click Me 2</button>
</stack>";

        var component = new MyTestComponent();
        component.LoadMarkup(markup);

        // Verify element lookup
        Assert.NotNull(component.MyButton);
        Assert.Equal("btn1", component.MyButton.Id);

        // Verify event binding (no param)
        var btn1 = component.FindElement<Button>("btn1");
        Assert.NotNull(btn1);
        btn1.TriggerEvent("click");
        Assert.True(component.Clicked);

        // Verify event binding (with param)
        var btn2 = component.FindElement<Button>("btn2");
        Assert.NotNull(btn2);
        btn2.TriggerEvent("click", "test-param");
        Assert.Equal("test-param", component.ClickParam);
    }

    [Fact]
    public void Debug_ParseMainView()
    {
        var rootPath = "..\\..\\..\\..\\..\\src\\Example\\Chess3d\\SUIM";
        var viewPath = Path.Combine(rootPath, "views", "MainView.suim");
        var markup = File.ReadAllText(viewPath);
        
        var project = new SUIMProject(rootPath);
        project.ResolveDependencies(markup);

        var (element, model) = MarkupParser.Parse(markup, null, null, rootPath);
        Assert.NotNull(element);
    }
}
