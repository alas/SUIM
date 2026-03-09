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
        var evaluator = new ExpressionEvaluator([new Dictionary<string, object?>()]);
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
        var evaluator = new ExpressionEvaluator([new Dictionary<string, object?>()]);
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
        var evaluator = new ExpressionEvaluator([new Dictionary<string, object?>()]);
        var result = evaluator.Evaluate(expression);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_Variables()
    {
        var scope = new Dictionary<string, object?>
        {
            ["count"] = 10,
            ["name"] = "Alice"
        };
        var evaluator = new ExpressionEvaluator([scope]);

        Assert.Equal(10, evaluator.Evaluate("count"));
        Assert.Equal(true, evaluator.Evaluate("count > 5"));
        Assert.Equal("Alice", evaluator.Evaluate("name"));
    }

    [Fact]
    public void Evaluate_ScopeHierarchy()
    {
        var baseScope = new Dictionary<string, object?> { ["x"] = 1, ["y"] = 2 };
        var topScope = new Dictionary<string, object?> { ["x"] = 10 };
        var evaluator = new ExpressionEvaluator([topScope, baseScope]);

        Assert.Equal(10, evaluator.Evaluate("x")); // Should find in top scope
        Assert.Equal(2, evaluator.Evaluate("y"));  // Should find in base scope
    }

    [Fact]
    public void Evaluate_Assignments()
    {
        var scope = new Dictionary<string, object?> { ["i"] = 0 };
        var evaluator = new ExpressionEvaluator([scope]);

        evaluator.Evaluate("i = 10");
        Assert.Equal(10, scope["i"]);

        evaluator.Evaluate("i++");
        Assert.Equal(11.0, Convert.ToDouble(scope["i"]));

        evaluator.Evaluate("i += 5");
        Assert.Equal(16.0, Convert.ToDouble(scope["i"]));
    }

    [Fact]
    public void Evaluate_Security_NoArbitraryCode()
    {
        var evaluator = new ExpressionEvaluator([new Dictionary<string, object?>()]);
        
        // This should return null or fail gracefully, not execute code
        var result = evaluator.Evaluate("System.Console.WriteLine(\"hack\")");
        Assert.Null(result);
    }
}
