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
@if (identifierbool)
{
    <label>True</label>
}
</div>";
        var element = MarkupParser.Parse(markup, _model);

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
@if (identifierbool2)
{
    <h1>True</h1>
}
else
{
    <h1>False</h1>
}
</div>";
        var element = MarkupParser.Parse(markup, _model);

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
@if (identifierbool3)
{
    <h1>True</h1>
}
else
{
    <h1>False</h1>
}
</div>";
        var element = MarkupParser.Parse(markup, _model);

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
@if (identifierbool3)
{
    <h1>False</h1>
}
else if (identifierbool3)
{
    <h1>False</h1>
}
else
{
    <h1>True</h1>
}
</div>";
        var element = MarkupParser.Parse(markup, _model);

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
@if (identifierbool3)
{
    <h1>False</h1>
}
else if (identifierbool3)
{
    <h1>False</h1>
}
else if (identifierbool3)
{
    <h1>False</h1>
}
else
{
    <h1>FinalElse</h1>
}
</div>";
        var element = MarkupParser.Parse(markup, _model);

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
@if (identifierbool3)
{
    <h1>False</h1>
}
else if (identifierbool3)
{
    <h1>False</h1>
}
else if (identifierbool2)
{
    <h1>True</h1>
}
else
{
    <h1>False</h1>
}
</div>";
        var element = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        Assert.IsType<H1>(div.Children[0]);
        var t = (Text)div.Children[0].Children[0];
        Assert.Equal("True", t.Value);
    }

    [Fact]
    public void Parse_ForeachNegativeRange()
    {
        var markup = @"<stack>
@foreach (var    i in -100..200)
{
    <label>@i</label>
}
</stack>";
        var element = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(300, stack.Children.Count);
        
        var firstLabel = (Label)stack.Children[0];
        Assert.Equal("-100", ((Text)firstLabel.Children[0]).Value);

        var lastLabel = (Label)stack.Children[299];
        Assert.Equal("199", ((Text)lastLabel.Children[0]).Value);
    }

    [Fact]
    public void Parse_ControlFlowInterpolation_RespectsDoubleAt()
    {
        var markup = @"<div>
@foreach (var i in 1..3)
{
    <label>@@i @i</label>
}
</div>";
        var element = MarkupParser.Parse(markup, _model);

        var div = Assert.IsType<Div>(element);
        Assert.NotEmpty(div.Children);

        var firstLabel = Assert.IsType<Label>(div.Children[0]);
        var text = Assert.IsType<Text>(firstLabel.Children[0]);
        Assert.Equal("@@i 1", text.Value);
    }

    [Fact]
    public void Parse_SwitchDirective()
    {
        var markup = @"<div>
@switch (identifierany)
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
        var element = MarkupParser.Parse(markup, _model);

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
        var markup = @"@switch (identifierany)
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

    // ============== CONTROL FLOW - SWITCH WITH STRING ==============

    [Fact]
    public void Parse_SwitchDirective_WithStringCase()
    {
        var markup = @"<div>
@switch (stringValue)
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
        var element = MarkupParser.Parse(markup, _model);

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
@switch (identifierany)
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
        var element = MarkupParser.Parse(markup, _model);

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
        var markup = @"@switch (identifierany)
{
    case @identifier2
    {
        <h1>Variable Match</h1>
    }
    default
    {
        <h1>No Match</h1>
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
@foreach (var item in Collection)
{
    <h1>@item</h1>
}
</stack>";
        var element = MarkupParser.Parse(markup, _model);

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
@foreach (var item in items)
{
    <h1>@item.Name</h1>
}
</stack>";
        var element = MarkupParser.Parse(markup, _model);

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
@foreach (var  i in 0..3)
{
    <h1>@i</h1>
}
</stack>";
        var element = MarkupParser.Parse(markup, _model);

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
@if (identifierbool)
{
    <h1>Click Me</h1>
}
else
{
    <h1>Disabled</h1>
}
</button>";
        var element = MarkupParser.Parse(markup, _model);

        Assert.IsType<Button>(element);
        var button = (Button)element;
        Assert.Single(button.Children);
        var label = (Text)button.Children[0].Children[0];
        Assert.Equal("Click Me", label.Value);
    }

    [Fact]
    public void Parse_IfExpression()
    {
        var markup = @"<div>
@if (numericValue > 40 && stringValue == ""test"")
{
    <label>Complex Expression True</label>
}
</div>";
        var element = MarkupParser.Parse(markup, _model);

        Assert.IsType<Div>(element);
        var div = (Div)element;
        Assert.Single(div.Children);
        var label = (Label)div.Children[0];
        var text = (Text)label.Children[0];
        Assert.Equal("Complex Expression True", text.Value);
    }

    [Fact]
    public void Parse_NestedForeach_ScopedVariables()
    {
        var model = new {
            Categories = new[] {
                new { Name = "Fruit", Items = new[] { "Apple", "Banana" } },
                new { Name = "Veg", Items = new[] { "Carrot" } }
            }
        };

        var markup = @"<stack>
@foreach (       var  cat in Categories)
{
    <label>@cat.Name</label>
    @foreach ( var   item   in         cat.Items    )
    {
        <label>@item</label>
    }
}
</stack>";
        var element = MarkupParser.Parse(markup, model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        // Fruit, Apple, Banana, Veg, Carrot = 5 labels
        Assert.Equal(5, stack.Children.Count);
        Assert.Equal("Fruit", ((Text)((Label)stack.Children[0]).Children[0]).Value);
        Assert.Equal("Apple", ((Text)((Label)stack.Children[1]).Children[0]).Value);
        Assert.Equal("Banana", ((Text)((Label)stack.Children[2]).Children[0]).Value);
        Assert.Equal("Veg", ((Text)((Label)stack.Children[3]).Children[0]).Value);
        Assert.Equal("Carrot", ((Text)((Label)stack.Children[4]).Children[0]).Value);
    }

    [Fact]
    public void Parse_ForEach_WithVar()
    {
        var markup = @"<stack>
@foreach (var   item in Collection)
{
    <h1>@item</h1>
}
</stack>";
        var element = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(2, stack.Children.Count);
        var labels = stack.Children.Cast<H1>().Select(x => x.Children[0]).Cast<Text>().ToList();
        Assert.Equal("item1", labels.ElementAt(0).Value);
        Assert.Equal("item2", labels.ElementAt(1).Value);
    }

    [Fact]
    public void Parse_For_Simple()
    {
        var markup = @"<stack>
@for (var i=0; i < 5; i++)
{
    <label>@i</label>
}
</stack>";
        var element = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(5, stack.Children.Count);
        for (int i = 0; i < 5; i++)
        {
            var label = (Label)stack.Children[i];
            Assert.Equal(i.ToString(), ((Text)label.Children[0]).Value);
        }
    }

    [Fact]
    public void Parse_For_CustomStep()
    {
        var markup = @"<stack>
@for (var i=0; i < 10; i = i + 2)
{
    <label>@i</label>
}
</stack>";
        var element = MarkupParser.Parse(markup, _model);

        Assert.IsType<Stack>(element);
        var stack = (Stack)element;
        Assert.Equal(5, stack.Children.Count); // 0, 2, 4, 6, 8
        Assert.Equal("0", ((Text)((Label)stack.Children[0]).Children[0]).Value);
        Assert.Equal("8", ((Text)((Label)stack.Children[4]).Children[0]).Value);
    }

    private static dynamic Create(object model)
    {
        var observable = new Model.ObservableObject();
        observable.Initialize(model);
        return observable;
    }
}

