namespace SUIM.Tests.Parsing;

using Xunit;
using SUIM.Parse;
using SUIM.Parse.Components;

public class ControlFlowParserTests
{
    private readonly object _model =
        new
        {
            identifierbool = true,
            identifierbool2 = true,
            identifierbool3 = false,
            identifierany = 500,
            identifier2 = 500,
            Collection = new[] { "item1", "item2" },
            stringValue = "test",
            numericValue = 42,
            currentWidth = 250,
            invWidth = 500,
            items = new[] { new { Name = "Apple" }, new { Name = "Banana" } }
        };

    [Fact]
    public void Parse_IfDirective_True()
    {
        var markup = @"<div>
@if identifierbool
{
    <label>True</label>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        var text = (Text)label.Children[0];
        Assert.Equal("True", text.Value);
    }

    [Fact]
    public void Parse_IfElseDirective_True()
    {
        var markup = @"<div>
@if identifierbool2
{
    <h1>True</h1>
}
else
{
    <h1>False</h1>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<H1>(div.Children[0]);
        var t = (Text)div.Children[0].Children[0];
        Assert.Equal("True", t.Value);
    }

    [Fact]
    public void Parse_IfElseDirective_False()
    {
        var markup = @"<div>
@if identifierbool3
{
    <h1>True</h1>
}
else
{
    <h1>False</h1>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<H1>(div.Children[0]);
        var t = (Text)div.Children[0].Children[0];
        Assert.Equal("False", t.Value);
    }

    [Fact]
    public void Parse_IfElseIfElseDirective_True()
    {
        var markup = @"<div>
@if identifierbool3
{
    <h1>False</h1>
}
else if identifierbool3
{
    <h1>False</h1>
}
else
{
    <h1>True</h1>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<H1>(div.Children[0]);
        var t = (Text)div.Children[0].Children[0];
        Assert.Equal("True", t.Value);
    }

    [Fact]
    public void Parse_IfElseIfElseIfElseDirective_FinalElse()
    {
        var markup = @"<div>
@if identifierbool3
{
    <h1>False</h1>
}
else if identifierbool3
{
    <h1>False</h1>
}
else if identifierbool3
{
    <h1>False</h1>
}
else
{
    <h1>FinalElse</h1>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<H1>(div.Children[0]);
        var t = (Text)div.Children[0].Children[0];
        Assert.Equal("FinalElse", t.Value);
    }

    [Fact]
    public void Parse_IfElseIfElseIfElseDirective_True()
    {
        var markup = @"<div>
@if identifierbool3
{
    <h1>False</h1>
}
else if identifierbool3
{
    <h1>False</h1>
}
else if identifierbool2
{
    <h1>True</h1>
}
else
{
    <h1>False</h1>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<H1>(div.Children[0]);
        var t = (Text)div.Children[0].Children[0];
        Assert.Equal("True", t.Value);
    }

    [Fact]
    public void Parse_ForDirectiveLabel()
    {
        var markup = @"<stack>
@for i=0 count=3
{
    <label>@i</label>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        for (int i = 0; i < 3; i++)
        {
            Assert.IsType<Label>(stack.Children[i]);
            var label = (Label)stack.Children[i];
            var text = (Text)label.Children[0];
            Assert.Equal(i.ToString(), text.Value);
        }
    }

    [Fact]
    public void Parse_ForDirective()
    {
        var markup = @"<stack>
@for i=0 count=3
{
    <label>@i</label>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        for (int i = 0; i < 3; i++)
        {
            Assert.IsType<Label>(stack.Children[i]);
            var label = (Label)stack.Children[i];
            var text = (Text)label.Children[0];
            Assert.Equal(i.ToString(), text.Value);
        }
    }

    [Fact]
    public void Parse_ForDirectiveWithoutModel()
    {
        var markup = @"<stack>
@for i=0 count=3
{
    <h1>@i</h1>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        for (int i = 0; i < 3; i++)
        {
            Assert.IsType<H1>(stack.Children[i]);
            var t = (Text)stack.Children[i].Children[0];
            Assert.Equal(i.ToString(), t.Value);
        }
    }

    [Fact]
    public void Parse_SwitchDirective()
    {
        var markup = @"<div>
@switch identifierany
{
    case 500
    {
        <h1>Matched</h1>
    }
    default
    {
        <h1>Default</h1>
    }
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<H1>(div.Children[0]);
        var t = (Text)div.Children[0].Children[0];
        Assert.Equal("Matched", t.Value);
    }

    [Fact]
    public void ControlFlow_SwitchDirective()
    {
        var parser = new ControlFlowParser(Create(_model));
        var markup = @"@switch identifierany
{
    case 500
    {
        <label>Matched</label>
    }
    default
    {
        <label>Default</label>
    }
}";
        var expanded = parser.ExpandDirectives(markup);
        Assert.Equal("<label>Matched</label>", expanded.Trim());
    }

    // ============== CONTROL FLOW - FOR WITH STEP ==============

    [Fact]
    public void Parse_ForDirective_WithNegativeStep()
    {
        var markup = @"<stack>
@for i=2 count=3 step=-1
{
    <label>@i</label>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        // Should contain: 2, 1, 0
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("2", ((Text)labels.ElementAt(0).Children[0]).Value);
        Assert.Equal("1", ((Text)labels.ElementAt(1).Children[0]).Value);
        Assert.Equal("0", ((Text)labels.ElementAt(2).Children[0]).Value);
    }

    [Fact]
    public void Parse_ForDirective_WithCustomStep()
    {
        var markup = @"<stack>
@for i=0 count=3 step=2
{
    <label>@i</label>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("0", ((Text)labels.ElementAt(0).Children[0]).Value);
        Assert.Equal("2", ((Text)labels.ElementAt(1).Children[0]).Value);
        Assert.Equal("4", ((Text)labels.ElementAt(2).Children[0]).Value);
    }

    // ============== CONTROL FLOW - SWITCH WITH STRING ==============

    [Fact]
    public void Parse_SwitchDirective_WithStringCase()
    {
        var markup = @"<div>
@switch stringValue
{
    case ""test""
    {
        <label>Matched String</label>
    }
    default
    {
        <label>No Match</label>
    }
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        var label = (Label)div.Children[0];
        var text = (Text)label.Children[0];
        Assert.Equal("Matched String", text.Value);
    }

    [Fact]
    public void Parse_SwitchDirective_WithMultipleCases()
    {
        var markup = @"<div>
@switch identifierany
{
    case 100
    {
        <h1>Hundred</h1>
    }
    case 500
    {
        <h1>FiveHundred</h1>
    }
    default
    {
        <h1>Other</h1>
    }
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        var t = (Text)div.Children[0].Children[0];
        Assert.Equal("FiveHundred", t.Value);
    }

    [Fact]
    public void ControlFlow_SwitchDirective_WithVariableCase()
    {
        var parser = new ControlFlowParser(Create(_model));
        var markup = @"@switch identifierany
{
    case @identifier2
    {
        <h1 value=""Variable Match""></h1>
    }
    default
    {
        <h1 value=""No Match""></h1>
    }
}";
        var expanded = parser.ExpandDirectives(markup);
        Assert.Contains("Variable Match", expanded);
    }

    // ============== CONTROL FLOW - FOREACH TESTS ==============

    [Fact]
    public void Parse_ForEach_WithCollection()
    {
        var markup = @"<stack>
@foreach item in Collection
{
    <h1>@item</h1>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(2, stack.Children.Count);
        var labels = stack.Children.Cast<H1>().Select(x => x.Children[0]).Cast<Text>().ToList();
        Assert.Equal("item1", labels.ElementAt(0).Value);
        Assert.Equal("item2", labels.ElementAt(1).Value);
    }

    [Fact]
    public void Parse_ForEach_WithCollectionProperty()
    {
        var markup = @"<stack>
@foreach item in items
{
    <h1>@item.Name</h1>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(2, stack.Children.Count);
        var labels = stack.Children.Cast<H1>().Select(x => x.Children[0]).Cast<Text>().ToList();
        Assert.Equal("Apple", labels.ElementAt(0).Value);
        Assert.Equal("Banana", labels.ElementAt(1).Value);
    }

    [Fact]
    public void Parse_ForEach_WithRange()
    {
        var markup = @"<stack>
@foreach i in 0..3
{
    <h1>@i</h1>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        var labels = stack.Children.Cast<H1>().Select(x => x.Children[0]).Cast<Text>().ToList();
        Assert.Equal("0", labels.ElementAt(0).Value);
        Assert.Equal("1", labels.ElementAt(1).Value);
        Assert.Equal("2", labels.ElementAt(2).Value);
    }

    // ============== COMPLEX NESTING & COMBINATIONS ==============

    [Fact]
    public void Parse_ControlFlow_IfWithin_Button()
    {
        var markup = @"<button>
@if identifierbool
{
    <h1>Click Me</h1>
}
else
{
    <h1>Disabled</h1>
}
</button>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        var label = (Text)button.Children[0].Children[0];
        Assert.Equal("Click Me", label.Value);
    }

    // ============== CONTROL FLOW - IF WITHOUT ELSE ==============

    [Fact]
    public void Parse_IfDirective_False_NoElement()
    {
        var markup = @"<div>
@if identifierbool3
{
    <h1 value=""Should not appear""></h1>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Empty(div.Children);
    }

    private static dynamic Create(object model)
    {
        var observable = new Model.ObservableObject();
        observable.Initialize(model);
        return observable;
    }
}
