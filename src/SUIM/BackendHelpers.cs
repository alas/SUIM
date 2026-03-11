namespace SUIM;

using System;

/// <summary>
/// Backend-agnostic helpers used by engine-specific integrations (Stride, Unity, Godot, etc.).
/// Contains color parsing, model method resolution and property binding setup utilities.
/// </summary>
public static class BackendHelpers
{
    public readonly record struct ParsedColor(byte R, byte G, byte B, byte A = 255);

    public static ParsedColor ParseColor(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr)) return new ParsedColor(255, 255, 255);

        if (colorStr.StartsWith('#'))
        {
            var hex = colorStr[1..];
            if (hex.Length == 6)
            {
                return new ParsedColor(
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));
            }
            
            if (hex.Length == 8)
            {
                return new ParsedColor(
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16),
                    Convert.ToByte(hex.Substring(6, 2), 16));
            }
        }

        // named colors (basic set)
        if (string.Equals(colorStr, "red", StringComparison.OrdinalIgnoreCase)) return new ParsedColor(255, 0, 0);
        if (string.Equals(colorStr, "green", StringComparison.OrdinalIgnoreCase)) return new ParsedColor(0, 255, 0);
        if (string.Equals(colorStr, "blue", StringComparison.OrdinalIgnoreCase)) return new ParsedColor(0, 0, 255);
        if (string.Equals(colorStr, "black", StringComparison.OrdinalIgnoreCase)) return new ParsedColor(0, 0, 0);
        if (string.Equals(colorStr, "yellow", StringComparison.OrdinalIgnoreCase)) return new ParsedColor(255, 255, 0);
        if (string.Equals(colorStr, "cyan", StringComparison.OrdinalIgnoreCase)) return new ParsedColor(0, 255, 255);
        if (string.Equals(colorStr, "magenta", StringComparison.OrdinalIgnoreCase)) return new ParsedColor(255, 0, 255);
        if (string.Equals(colorStr, "transparent", StringComparison.OrdinalIgnoreCase)) return new ParsedColor(0, 0, 0, 0);
        if (string.Equals(colorStr, "white", StringComparison.OrdinalIgnoreCase)) return new ParsedColor(255, 255, 255);

        // fallback white
        throw new NotImplementedException($"named color not supported: {colorStr}");
    }
}
