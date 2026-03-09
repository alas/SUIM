namespace SUIM.Parse;

using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

public partial class ControlFlowParser(dynamic model)
{
    private readonly Stack<IDictionary<string, object?>> _scopes = CreateInitialScope(model);

    private static Stack<IDictionary<string, object?>> CreateInitialScope(dynamic model)
    {
        var stack = new Stack<IDictionary<string, object?>>();
        var dict = new Dictionary<string, object?>();
        
        if (model is Model.ObservableObject oo)
        {
            var propertiesField = typeof(Model.ObservableObject).GetField("_properties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (propertiesField?.GetValue(oo) is Dictionary<string, object?> props)
            {
                foreach (var kvp in props) dict[kvp.Key] = kvp.Value;
            }
        }
        else if (model != null)
        {
            foreach (var prop in model.GetType().GetProperties())
            {
                if (prop.CanRead) dict[prop.Name] = prop.GetValue(model);
            }
        }
        
        stack.Push(dict);
        return stack;
    }

    private Dictionary<string, object?> GetCurrentScope()
    {
        var merged = new Dictionary<string, object?>();
        foreach (var scope in _scopes.Reverse())
        {
            foreach (var kvp in scope) merged[kvp.Key] = kvp.Value;
        }
        return merged;
    }

    public string ExpandDirectives(string markup)
    {
        var sb = new StringBuilder();
        int i = 0;
        int length = markup.Length;

        while (i < length)
        {
            if (markup[i] == '@')
            {
                // Potential directive
                if (IsDirective(markup, i, "if"))
                {
                    var (result, eaten) = ProcessIf(markup, i);
                    sb.Append(ExpandDirectives(result)); // Recurse for nested blocks
                    i += eaten;
                    continue;
                }
                else if (IsDirective(markup, i, "switch"))
                {
                    var (result, eaten) = ProcessSwitch(markup, i);
                    sb.Append(ExpandDirectives(result));
                    i += eaten;
                    continue;
                }
                else if (IsDirective(markup, i, "foreach"))
                {
                    var (result, eaten) = ProcessForeach(markup, i);
                    sb.Append(ExpandDirectives(result));
                    i += eaten;
                    continue;
                }
                else if (IsDirective(markup, i, "for"))
                {
                    var (result, eaten) = ProcessFor(markup, i);
                    sb.Append(ExpandDirectives(result));
                    i += eaten;
                    continue;
                }
            }

            sb.Append(markup[i]);
            i++;
        }

        return InterpolateVariables(sb.ToString());
    }

    private string InterpolateVariables(string template)
    {
        // Replace @Variable or @Variable.Property with scoped values
        // ONLY if they are from a local scope (not the root model scope)
        return MyRegex2().Replace(template, match =>
        {
            var ident = match.Groups[1].Value;
            if (IsLocalVariable(ident))
            {
                var result = GetValue(match.Value);
                return result?.ToString() ?? string.Empty;
            }
            return match.Value;
        });
    }

    private bool IsLocalVariable(string name)
    {
        if (_scopes.Count <= 1) return false;
        
        // Check all scopes except the bottom one (the model)
        var list = _scopes.ToList(); // Top is 0, Bottom is Count-1
        for (int i = 0; i < list.Count - 1; i++)
        {
            if (list[i].ContainsKey(name)) return true;
        }
        return false;
    }

    private static bool IsDirective(string markup, int index, string directive)
    {
        if (index + 1 + directive.Length > markup.Length) return false;
        
        // precise match "@directive"
        if (markup.Substring(index + 1, directive.Length) == directive)
        {
            // check boundary: next char should be space or ( or { or something valid
            char next = index + 1 + directive.Length < markup.Length ? markup[index + 1 + directive.Length] : '\0';
            if (char.IsWhiteSpace(next) || next == '(' || next == '{')
            {
                return true;
            }
        }
        return false;
    }

    private (string result, int eaten) ProcessIf(string markup, int startIndex)
    {
        int currentIndex = startIndex + 3; // Skip "@if"
        
        // 1. Get condition
        var condition = ExtractCondition(markup, ref currentIndex);
        
        // 2. Get Block
        var (trueBlock, blockLen) = ExtractBlock(markup, currentIndex);

        bool conditionMet = EvaluateCondition(condition);
        string result = conditionMet ? trueBlock : string.Empty;
        bool hasExecuted = conditionMet;

        currentIndex += blockLen;

        // Check for else if / else
        while (true)
        {
            int nextTokenIndex = SkipWhitespace(markup, currentIndex);
            if (nextTokenIndex >= markup.Length) break;

            if (IsKeyword(markup, nextTokenIndex, "else"))
            {
                int keywordLen = 4;
                int tempIndex = nextTokenIndex + keywordLen;
                int afterElseIndex = SkipWhitespace(markup, tempIndex);
                
                if (IsKeyword(markup, afterElseIndex, "if"))
                {
                    // else if
                    int ifIndex = afterElseIndex + 2;
                    var elseIfCondition = ExtractCondition(markup, ref ifIndex);
                    var (elseIfBlock, elseIfBlockLen) = ExtractBlock(markup, ifIndex);
                    
                    if (!hasExecuted && EvaluateCondition(elseIfCondition))
                    {
                        result = elseIfBlock;
                        hasExecuted = true;
                    }
                    
                    currentIndex = ifIndex + elseIfBlockLen;
                }
                else
                {
                    // else (final)
                    var (elseBlock, elseBlockLen) = ExtractBlock(markup, afterElseIndex);
                    
                    if (!hasExecuted)
                    {
                        result = elseBlock;
                    }

                    currentIndex = afterElseIndex + elseBlockLen;
                    break; // 'else' is final
                }
            }
            else
            {
                break;
            }
        }

        return (result, currentIndex - startIndex);
    }

    private (string result, int eaten) ProcessSwitch(string markup, int startIndex)
    {
        int currentIndex = startIndex + 7; // "@switch"
        var switchVar = ExtractCondition(markup, ref currentIndex);
        var switchVal = GetValue(switchVar);

        var (blockBody, blockLen) = ExtractBlock(markup, currentIndex);
        
        // Parse cases inside blockBody
        string result = string.Empty;
        bool matched = false;

        // Simple sub-parser for cases inside the block
        int i = 0;
        int len = blockBody.Length;
        var defaultContent = string.Empty;

        while (i < len)
        {
            i = SkipWhitespace(blockBody, i);
            if (i >= len) break;

            if (IsKeyword(blockBody, i, "case"))
            {
                int valStart = i + 4;
                valStart = SkipWhitespace(blockBody, valStart);
                
                // Read until '{'
                int valEnd = valStart;
                while (valEnd < len && blockBody[valEnd] != '{')
                {
                    valEnd++;
                }
                var caseValueStr = blockBody[valStart..valEnd].Trim();
                
                // Validate value
                var caseValue = ParseValue(caseValueStr);

                var (caseContent, caseLen) = ExtractBlock(blockBody, valEnd);
                
                if (!matched && Equals(switchVal, caseValue))
                {
                    result = caseContent;
                    matched = true;
                }

                i = valEnd + caseLen;
            }
            else if (IsKeyword(blockBody, i, "default"))
            {
                int defStart = i + 7;
                var (defContent, defLen) = ExtractBlock(blockBody, defStart);
                defaultContent = defContent;
                i = defStart + defLen;
            }
            else
            {
                i++; // skip unknown or comments?
            }
        }

        if (!matched)
        {
            result = defaultContent;
        }

        return (result, (currentIndex - startIndex) + blockLen);
    }

    private (string result, int eaten) ProcessForeach(string markup, int startIndex)
    {
        int currentIndex = startIndex + 8; // "@foreach"
        
        // Parse: (var i in collection)
        var header = ExtractCondition(markup, ref currentIndex);
        
        // Handle 'var ' prefix
        if (header.StartsWith("var") && char.IsWhiteSpace(header[3])) header = header[4..].Trim();
        else throw new Exception("Invalid @foreach syntax. Expected (var item in collection)");

        var parts = MyRegex().Split(header);
        if (parts.Length < 2) throw new Exception("Invalid @foreach syntax. Expected (var item in collection)");
        
        var varName = parts[0].Trim();
        var collectionName = parts[1].Trim();

        var (blockContent, blockLen) = ExtractBlock(markup, currentIndex);

        IEnumerable items;
        if (collectionName.Contains(".."))
        {
             var rangeParts = collectionName.Split("..");
             int start = int.Parse(rangeParts[0]);
             int end = int.Parse(rangeParts[1]);
             items = Enumerable.Range(start, end - start).Cast<object>();
        }
        else
        {
            items = GetValue(collectionName) as IEnumerable ?? Array.Empty<object>();
        }

        var sb = new StringBuilder();
        foreach (var item in items)
        {
             // Push local scope
             _scopes.Push(new Dictionary<string, object?> { [varName] = item });
             sb.Append(ExpandDirectives(blockContent));
             _scopes.Pop();
        }

        return (sb.ToString(), currentIndex + blockLen - startIndex);
    }

    private (string result, int eaten) ProcessFor(string markup, int startIndex)
    {
        int currentIndex = startIndex + 4; // "@for"
        var header = ExtractCondition(markup, ref currentIndex);
        
        // (var i=0; i < 100; i++)
        var parts = header.Split(';');
        if (parts.Length != 3) throw new Exception("Invalid @for syntax. Expected (init; condition; step)");

        var initPart = parts[0].Trim();
        var conditionPart = parts[1].Trim();
        var stepPart = parts[2].Trim();

        // Init: var i=0
        if (initPart.StartsWith("var") && char.IsWhiteSpace(initPart[3])) initPart = initPart[4..];
        else throw new Exception("Invalid @for initialization. Expected 'var i = value'");
        var initMatch = MyRegex3().Match(initPart);
        if (!initMatch.Success) throw new Exception("Invalid @for initialization. Expected 'var i = value'");

        var varName = initMatch.Groups[1].Value;
        var startValue = ParseValue(initMatch.Groups[2].Value);

        var (blockContent, blockLen) = ExtractBlock(markup, currentIndex);
        var sb = new StringBuilder();

        _scopes.Push(new Dictionary<string, object?> { [varName] = startValue });
        try
        {
            while (EvaluateCondition(conditionPart))
            {
                sb.Append(ExpandDirectives(blockContent));

                // Step: i++, i--, i = i + 1
                var currentValue = _scopes.Peek()[varName];
                var nextValue = EvaluateStep(varName, stepPart, currentValue);
                _scopes.Peek()[varName] = nextValue;
            }
        }
        finally
        {
            _scopes.Pop();
        }

        return (sb.ToString(), currentIndex + blockLen - startIndex);
    }

    private object? EvaluateStep(string varName, string step, object? current)
    {
        if (step == $"{varName}++") return Convert.ToDouble(current) + 1;
        if (step == $"{varName}--") return Convert.ToDouble(current) - 1;
        
        // Handle i = i + 1 or similar
        var match = Regex.Match(step, $@"^{varName}\s*=\s*(.*)$");
        if (match.Success)
        {
            var expr = match.Groups[1].Value;
            var evaluator = new ExpressionEvaluator(GetCurrentScope());
            return evaluator.Evaluate(expr);
        }

        throw new Exception($"Unsupported @for step expression: {step}");
    }
    
    // --- Helpers ---

    private static string ExtractCondition(string markup, ref int index)
    {
        index = SkipWhitespace(markup, index);
        if (index >= markup.Length || markup[index] != '(')
        {
            throw new InvalidOperationException("Directives must use parentheses, e.g., @if (condition).");
        }

        int start = index + 1;
        int balance = 1;
        index++;
        while (index < markup.Length && balance > 0)
        {
            if (markup[index] == '(') balance++;
            else if (markup[index] == ')') balance--;
            index++;
        }

        if (balance > 0) throw new InvalidOperationException("Unbalanced parentheses in directive condition.");

        return markup[start..(index - 1)].Trim();
    }

    private static (string content, int totalLen) ExtractBlock(string markup, int startIndex)
    {
        // startIndex should be at '{' or whitespace before '{'
        int i = SkipWhitespace(markup, startIndex);
        if (i >= markup.Length || markup[i] != '{') return (string.Empty, 0);

        int contentStart = i + 1;
        int balance = 1;
        i++;
        
        while (i < markup.Length && balance > 0)
        {
            if (markup[i] == '{') balance++;
            else if (markup[i] == '}') balance--;
            i++;
        }

        if (balance == 0)
        {
            // Return content inside braces, and total length including braces
            return (markup[contentStart..(i - 1)], i - startIndex);
        }
        
        return (string.Empty, 0); // Unbalanced or EOF
    }

    private static int SkipWhitespace(string s, int idx)
    {
        while (idx < s.Length && char.IsWhiteSpace(s[idx])) idx++;
        return idx;
    }

    private static bool IsKeyword(string s, int idx, string keyword)
    {
        if (idx + keyword.Length > s.Length) return false;
        if (s.Substring(idx, keyword.Length) != keyword) return false;
        // ensure boundary
        if (idx + keyword.Length < s.Length)
        {
             char next = s[idx + keyword.Length];
             return char.IsWhiteSpace(next) || next == '{' || next == '(';
        }
        return true;
    }

    private static Dictionary<string, string> ParseParams(string s)
    {
        var dict = new Dictionary<string, string>();
        var matches = MyRegex1().Matches(s);
        foreach (Match m in matches)
        {
            dict[m.Groups[1].Value] = m.Groups[2].Value;
        }
        return dict;
    }
    
    // --- Evaluation & Binding copied/adapted ---

    private bool EvaluateCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return false;
        
        var evaluator = new ExpressionEvaluator(GetCurrentScope());
        var result = evaluator.Evaluate(condition);
        
        return result is bool b && b;
    }

    private object? GetValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (key.StartsWith('@')) key = key[1..];
        
        var evaluator = new ExpressionEvaluator(GetCurrentScope());
        return evaluator.Evaluate(key);
    }

    private object? ParseValue(string val)
    {
        if (val.StartsWith('@')) return GetValue(val);
        if (int.TryParse(val, out int i)) return i;
        if (val.StartsWith('"') && val.EndsWith('"')) return val.Trim('"');
        
        var evaluator = new ExpressionEvaluator(GetCurrentScope());
        return evaluator.Evaluate(val);
    }

    [GeneratedRegex(@"\s+in\s+")]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"(\w+)=(-?\d+)")]
    private static partial Regex MyRegex1();
    [GeneratedRegex(@"@(\w+)(\.(\w+))?")]
    private static partial Regex MyRegex2();
    [GeneratedRegex(@"^(\w+)\s*=\s*(.*)$")]
    private static partial Regex MyRegex3();
}
