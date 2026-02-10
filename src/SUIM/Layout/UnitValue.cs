namespace SUIM.Layout;

public record struct UnitValue(float Value, UnitType Type = UnitType.Pixels)
{
    public static readonly UnitValue None = new (0, UnitType.None);

    public static UnitValue FromObject(object? obj)
    {
        if (obj is UnitValue uv)
            return uv;
        if (obj is string str)
            return Parse(str);
        if (obj.IsNumericType())
            return new UnitValue(Convert.ToSingle(obj));

        throw new ArgumentException($"Cannot convert object of type '{obj?.GetType()}'.");
    }

    public static UnitValue Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new UnitValue(0, UnitType.None);
            
        value = value.Trim();
        
        // Handle star units
        if (value.EndsWith('*'))
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

public enum UnitType
{
    None,
    Pixels,
    Rem,      // Root em - relative to root font size
    Em,       // Relative to parent font size  
    Star,     // Proportional space
    Auto      // Content-based sizing
}

public static class NumberExtensions
{
    public static bool IsNumericType(this object? value)
    {
        return value is sbyte ||
               value is byte ||
               value is short ||
               value is ushort ||
               value is int ||
               value is uint ||
               value is long ||
               value is ulong ||
               value is float ||
               value is double ||
               value is decimal;
    }
}
