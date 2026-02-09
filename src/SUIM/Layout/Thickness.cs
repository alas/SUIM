using System.Numerics;

namespace SUIM.Layout;

public record struct Thickness(UnitValue Left, UnitValue Top, UnitValue Right, UnitValue Bottom)
{
    public static readonly Thickness None = new(UnitValue.None);

    public Thickness(float uniformValue) : this(new UnitValue(uniformValue)) { }
    public Thickness(float horizontal, float vertical) : this(new UnitValue(horizontal), new UnitValue(vertical)) { }
    public Thickness(float left, float top, float right, float bottom) : this(new UnitValue(left), new UnitValue(top), new UnitValue(right), new UnitValue(bottom)) { }
    public Thickness(UnitValue uniformValue) : this(uniformValue, uniformValue, uniformValue, uniformValue) { }
    public Thickness(UnitValue horizontal, UnitValue vertical) : this(horizontal, vertical, horizontal, vertical) { }

    public static Thickness FromObject(object? obj)
    {
        if (obj is Thickness thickness)
            return thickness;
        if (obj is UnitValue uv)
            return new Thickness(uv);
        if (obj is string str)
            return Parse(str);
        if (obj.IsNumericType())
            return new Thickness(new UnitValue(Convert.ToSingle(obj)));

        throw new ArgumentException($"Cannot convert object of type '{obj?.GetType()}'.");
    }

    public static Thickness Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new Thickness(new UnitValue(0, UnitType.Pixels), new UnitValue(0, UnitType.Pixels), new UnitValue(0, UnitType.Pixels), new UnitValue(0, UnitType.Pixels));

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
            return new Thickness(uv, uv, uv, uv);
        }
        else if (parts.Length == 2)
        {
            var horizontal = UnitValue.Parse(parts[0]);
            var vertical = UnitValue.Parse(parts[1]);
            return new Thickness(horizontal, vertical, horizontal, vertical);
        }
        else if (parts.Length == 4)
        {
            return new Thickness(
                UnitValue.Parse(parts[0]),
                UnitValue.Parse(parts[1]),
                UnitValue.Parse(parts[2]),
                UnitValue.Parse(parts[3])
            );
        }

        throw new FormatException($"Invalid unit value format: '{value}'");
    }
}
