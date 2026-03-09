namespace SUIM.Tests.Parsing;

using Xunit;
using SUIM.Parse;
using System.Collections.Generic;

public class ExpressionEvaluatorTests
{
    [Theory]
    [InlineData("1 + 2", 3.0)]
    [InlineData("10 - 4", 6.0)]
    [InlineData("3 * 4", 12.0)]
    [InlineData("12 / 3", 4.0)]
    [InlineData("(1 + 2) * 3", 9.0)]
    [InlineData("1 + 2 * 3", 7.0)]
    [InlineData("10.5 + 0.5", 11.0)]
    public void Evaluate_Arithmetic(string expression, object expected)
    {
        var evaluator = new ExpressionEvaluator(new Dictionary<string, object?>());
        var result = evaluator.Evaluate(expression);
        Assert.Equal(Convert.ToDouble(expected), Convert.ToDouble(result));
    }

    [Theory]
    [InlineData("1 == 1", true)]
    [InlineData("1 == 2", false)]
    [InlineData("1 != 2", true)]
    [InlineData("5 > 3", true)]
    [InlineData("5 < 3", false)]
    [InlineData("5 >= 5", true)]
    [InlineData("5 <= 4", false)]
    [InlineData("\"test\" == \"test\"", true)]
    [InlineData("\"a\" == \"b\"", false)]
    public void Evaluate_Comparison(string expression, bool expected)
    {
        var evaluator = new ExpressionEvaluator(new Dictionary<string, object?>());
        var result = evaluator.Evaluate(expression);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("true && true", true)]
    [InlineData("true && false", false)]
    [InlineData("true || false", true)]
    [InlineData("false || false", false)]
    [InlineData("!true", false)]
    [InlineData("!false", true)]
    [InlineData("!(1 == 2)", true)]
    public void Evaluate_Logical(string expression, bool expected)
    {
        var evaluator = new ExpressionEvaluator(new Dictionary<string, object?>());
        var result = evaluator.Evaluate(expression);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_Variables()
    {
        var scope = new Dictionary<string, object?>
        {
            ["count"] = 10,
            ["name"] = "Alice",
            ["user"] = new { Role = "Admin" }
        };
        var evaluator = new ExpressionEvaluator(scope);

        Assert.Equal(10, evaluator.Evaluate("count"));
        Assert.Equal(true, evaluator.Evaluate("count > 5"));
        Assert.Equal("Alice", evaluator.Evaluate("name"));
        Assert.Equal("Admin", evaluator.Evaluate("user.Role"));
    }

    [Fact]
    public void Evaluate_Security_NoArbitraryCode()
    {
        var evaluator = new ExpressionEvaluator(new Dictionary<string, object?>());
        
        // This should return null or fail gracefully, not execute code
        // Our evaluator doesn't support method calls, so this will just be an unknown identifier or fail parsing
        var result = evaluator.Evaluate("System.Console.WriteLine(\"hack\")");
        Assert.Null(result);
    }
}
