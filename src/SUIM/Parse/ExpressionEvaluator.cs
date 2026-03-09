namespace SUIM.Parse;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class ExpressionEvaluator(IDictionary<string, object?> scope)
{
    public object? Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;

        var tokens = Tokenize(expression);
        var rpn = ShuntingYard(tokens);
        return EvaluateRPN(rpn);
    }

    private static List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        int i = 0;
        int length = expression.Length;

        while (i < length)
        {
            char c = expression[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsDigit(c) || (c == '.' && i + 1 < length && char.IsDigit(expression[i + 1])))
            {
                var sb = new StringBuilder();
                while (i < length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                {
                    sb.Append(expression[i]);
                    i++;
                }
                var valStr = sb.ToString();
                if (valStr.Contains('.'))
                    tokens.Add(new Token(TokenType.Literal, float.Parse(valStr, CultureInfo.InvariantCulture)));
                else
                    tokens.Add(new Token(TokenType.Literal, int.Parse(valStr)));
                continue;
            }

            if (c == '"' || c == '\'')
            {
                char quote = c;
                var sb = new StringBuilder();
                i++;
                while (i < length && expression[i] != quote)
                {
                    sb.Append(expression[i]);
                    i++;
                }
                i++;
                tokens.Add(new Token(TokenType.Literal, sb.ToString()));
                continue;
            }

            if (char.IsLetter(c) || c == '_' || c == '@')
            {
                var sb = new StringBuilder();
                while (i < length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_' || expression[i] == '@' || expression[i] == '.'))
                {
                    sb.Append(expression[i]);
                    i++;
                }
                var ident = sb.ToString();
                if (ident == "true") tokens.Add(new Token(TokenType.Literal, true));
                else if (ident == "false") tokens.Add(new Token(TokenType.Literal, false));
                else if (ident == "null") tokens.Add(new Token(TokenType.Literal, null));
                else tokens.Add(new Token(TokenType.Identifier, ident));
                continue;
            }

            // Operators
            if (Match(expression, ref i, "==")) tokens.Add(new Token(TokenType.Operator, "=="));
            else if (Match(expression, ref i, "!=")) tokens.Add(new Token(TokenType.Operator, "!="));
            else if (Match(expression, ref i, "<=")) tokens.Add(new Token(TokenType.Operator, "<="));
            else if (Match(expression, ref i, ">=")) tokens.Add(new Token(TokenType.Operator, ">="));
            else if (Match(expression, ref i, "&&")) tokens.Add(new Token(TokenType.Operator, "&&"));
            else if (Match(expression, ref i, "||")) tokens.Add(new Token(TokenType.Operator, "||"));
            else if (Match(expression, ref i, "<")) tokens.Add(new Token(TokenType.Operator, "<"));
            else if (Match(expression, ref i, ">")) tokens.Add(new Token(TokenType.Operator, ">"));
            else if (Match(expression, ref i, "!")) tokens.Add(new Token(TokenType.Operator, "!"));
            else if (Match(expression, ref i, "+")) tokens.Add(new Token(TokenType.Operator, "+"));
            else if (Match(expression, ref i, "-")) tokens.Add(new Token(TokenType.Operator, "-"));
            else if (Match(expression, ref i, "*")) tokens.Add(new Token(TokenType.Operator, "*"));
            else if (Match(expression, ref i, "/")) tokens.Add(new Token(TokenType.Operator, "/"));
            else if (Match(expression, ref i, "(")) tokens.Add(new Token(TokenType.OpenParen, "("));
            else if (Match(expression, ref i, ")")) tokens.Add(new Token(TokenType.CloseParen, ")"));
            else
            {
                throw new Exception($"Unexpected character: {c}");
            }
        }

        return tokens;
    }

    private static bool Match(string s, ref int i, string pattern)
    {
        if (i + pattern.Length <= s.Length && s.Substring(i, pattern.Length) == pattern)
        {
            i += pattern.Length;
            return true;
        }
        return false;
    }

    private static List<Token> ShuntingYard(List<Token> tokens)
    {
        var output = new List<Token>();
        var operators = new Stack<Token>();

        var precedence = new Dictionary<string, int>
        {
            ["||"] = 1,
            ["&&"] = 2,
            ["=="] = 3, ["!="] = 3,
            ["<"] = 4, [">"] = 4, ["<="] = 4, [">="] = 4,
            ["+"] = 5, ["-"] = 5,
            ["*"] = 6, ["/"] = 6,
            ["!"] = 7 // Unary negation (simplified handling)
        };

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.Literal || token.Type == TokenType.Identifier)
            {
                output.Add(token);
            }
            else if (token.Type == TokenType.OpenParen)
            {
                operators.Push(token);
            }
            else if (token.Type == TokenType.CloseParen)
            {
                while (operators.Count > 0 && operators.Peek().Type != TokenType.OpenParen)
                {
                    output.Add(operators.Pop());
                }
                if (operators.Count > 0) operators.Pop(); // Pop open paren
            }
            else if (token.Type == TokenType.Operator)
            {
                while (operators.Count > 0 && operators.Peek().Type == TokenType.Operator &&
                       precedence.GetValueOrDefault((string)operators.Peek().Value!) >= precedence.GetValueOrDefault((string)token.Value!))
                {
                    output.Add(operators.Pop());
                }
                operators.Push(token);
            }
        }

        while (operators.Count > 0)
        {
            output.Add(operators.Pop());
        }

        return output;
    }

    private object? EvaluateRPN(List<Token> rpn)
    {
        if (rpn.Count == 0) return null;
        var stack = new Stack<object?>();

        foreach (var token in rpn)
        {
            if (token.Type == TokenType.Literal)
            {
                stack.Push(token.Value);
            }
            else if (token.Type == TokenType.Identifier)
            {
                stack.Push(ResolveIdentifier((string)token.Value!));
            }
            else if (token.Type == TokenType.Operator)
            {
                var op = (string)token.Value!;
                if (op == "!")
                {
                    if (stack.Count < 1) return null;
                    var val = stack.Pop();
                    stack.Push(!(val is bool b && b));
                    continue;
                }

                if (stack.Count < 2) return null; // Or throw
                var right = stack.Pop();
                var left = stack.Pop();

                stack.Push(ExecuteOperator(left, op, right));
            }
            else
            {
                // Unknown token type in RPN (shouldn't happen with current ShuntingYard)
                return null;
            }
        }

        if (stack.Count != 1) return null; // Strict result: exactly one value should remain
        return stack.Pop();
    }

    private object? ResolveIdentifier(string identifier)
    {
        if (identifier.StartsWith('@')) identifier = identifier[1..];
        
        // Handle dot-notation for basic property access
        var parts = identifier.Split('.');

        if (scope.TryGetValue(parts[0], out object? current))
        {
            for (int i = 1; i < parts.Length; i++)
            {
                if (current == null) return null;
                var prop = current.GetType().GetProperty(parts[i]);
                if (prop != null)
                {
                    current = prop.GetValue(current);
                }
                else
                {
                    // Fallback for DynamicObject/ObservableObject if needed?
                    // For now, assume standard reflection or model lookup.
                    return null;
                }
            }
            return current;
        }

        return null;
    }

    private static object? ExecuteOperator(object? left, string op, object? right)
    {
        return op switch
        {
            "+" => Convert.ToDouble(left) + Convert.ToDouble(right),
            "-" => Convert.ToDouble(left) - Convert.ToDouble(right),
            "*" => Convert.ToDouble(left) * Convert.ToDouble(right),
            "/" => Convert.ToDouble(left) / Convert.ToDouble(right),
            "==" => Equals(left, right),
            "!=" => !Equals(left, right),
            "<" => Convert.ToDouble(left) < Convert.ToDouble(right),
            ">" => Convert.ToDouble(left) > Convert.ToDouble(right),
            "<=" => Convert.ToDouble(left) <= Convert.ToDouble(right),
            ">=" => Convert.ToDouble(left) >= Convert.ToDouble(right),
            "&&" => (left is bool lb && lb) && (right is bool rb && rb),
            "||" => (left is bool lo && lo) || (right is bool ro && ro),
            _ => throw new Exception($"Unknown operator: {op}"),
        };
    }

    private enum TokenType { Literal, Identifier, Operator, OpenParen, CloseParen }

    private record Token(TokenType Type, object? Value);
}
