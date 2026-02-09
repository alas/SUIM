namespace SUIM.Layout;

public record struct UnitValue(float Value, UnitType Type)
{
    public static UnitValue Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new UnitValue(0, UnitType.Pixels);
            
        value = value.Trim();
        
        // Handle star units
        if (value.EndsWith("*"))
        {
            if (value == "*")
                return new UnitValue(1, UnitType.Star);
            else if (float.TryParse(value[..^1], out float starValue))
                return new UnitValue(starValue, UnitType.Star);
        }
        
        // Handle auto
        if (value.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return new UnitValue(0, UnitType.Auto);
            
        // Handle rem
        if (value.EndsWith("rem"))
        {
            if (float.TryParse(value[..^3], out float remValue))
                return new UnitValue(remValue, UnitType.Rem);
        }
        
        // Handle em
        if (value.EndsWith("em"))
        {
            if (float.TryParse(value[..^2], out float emValue))
                return new UnitValue(emValue, UnitType.Em);
        }
        
        // Default to pixels
        if (float.TryParse(value, out float pixelValue))
            return new UnitValue(pixelValue, UnitType.Pixels);
            
        return new UnitValue(0, UnitType.Pixels);
    }
}

public record struct UnitValue4(UnitValue Left, UnitValue Top, UnitValue Right, UnitValue Bottom)
{
    public static UnitValue4 Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new UnitValue4(new UnitValue(0, UnitType.Pixels), new UnitValue(0, UnitType.Pixels), new UnitValue(0, UnitType.Pixels), new UnitValue(0, UnitType.Pixels));
        
        var parts = value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.All(x => char.IsNumber(x))
                || p == "*"
                || p.EndsWith("px", StringComparison.OrdinalIgnoreCase)
                || string.Compare(p, "rem", StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(p, "em", StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(p, "auto", StringComparison.OrdinalIgnoreCase) == 0)
            .ToArray();
        if (parts.Length == 1)
        {
            var uv = UnitValue.Parse(parts[0]);
            return new UnitValue4(uv, uv, uv, uv);
        }
        else if (parts.Length == 2)
        {
            var vertical = UnitValue.Parse(parts[0]);
            var horizontal = UnitValue.Parse(parts[1]);
            return new UnitValue4(horizontal, vertical, horizontal, vertical);
        }
        else if (parts.Length == 4)
        {
            return new UnitValue4(
                UnitValue.Parse(parts[0]),
                UnitValue.Parse(parts[1]),
                UnitValue.Parse(parts[2]),
                UnitValue.Parse(parts[3])
            );
        }
        
        throw new FormatException($"Invalid unit value format: '{value}'");
    }

    public static implicit operator UnitValue4(UnitValue uniformValue) => new(uniformValue, uniformValue, uniformValue, uniformValue);
}

public enum UnitType
{
    Pixels,
    Rem,      // Root em - relative to root font size
    Em,       // Relative to parent font size  
    Star,     // Proportional space
    Auto      // Content-based sizing
}
