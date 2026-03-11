namespace SUIM.Binding;

using System.Text.RegularExpressions;

public static partial class BindingText
{
    public static string InterpolateLocalVariables(string template, Func<string, bool> isLocalVariable, Func<string, object?> evaluateExpression)
    {
        return BindingTextRegexes.LocalTokenRegex().Replace(template, match =>
        {
            var ident = match.Groups[1].Value;
            if (!isLocalVariable(ident))
            {
                return match.Value;
            }

            var expr = match.Value;
            if (expr.StartsWith('@')) expr = expr[1..];

            var result = evaluateExpression(expr);
            return result?.ToString() ?? string.Empty;
        });
    }

    public static List<string> SplitAtSingleAtTokens(ReadOnlySpan<char> input)
    {
        var result = new List<string>();

        int i = 0;
        int segmentStart = 0;

        while (i < input.Length)
        {
            // Escaped @@ -> normal text
            if (i + 1 < input.Length && input[i] == '@' && input[i + 1] == '@')
            {
                i += 2;
                continue;
            }

            // Single @ that forms a valid token (@ + non-whitespace)
            if (input[i] == '@' &&
                i + 1 < input.Length &&
                !char.IsWhiteSpace(input[i + 1]))
            {
                // Flush preceding text
                if (i > segmentStart)
                {
                    result.Add(input[segmentStart..i].ToString());
                }

                int tokenStart = i;
                i++; // skip '@'

                while (i < input.Length && !char.IsWhiteSpace(input[i]))
                {
                    // Stop if escaped @@ appears
                    if (i + 1 < input.Length && input[i] == '@' && input[i + 1] == '@')
                        break;

                    i++;
                }

                result.Add(input[tokenStart..i].ToString());

                segmentStart = i;
                continue;
            }

            i++;
        }

        // Flush remaining text
        if (segmentStart < input.Length)
        {
            result.Add(input[segmentStart..].ToString());
        }

        return result;
    }
}

internal static partial class BindingTextRegexes
{
    [GeneratedRegex(@"@(\w+)(\.(\w+))?")]
    internal static partial Regex LocalTokenRegex();
}
