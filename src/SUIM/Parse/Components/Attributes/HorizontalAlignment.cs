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

            return Enum.TryParse<HorizontalAlignment>(value, true, out var r) ? r : default;
        }
    }
}
