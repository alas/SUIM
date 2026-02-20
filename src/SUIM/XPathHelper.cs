namespace SUIM;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class XPathHelper
{

    /// <summary>
    /// Find an element by a simple path expression.
    /// Supports either a simple name (recursive search) or a slash-separated path (direct traversal).
    /// Each path segment may include an optional 1-based index like "panel[2]".
    /// This helper is intentionally conservative and uses reflection to support multiple backends.
    /// </summary>
    public static object? FindElementByPath(object? root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path)) return null;

        // Normalize
        path = path.Trim();
        if (path.StartsWith('/')) path = path[1..];

        // If simple name (no slash) do a recursive search by Name
        if (!path.Contains('/'))
        {
            return RecursiveFindByName(root, path);
        }

        var segments = path.Split('/');
        object? current = root;
        foreach (var seg in segments)
        {
            if (current == null) return null;
            var (name, index) = ParseSegment(seg);
            var children = GetChildren(current).ToList();
            if (children.Count == 0) return null;

            var matches = new List<object>();
            foreach (var c in children)
            {
                try
                {
                    var nameProp = c.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    var cName = nameProp?.GetValue(c)?.ToString();
                    if (!string.IsNullOrEmpty(cName) && string.Equals(cName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(c);
                    }
                }
                catch { }
            }

            if (matches.Count == 0) return null;
            if (index.HasValue)
            {
                var idx = index.Value - 1;
                if (idx < 0 || idx >= matches.Count) return null;
                current = matches[idx];
            }
            else
            {
                current = matches[0];
            }
        }

        return current;
    }

    private static object? RecursiveFindByName(object root, string name)
    {
        // Check root itself
        try
        {
            var nameProp = root.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var rName = nameProp?.GetValue(root)?.ToString();
            if (!string.IsNullOrEmpty(rName) && string.Equals(rName, name, StringComparison.OrdinalIgnoreCase)) return root;
        }
        catch { }

        foreach (var child in GetChildren(root))
        {
            var found = RecursiveFindByName(child, name);
            if (found != null) return found;
        }

        return null;
    }

    private static IEnumerable<object> GetChildren(object obj)
    {
        if (obj == null) yield break;

        // If object itself is enumerable (and not a string), yield its items
        if (obj is IEnumerable ie && obj is not string)
        {
            foreach (var it in ie)
            {
                yield return it!;
            }
            yield break;
        }

        object? childrenVal = null;
        bool hasChildrenEnumerable = false;
        try
        {
            var childrenProp = obj.GetType().GetProperty("Children", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (childrenProp != null)
            {
                var val = childrenProp.GetValue(obj);
                if (val is IEnumerable)
                {
                    childrenVal = val;
                    hasChildrenEnumerable = true;
                }
            }
        }
        catch { }

        if (hasChildrenEnumerable && childrenVal is IEnumerable childrenEnum)
        {
            foreach (var c in childrenEnum) yield return c!;
            yield break;
        }

        object? contentVal = null;
        bool hasContent = false;
        try
        {
            var contentProp = obj.GetType().GetProperty("Content", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (contentProp != null)
            {
                var val = contentProp.GetValue(obj);
                if (val != null)
                {
                    contentVal = val;
                    hasContent = true;
                }
            }
        }
        catch { }

        if (hasContent && contentVal != null)
        {
            yield return contentVal;
        }

        yield break;
    }

    private static (string name, int? index) ParseSegment(string seg)
    {
        if (string.IsNullOrEmpty(seg)) return ("", null);
        var name = seg;
        int? idx = null;
        var idxStart = seg.IndexOf('[');
        if (idxStart >= 0)
        {
            var idxEnd = seg.IndexOf(']', idxStart + 1);
            if (idxEnd > idxStart)
            {
                var numStr = seg.Substring(idxStart + 1, idxEnd - idxStart - 1);
                if (int.TryParse(numStr, out var num)) idx = num;
                name = seg[..idxStart];
            }
        }
        return (name, idx);
    }
}
