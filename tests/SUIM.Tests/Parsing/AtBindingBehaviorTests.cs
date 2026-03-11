namespace SUIM.Tests.Parsing;

using System.Linq;
using SUIM.Binding;
using SUIM.Parse;
using SUIM.Parse.Components;
using Xunit;

public class AtBindingBehaviorTests
{
    [Fact]
    public void BindingExpression_SingleAt_IsBinding_DoubleAt_IsNot()
    {
        Assert.True(BindingExpression.IsBindingValue("@name"));
        Assert.False(BindingExpression.IsBindingValue("@@name"));
        Assert.False(BindingExpression.IsBindingValue("@"));
        Assert.False(BindingExpression.IsBindingValue(null));
    }

    [Fact]
    public void MarkupParser_TextTokens_HandleAtAndDoubleAt()
    {
        var markup = "<div>Hello @@name and @name</div>";
        var element = MarkupParser.Parse(markup, new { name = "Bob" });

        var div = Assert.IsType<Div>(element);
        var textNodes = div.Children.OfType<Text>().ToList();

        Assert.True(textNodes[0].Value == "Hello @@name and " && textNodes[0].Bindings.Count == 0);
        Assert.Contains(textNodes[1].Bindings, b => b.TargetPropertyName == "value" && b.ModelPropertyName == "name");
    }

    [Fact]
    public void MarkupParser_AttributeBinding_SingleAt_Binds_DoubleAt_Literal()
    {
        var bound = (Input)MarkupParser.Parse("<input value=\"@name\" />", new { name = "Bob" });
        Assert.Contains(bound.Bindings, b => b.TargetPropertyName == "value" && b.ModelPropertyName == "name");
        Assert.Null(bound.Value);

        var literal = (Input)MarkupParser.Parse("<input value=\"@@name\" />", new { name = "Bob" });
        Assert.Empty(literal.Bindings);
        Assert.Equal("@@name", literal.Value);
    }
}
