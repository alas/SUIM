namespace SUIM.Parse.Components.Attributes;

public enum HorizontalAlignment
{
    Unspecified = 0,
    Left,
    Center,
    Right
}

public static class HorizontalAlignmentExtensions
{
    extension(HorizontalAlignment)
    {
        public static HorizontalAlignment Parse(string? value)
        {
            if (value == null) return default;

            if (value.Equals("flex-start", StringComparison.OrdinalIgnoreCase) || value.Equals("start", StringComparison.OrdinalIgnoreCase)) return HorizontalAlignment.Left;

            if (value.Equals("flex-end", StringComparison.OrdinalIgnoreCase) || value.Equals("end", StringComparison.OrdinalIgnoreCase)) return HorizontalAlignment.Right;

            return Enum.TryParse<HorizontalAlignment>(value, true, out var r) ? r : default;
        }
    }
}
