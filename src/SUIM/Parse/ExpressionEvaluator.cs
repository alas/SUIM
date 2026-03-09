namespace SUIM.Parse;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class ExpressionEvaluator(IEnumerable<IDictionary<string, object?>> scopes)
{
    private readonly IEnumerable<IDictionary<string, object?>> _scopes = scopes;

    public object? Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;

        // Support multiple expressions separated by ;
        var exprs = expression.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        object? lastResult = null;
        foreach (var expr in exprs)
        {
            var tokens = Tokenize(expr);
            var rpn = ShuntingYard(tokens);
            lastResult = EvaluateRPN(rpn);
        }
        return lastResult;
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

            // Check multi-character operators first to avoid conflicts (like 0..5 vs 0.5)
            if (Match(expression, ref i, "==")) { tokens.Add(new Token(TokenType.Operator, "==")); continue; }
            if (Match(expression, ref i, "!=")) { tokens.Add(new Token(TokenType.Operator, "!=")); continue; }
            if (Match(expression, ref i, "<=")) { tokens.Add(new Token(TokenType.Operator, "<=")); continue; }
            if (Match(expression, ref i, ">=")) { tokens.Add(new Token(TokenType.Operator, ">=")); continue; }
            if (Match(expression, ref i, "&&")) { tokens.Add(new Token(TokenType.Operator, "&&")); continue; }
            if (Match(expression, ref i, "||")) { tokens.Add(new Token(TokenType.Operator, "||")); continue; }
            if (Match(expression, ref i, "++")) { tokens.Add(new Token(TokenType.Operator, "++")); continue; }
            if (Match(expression, ref i, "--")) { tokens.Add(new Token(TokenType.Operator, "--")); continue; }
            if (Match(expression, ref i, "+=")) { tokens.Add(new Token(TokenType.Operator, "+=")); continue; }
            if (Match(expression, ref i, "-=")) { tokens.Add(new Token(TokenType.Operator, "-=")); continue; }
            if (Match(expression, ref i, "*=")) { tokens.Add(new Token(TokenType.Operator, "*=")); continue; }
            if (Match(expression, ref i, "/=")) { tokens.Add(new Token(TokenType.Operator, "/=")); continue; }
            if (Match(expression, ref i, "..")) { tokens.Add(new Token(TokenType.Operator, "..")); continue; }

            bool isUnary = tokens.Count == 0 || tokens[^1].Type == TokenType.Operator || tokens[^1].Type == TokenType.OpenParen;
            if (char.IsDigit(c) || (c == '-' && isUnary && i + 1 < length && char.IsDigit(expression[i + 1])) || (c == '.' && i + 1 < length && char.IsDigit(expression[i + 1])))
            {
                var sb = new StringBuilder();
                if (c == '-') { sb.Append(c); i++; }
                
                bool hasDot = false;
                while (i < length)
                {
                    char cur = expression[i];
                    if (char.IsDigit(cur)) sb.Append(cur);
                    else if (cur == '.' && !hasDot)
                    {
                        // Only consume dot if not followed by another dot (which would be a range)
                        if (i + 1 < length && expression[i + 1] == '.') break; 
                        sb.Append(cur);
                        hasDot = true;
                    }
                    else break;
                    i++;
                }
                var valStr = sb.ToString();
                if (hasDot)
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
                else if (ident == "var") { /* ignore var keyword */ }
                else tokens.Add(new Token(TokenType.Identifier, ident));
                continue;
            }

            // Single-character operators
            if (Match(expression, ref i, "<")) tokens.Add(new Token(TokenType.Operator, "<"));
            else if (Match(expression, ref i, ">")) tokens.Add(new Token(TokenType.Operator, ">"));
            else if (Match(expression, ref i, "!")) tokens.Add(new Token(TokenType.Operator, "!"));
            else if (Match(expression, ref i, "=")) tokens.Add(new Token(TokenType.Operator, "="));
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
            ["="] = 0, ["+="] = 0, ["-="] = 0, ["*="] = 0, ["/="] = 0,
            ["||"] = 1,
            ["&&"] = 2,
            ["=="] = 3, ["!="] = 3,
            [".."] = 4,
            ["<"] = 5, [">"] = 5, ["<="] = 5, [">="] = 5,
            ["+"] = 6, ["-"] = 6,
            ["*"] = 7, ["/"] = 7,
            ["!"] = 8, ["++"] = 8, ["--"] = 8
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
                var op = (string)token.Value!;
                while (operators.Count > 0 && operators.Peek().Type == TokenType.Operator &&
                       precedence.GetValueOrDefault((string)operators.Peek().Value!) >= precedence.GetValueOrDefault(op))
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
                // We push a VariableRef if we expect an assignment later
                stack.Push(new VariableRef((string)token.Value!));
            }
            else if (token.Type == TokenType.Operator)
            {
                var op = (string)token.Value!;
                
                // Unary operators
                if (op == "!" || op == "++" || op == "--")
                {
                    if (stack.Count < 1) return null;
                    var operand = stack.Pop();
                    
                    if (op == "!")
                    {
                        var val = ResolveValue(operand);
                        stack.Push(!(val is bool b && b));
                    }
                    else if (op == "++")
                    {
                        if (operand is VariableRef vref)
                        {
                            var oldVal = ResolveValue(vref);
                            var newVal = Convert.ToDouble(oldVal) + 1;
                            WriteValue(vref, newVal);
                            stack.Push(oldVal); // Postfix behavior: return old value
                        }
                    }
                    else if (op == "--")
                    {
                        if (operand is VariableRef vref)
                        {
                            var oldVal = ResolveValue(vref);
                            var newVal = Convert.ToDouble(oldVal) - 1;
                            WriteValue(vref, newVal);
                            stack.Push(oldVal);
                        }
                    }
                    continue;
                }

                if (stack.Count < 2) return null;
                var right = stack.Pop();
                var left = stack.Pop();

                if (op == "=" || op == "+=" || op == "-=" || op == "*=" || op == "/=")
                {
                    if (left is VariableRef vref)
                    {
                        var rightVal = ResolveValue(right);
                        if (op == "=")
                        {
                            WriteValue(vref, rightVal);
                            stack.Push(rightVal);
                        }
                        else
                        {
                            var leftVal = ResolveValue(vref);
                            var result = ExecuteOperator(leftVal, op.Substring(0, 1), rightVal);
                            WriteValue(vref, result);
                            stack.Push(result);
                        }
                        continue;
                    }
                    throw new Exception("Left side of assignment must be a variable.");
                }

                stack.Push(ExecuteOperator(ResolveValue(left), op, ResolveValue(right)));
            }
        }

        if (stack.Count != 1) return null;
        return ResolveValue(stack.Pop());
    }

    private object? ResolveValue(object? obj)
    {
        if (obj is VariableRef vref) return ResolveIdentifier(vref.Name);
        return obj;
    }

    private void WriteValue(VariableRef vref, object? value)
    {
        var identifier = vref.Name;
        if (identifier.StartsWith('@')) identifier = identifier[1..];

        var parts = identifier.Split('.');
        if (parts.Length == 1)
        {
             // Update the first scope that contains the key, or the top scope if it's new
             foreach (var scope in _scopes)
             {
                 if (scope.ContainsKey(parts[0]))
                 {
                     scope[parts[0]] = value;
                     return;
                 }
             }
             _scopes.First()[parts[0]] = value;
             return;
        }

        // Dot notation
        var current = ResolveIdentifier(parts[0]);
        if (current == null) 
        {
             // If the base doesn't exist, we can't write down the dot path
             return;
        }

        for (int i = 1; i < parts.Length - 1; i++)
        {
            var prop = current.GetType().GetProperty(parts[i]);
            if (prop != null) current = prop.GetValue(current);
            else return;
            if (current == null) return;
        }

        var finalProp = current.GetType().GetProperty(parts[^1]);
        if (finalProp != null && finalProp.CanWrite)
        {
            finalProp.SetValue(current, value);
        }
    }

    private object? ResolveIdentifier(string identifier)
    {
        if (identifier.StartsWith('@')) identifier = identifier[1..];
        
        var parts = identifier.Split('.');

        // Search through scopes from top to bottom
        object? current = null;
        bool found = false;

        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(parts[0], out current))
            {
                found = true;
                break;
            }
        }

        if (!found) return null;

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
                return null;
            }
        }
        return current;
    }

    private static object? ExecuteOperator(object? left, string op, object? right)
    {
        return op switch
        {
            "+" => Convert.ToDouble(left, CultureInfo.InvariantCulture) + Convert.ToDouble(right, CultureInfo.InvariantCulture),
            "-" => Convert.ToDouble(left, CultureInfo.InvariantCulture) - Convert.ToDouble(right, CultureInfo.InvariantCulture),
            "*" => Convert.ToDouble(left, CultureInfo.InvariantCulture) * Convert.ToDouble(right, CultureInfo.InvariantCulture),
            "/" => Convert.ToDouble(left, CultureInfo.InvariantCulture) / Convert.ToDouble(right, CultureInfo.InvariantCulture),
            ".." => Enumerable.Range(Convert.ToInt32(left), Convert.ToInt32(right) - Convert.ToInt32(left)).Cast<object>(),
            "==" => Equals(left, right),
            "!=" => !Equals(left, right),
            "<" => Convert.ToDouble(left, CultureInfo.InvariantCulture) < Convert.ToDouble(right, CultureInfo.InvariantCulture),
            ">" => Convert.ToDouble(left, CultureInfo.InvariantCulture) > Convert.ToDouble(right, CultureInfo.InvariantCulture),
            "<=" => Convert.ToDouble(left, CultureInfo.InvariantCulture) <= Convert.ToDouble(right, CultureInfo.InvariantCulture),
            ">=" => Convert.ToDouble(left, CultureInfo.InvariantCulture) >= Convert.ToDouble(right, CultureInfo.InvariantCulture),
            "&&" => (left is bool lb && lb) && (right is bool rb && rb),
            "||" => (left is bool lo && lo) || (right is bool ro && ro),
            _ => throw new Exception($"Unknown operator: {op}"),
        };
    }

    private enum TokenType { Literal, Identifier, Operator, OpenParen, CloseParen }

    private record Token(TokenType Type, object? Value);
    
    // Internal class to hold variable reference during RPN evaluation
    private record VariableRef(string Name);
}
