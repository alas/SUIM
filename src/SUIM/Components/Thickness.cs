namespace SUIM.Components;

public class Thickness
{
    public float Left { get; set; }
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }

    public Thickness() { }

    public Thickness(float uniform)
    {
        Left = Top = Right = Bottom = uniform;
    }

    public Thickness(float horizontal, float vertical)
    {
        Left = Right = horizontal;
        Top = Bottom = vertical;
    }

    public Thickness(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>
    /// Parses a thickness string with 1, 2, or 4 comma-separated values.
    /// - 1 value: applied to all sides (uniform)
    /// - 2 values: first is horizontal (left/right), second is vertical (top/bottom)
    /// - 4 values: left, top, right, bottom
    /// </summary>
    public static Thickness Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new Thickness(0);

        var parts = value.Split(',')
            .Select(s => float.TryParse(s.Trim(), out float f) ? f : 0)
            .ToArray();

        return parts.Length switch
        {
            1 => new Thickness(parts[0]),
            2 => new Thickness(parts[0], parts[1]),
            4 => new Thickness(parts[0], parts[1], parts[2], parts[3]),
            _ => new Thickness(0)
        };
    }

    public override string ToString()
    {
        if (Left == Top && Top == Right && Right == Bottom)
            return Left.ToString();
        
        if (Left == Right && Top == Bottom)
            return $"{Left},{Top}";
        
        return $"{Left},{Top},{Right},{Bottom}";
    }
}
