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
    <label value=""True""></label>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("True", label.Value);
    }

    [Fact]
    public void Parse_IfElseDirective_True()
    {
        var markup = @"<div>
@if identifierbool2
{
    <label value=""True""></label>
}
else
{
    <label value=""False""></label>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("True", label.Value);
    }

    [Fact]
    public void Parse_IfElseDirective_False()
    {
        var markup = @"<div>
@if identifierbool3
{
    <label value=""True""></label>
}
else
{
    <label value=""False""></label>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("False", label.Value);
    }

    [Fact]
    public void Parse_IfElseIfElseDirective_True()
    {
        var markup = @"<div>
@if identifierbool3
{
    <label value=""False""></label>
}
else if identifierbool3
{
    <label value=""False""></label>
}
else
{
    <label value=""True""></label>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("True", label.Value);
    }

    [Fact]
    public void Parse_IfElseIfElseIfElseDirective_FinalElse()
    {
        var markup = @"<div>
@if identifierbool3
{
    <label value=""False""></label>
}
else if identifierbool3
{
    <label value=""False""></label>
}
else if identifierbool3
{
    <label value=""False""></label>
}
else
{
    <label value=""FinalElse""></label>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("FinalElse", label.Value);
    }

    [Fact]
    public void Parse_IfElseIfElseIfElseDirective_True()
    {
        var markup = @"<div>
@if identifierbool3
{
    <label value=""False""></label>
}
else if identifierbool3
{
    <label value=""False""></label>
}
else if identifierbool2
{
    <label value=""True""></label>
}
else
{
    <label value=""False""></label>
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("True", label.Value);
    }

    [Fact]
    public void Parse_ForDirective()
    {
        var markup = @"<stack>
@for i=0 count=3
{
    <label value=""@i""></label>
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
            Assert.Equal(i.ToString(), label.Value);
        }
    }

    [Fact]
    public void Parse_ForDirectiveWithoutModel()
    {
        var markup = @"<stack>
@for i=0 count=3
{
    <label value=""@i""></label>
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
            Assert.Equal(i.ToString(), label.Value);
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
        <label value=""Matched""></label>
    }
    default
    {
        <label value=""Default""></label>
    }
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<Label>(div.Children[0]);
        var label = (Label)div.Children[0];
        Assert.Equal("Matched", label.Value);
    }

    [Fact]
    public void ControlFlow_SwitchDirective()
    {
        var parser = new ControlFlowParser(Create(_model));
        var markup = @"@switch identifierany
{
    case 500
    {
        <label value=""Matched""></label>
    }
    default
    {
        <label value=""Default""></label>
    }
}";
        var expanded = parser.ExpandDirectives(markup);
        Assert.Equal("<label value=\"Matched\"></label>", expanded.Trim());
    }

    // ============== CONTROL FLOW - FOR WITH STEP ==============

    [Fact]
    public void Parse_ForDirective_WithNegativeStep()
    {
        var markup = @"<stack>
@for i=2 count=3 step=-1
{
    <label value=""@i""></label>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        // Should contain: 2, 1, 0
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("2", labels.ElementAt(0).Value);
        Assert.Equal("1", labels.ElementAt(1).Value);
        Assert.Equal("0", labels.ElementAt(2).Value);
    }

    [Fact]
    public void Parse_ForDirective_WithCustomStep()
    {
        var markup = @"<stack>
@for i=0 count=3 step=2
{
    <label value=""@i""></label>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("0", labels.ElementAt(0).Value);
        Assert.Equal("2", labels.ElementAt(1).Value);
        Assert.Equal("4", labels.ElementAt(2).Value);
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
        <label value=""Matched String""></label>
    }
    default
    {
        <label value=""No Match""></label>
    }
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        var label = (Label)div.Children[0];
        Assert.Equal("Matched String", label.Value);
    }

    [Fact]
    public void Parse_SwitchDirective_WithMultipleCases()
    {
        var markup = @"<div>
@switch identifierany
{
    case 100
    {
        <label value=""Hundred""></label>
    }
    case 500
    {
        <label value=""FiveHundred""></label>
    }
    default
    {
        <label value=""Other""></label>
    }
}
</div>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        var label = (Label)div.Children[0];
        Assert.Equal("FiveHundred", label.Value);
    }

    [Fact]
    public void ControlFlow_SwitchDirective_WithVariableCase()
    {
        var parser = new ControlFlowParser(Create(_model));
        var markup = @"@switch identifierany
{
    case @identifier2
    {
        <label value=""Variable Match""></label>
    }
    default
    {
        <label value=""No Match""></label>
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
    <label value=""@item""></label>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(2, stack.Children.Count);
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("item1", labels.ElementAt(0).Value);
        Assert.Equal("item2", labels.ElementAt(1).Value);
    }

    [Fact]
    public void Parse_ForEach_WithCollectionProperty()
    {
        var markup = @"<stack>
@foreach item in items
{
    <label value=""@item.Name""></label>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(2, stack.Children.Count);
        var labels = stack.Children.Cast<Label>();
        Assert.Equal("Apple", labels.ElementAt(0).Value);
        Assert.Equal("Banana", labels.ElementAt(1).Value);
    }

    [Fact]
    public void Parse_ForEach_WithRange()
    {
        var markup = @"<stack>
@foreach i in 0..3
{
    <label value=""@i""></label>
}
</stack>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(3, stack.Children.Count);
        var labels = stack.Children.Cast<Label>();
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
    <label value=""Click Me""></label>
}
else
{
    <label value=""Disabled""></label>
}
</button>";
        var (element, _) = MarkupParser.Parse(markup, _model);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        var label = (Label)button.Children[0];
        Assert.Equal("Click Me", label.Value);
    }

    // ============== CONTROL FLOW - IF WITHOUT ELSE ==============

    [Fact]
    public void Parse_IfDirective_False_NoElement()
    {
        var markup = @"<div>
@if identifierbool3
{
    <label value=""Should not appear""></label>
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
