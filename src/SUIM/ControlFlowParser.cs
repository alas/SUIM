namespace SUIM;

using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

public class ControlFlowParser(dynamic model)
{
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
                if (IsDirective(markup, i, "if", out _))
                {
                    var (result, eaten) = ProcessIf(markup, i);
                    sb.Append(ExpandDirectives(result)); // Recurse for nested blocks
                    i += eaten;
                    continue;
                }
                else if (IsDirective(markup, i, "switch", out _))
                {
                    var (result, eaten) = ProcessSwitch(markup, i);
                    sb.Append(ExpandDirectives(result));
                    i += eaten;
                    continue;
                }
                else if (IsDirective(markup, i, "for", out _))
                {
                    var (result, eaten) = ProcessFor(markup, i);
                    sb.Append(ExpandDirectives(result));
                    i += eaten;
                    continue;
                }
                else if (IsDirective(markup, i, "foreach", out _))
                {
                    var (result, eaten) = ProcessForeach(markup, i);
                    sb.Append(ExpandDirectives(result));
                    i += eaten;
                    continue;
                }
            }

            sb.Append(markup[i]);
            i++;
        }

        return sb.ToString();
    }

    private static bool IsDirective(string markup, int index, string directive, out int length)
    {
        length = 0;
        if (index + 1 + directive.Length > markup.Length) return false;
        
        // precise match "@directive"
        if (markup.Substring(index + 1, directive.Length) == directive)
        {
            // check boundary: next char should be space or { or something valid
            char next = index + 1 + directive.Length < markup.Length ? markup[index + 1 + directive.Length] : '\0';
            if (char.IsWhiteSpace(next) || next == '{')
            {
                length = 1 + directive.Length; // @ + directive
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
        _ = (currentIndex - startIndex) + blockLen;

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
                        hasExecuted = true;
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
                var caseValueStr = ExtractCondition(blockBody, ref valStart); // re-use extract condition to get value until {
                 // Remove any trailing whitespace/brace handling from ExtractCondition if it over-consumed? 
                 // Actually ExtractCondition consumes until '{', which is perfect.
                 // But ExtractCondition returns trimmed string.
                 
                // Validate value
                var caseValue = ParseValue(caseValueStr);

                var (caseContent, caseLen) = ExtractBlock(blockBody, valStart);
                
                if (!matched && object.Equals(switchVal, caseValue))
                {
                    result = caseContent;
                    matched = true;
                }
                
                i = valStart + caseLen;
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

    private (string result, int eaten) ProcessFor(string markup, int startIndex)
    {
        int currentIndex = startIndex + 4; // "@for"
        
        // Parse parameters up to '{'
        // Format: i=0 count=100 step=-1
        int blockStart = markup.IndexOf('{', currentIndex);
        if (blockStart == -1) return (markup, 0); // Invalid

        string paramStr = markup.Substring(currentIndex, blockStart - currentIndex);
        var parameters = ParseParams(paramStr);

        var (blockContent, blockLen) = ExtractBlock(markup, blockStart);

        string varName = parameters.Keys.FirstOrDefault(k => k != "count" && k != "step") ?? "i";
        int startVal = int.Parse(parameters.ContainsKey(varName) ? parameters[varName] : "0");
        int count = int.Parse(parameters.TryGetValue("count", out var c) ? c : "0");
        int step = int.Parse(parameters.TryGetValue("step", out var s) ? s : "1");

        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            int val = startVal + (i * step);
            sb.Append(blockContent.Replace($"@{varName}", val.ToString()));
        }

        return (sb.ToString(), (blockStart - startIndex) + blockLen);
    }

    private (string result, int eaten) ProcessForeach(string markup, int startIndex)
    {
        int currentIndex = startIndex + 8; // "@foreach"
        
        // Parse: var in collection
        int blockStart = markup.IndexOf('{', currentIndex);
        string header = markup.Substring(currentIndex, blockStart - currentIndex).Trim();
        var parts = Regex.Split(header, @"\s+in\s+");
        var varName = parts[0];
        var collectionName = parts[1];

        var (blockContent, blockLen) = ExtractBlock(markup, blockStart);

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
             sb.Append(ReplaceVariables(blockContent, varName, item));
        }

        return (sb.ToString(), (blockStart - startIndex) + blockLen);
    }
    
    // --- Helpers ---

    private string ExtractCondition(string markup, ref int index)
    {
        index = SkipWhitespace(markup, index);
        int start = index;
        while (index < markup.Length && markup[index] != '{')
        {
            index++;
        }
        return markup.Substring(start, index - start).Trim();
    }

    private (string content, int totalLen) ExtractBlock(string markup, int startIndex)
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
            return (markup.Substring(contentStart, i - 1 - contentStart), i - startIndex);
        }
        
        return (string.Empty, 0); // Unbalanced or EOF
    }

    private int SkipWhitespace(string s, int idx)
    {
        while (idx < s.Length && char.IsWhiteSpace(s[idx])) idx++;
        return idx;
    }

    private bool IsKeyword(string s, int idx, string keyword)
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
        var matches = Regex.Matches(s, @"(\w+)=(-?\d+)");
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
        
        // Remove parens if present
        if (condition.StartsWith('(') && condition.EndsWith(')'))
             condition = condition.Substring(1, condition.Length - 2);

        return model.GetValue(condition) is bool b && b;
    }

    private object? GetValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (key.StartsWith('@')) key = key.Substring(1);
        
        return model.GetValue(key);
    }

    private object? ParseValue(string val)
    {
        if (val.StartsWith('@')) return GetValue(val);
        if (int.TryParse(val, out int i)) return i;
        if (val.StartsWith('"') && val.EndsWith('"')) return val.Trim('"');
        return val; // fallback string
    }

    private static string ReplaceVariables(string template, string varName, object item)
    {
        // Simple regex replace for @var.Prop or @var
        // Same logic as before is okay for basic variable replacement
        // But need to be careful not to replace inside other directives if nested? 
        // The Recursion happens *after* this anyway.
        
        return Regex.Replace(template, $@"@{Regex.Escape(varName)}(\.(\w+))?", match =>
        {
            if (match.Groups[2].Success)
            {
                var propName = match.Groups[2].Value;
                var prop = item.GetType().GetProperty(propName);
                if (prop != null)
                {
                    return prop.GetValue(item)?.ToString() ?? string.Empty;
                }
            }
            return item.ToString() ?? string.Empty;
        });
    }
}