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

            return Enum.TryParse<VerticalAlignment>(value, true, out var r) ? r : default;
        }
    }
}
