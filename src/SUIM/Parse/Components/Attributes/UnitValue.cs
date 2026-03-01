namespace SUIM.Parse.Components.Attributes;

public record struct UnitValue(float Value, UnitType Type = UnitType.Pixels)
{
    public static readonly UnitValue None = new(0, UnitType.None);
    public static readonly UnitValue Auto = new(0, UnitType.Auto);
    public static readonly UnitValue OneFR = new(1, UnitType.Fr);

    public static UnitValue FromObject(object? obj)
    {
        if (obj is UnitValue uv)
            return uv;
        if (obj is string str)
            return Parse(str);
        if (obj is float f)
            return new UnitValue(f, UnitType.Pixels);
        if (obj.IsNumericType())
            return new UnitValue(Convert.ToSingle(obj));

        throw new ArgumentException($"Cannot convert object of type '{obj?.GetType()}'.");
    }

    public static UnitValue Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new UnitValue(0, UnitType.None);
            
        value = value.Trim();

        // Handle fr units (fractional units - web-friendly replacement for FractionalUnits)
        if (value.EndsWith("fr", StringComparison.OrdinalIgnoreCase))
        {
            if (value.Equals("fr", StringComparison.OrdinalIgnoreCase))
                return new UnitValue(1, UnitType.Fr);
            else if (float.TryParse(value[..^2], out float frValue))
                return new UnitValue(frValue, UnitType.Fr);
        }
        
        // Handle auto
        if (value.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return new UnitValue(0, UnitType.Auto);
        
        // Handle px suffix (web-friendly)
        if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            if (float.TryParse(value[..^2], out float pxValue))
                return new UnitValue(pxValue, UnitType.Pixels);
        }
        
        // Default to pixels
        if (float.TryParse(value, out float pixelValue))
            return new UnitValue(pixelValue, UnitType.Pixels);
            
        return new UnitValue(0, UnitType.Pixels);
    }

    public readonly bool IsExplicit() => Type != UnitType.None && Type != UnitType.Auto;
}

public enum UnitType
{
    None,
    Pixels,
    Rem,      // Root em - relative to root font size
    Fr,       // Fractional units - proportional space (CSS Grid's fr unit)
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
