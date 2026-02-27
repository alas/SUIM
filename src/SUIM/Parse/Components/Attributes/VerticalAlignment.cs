namespace SUIM.Parse.Components.Attributes;

public enum VerticalAlignment
{
    Unspecified = 0,
    Top,
    Center,
    Bottom
}

public static class VerticalAlignmentExtensions
{
    extension(VerticalAlignment)
    {
        public static VerticalAlignment Parse(string? value)
        {
            if (value == null) return default;

            if (value.Equals("start", StringComparison.OrdinalIgnoreCase) || value.Equals("flex-start", StringComparison.OrdinalIgnoreCase)) return VerticalAlignment.Top;

            if (value.Equals("end", StringComparison.OrdinalIgnoreCase) || value.Equals("flex-end", StringComparison.OrdinalIgnoreCase)) return VerticalAlignment.Bottom;

            return Enum.TryParse<VerticalAlignment>(value, true, out var r) ? r : default;
        }
    }
}
