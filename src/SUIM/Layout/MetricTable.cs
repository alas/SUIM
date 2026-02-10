namespace SUIM.Layout;

using System.Collections.Concurrent;
using SUIM.Components;

public interface IFontMetricsProvider
{
    // Return a 256-entry per-character width multiplier array for the given font name and size.
    // Multipliers are relative to font size (e.g. 0.6 means 0.6 * fontSize pixels for that char).
    float[] GetMetrics(string fontName, float fontSize);

    // Return line height as a multiplier of font size (e.g., 1.2 = 1.2 * fontSize pixels)
    float GetLineHeightMultiplier(string fontName, float fontSize);
}

public static class MetricTable
{
    // Default fallback multipliers
    private static readonly float[] _default = new float[256];
    private const float _defaultLineHeightMultiplier = 1.0f;
    
    private static readonly ConcurrentDictionary<(string fontName, float fontSize), float[]> _cache = new();
    private static readonly ConcurrentDictionary<(string fontName, float fontSize), float> _lineHeightCache = new();
    private static IFontMetricsProvider? _provider;

    static MetricTable()
    {
        for (int i = 0; i < 256; i++) _default[i] = 0.6f;
        _default[' '] = 0.33f;
        _default['i'] = 0.33f; _default['l'] = 0.33f; _default['I'] = 0.4f;
        _default['m'] = 0.95f; _default['w'] = 0.95f;
        _default['.'] = 0.25f; _default[','] = 0.25f; _default[':'] = 0.25f; _default[';'] = 0.25f;
        _default['-'] = 0.4f; _default['_'] = 0.6f; _default['('] = 0.4f; _default[')'] = 0.4f;
        for (int c = '0'; c <= '9'; c++) _default[c] = 0.55f;
        for (int c = 'A'; c <= 'Z'; c++) _default[c] = 0.75f;
        for (int c = 'a'; c <= 'z'; c++) _default[c] = 0.62f;
    }

    // Register a runtime provider (e.g. Stride font loader). Passing null clears the provider.
    public static void RegisterProvider(IFontMetricsProvider? provider)
    {
        _provider = provider;
        _cache.Clear();
        _lineHeightCache.Clear();
    }

    // Pre-scan the markup tree and populate cache with metrics from provider for all fonts/sizes found.
    public static void PrePopulateFromTree(UIElement root)
    {
        if (_provider == null) return;

        var fontSizes = CollectFonts(root);
        foreach (var (fontName, fontSize) in fontSizes)
        {
            Cache(fontName, fontSize);
        }
    }

    // Collect all (font, fontSize) pairs used in the tree.
    private static HashSet<(string fontName, float fontSize)> CollectFonts(UIElement element)
    {
        var result = new HashSet<(string, float)>();
        CollectFontsRecursive(element, result);
        return result;
    }

    private static void CollectFontsRecursive(UIElement element, HashSet<(string fontName, float fontSize)> fonts)
    {
        float fontSize = element.FontSize > 0 ? element.FontSize : (element.RootFontSize > 0 ? element.RootFontSize : 16f);
        string fontName = element.Font ?? element.RootFont ?? "__default__";
        fonts.Add((fontName, fontSize));

        foreach (var child in element.Children)
        {
            CollectFontsRecursive(child, fonts);
        }
    }

    // Cache metrics for a specific font/size by calling the provider.
    private static void Cache(string fontName, float fontSize)
    {
        var key = (fontName, fontSize);
        
        if (!_cache.ContainsKey(key) && _provider != null)
        {
            try
            {
                var metrics = _provider.GetMetrics(fontName, fontSize);
                if (metrics != null) _cache[key] = metrics;
                else _cache[key] = _default;
            }
            catch
            {
                _cache[key] = _default;
            }
        }

        if (!_lineHeightCache.ContainsKey(key) && _provider != null)
        {
            try
            {
                float multiplier = _provider.GetLineHeightMultiplier(fontName, fontSize);
                _lineHeightCache[key] = multiplier;
            }
            catch
            {
                _lineHeightCache[key] = _defaultLineHeightMultiplier;
            }
        }
    }

    // Measure text using per-font/size metrics when available, otherwise fall back to defaults.
    public static float MeasureText(string text, string? fontName, float fontSize)
    {
        if (string.IsNullOrEmpty(text)) return 0f;
        if (fontSize <= 0) fontSize = 16f;

        var keyName = fontName ?? "__default__";
        var key = (keyName, fontSize);

        if (!_cache.TryGetValue(key, out var metrics))
        {
            metrics = _default;
            if (_provider != null)
            {
                try
                {
                    var providerMetrics = _provider.GetMetrics(keyName, fontSize);
                    if (providerMetrics != null) metrics = providerMetrics;
                }
                catch
                {
                    // Use default on error
                }
            }
            _cache[key] = metrics;
        }

        float w = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            var code = (int)text[i];
            if (code < 0 || code >= 256) code = '?';
            w += metrics[code] * fontSize;
        }
        return w;
    }

    // Get line height in pixels using per-font/size metrics when available.
    public static float GetLineHeight(string? fontName, float fontSize)
    {
        if (fontSize <= 0) fontSize = 16f;

        float multiplier;
        var keyName = fontName ?? "__default__";
        var key = (keyName, fontSize);

        if (!_lineHeightCache.TryGetValue(key, out multiplier))
        {
            if (_provider != null)
            {
                try
                {
                    multiplier = _provider.GetLineHeightMultiplier(keyName, fontSize);
                }
                catch
                {
                    multiplier = _defaultLineHeightMultiplier;
                }
            }
            else
            {
                multiplier = _defaultLineHeightMultiplier;
            }
            _lineHeightCache[key] = multiplier;
        }

        return multiplier * fontSize;
    }
}
