namespace SUIM.Components.Attributes;

[Flags]
public enum Anchor
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    Right = 8
}

public static class AnchorExtensions
{
    extension (Anchor)
    {
        public static Anchor Parse(string? value)
        {
            if (value == null) return default;

            var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Anchor result = Anchor.None;

            foreach (var part in parts)
            {
                if (Enum.TryParse<Anchor>(part, true, out var anchor))
                {
                    result |= anchor;
                }
            }

            return result;
        }

    }

    public static bool HasFlagFast(this Anchor value, Anchor flag)
    {
        return (value & flag) == flag;
    }
}
