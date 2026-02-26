namespace SUIM.Parse.Components.Attributes;

public enum Visibility
{
    Visible = 0,
    Hidden,
    Collapsed
}

public static class VisibilityExtensions
{
    extension(Visibility)
    {
        public static Visibility Parse(string? value)
        {
            if (value == null) return default;

            return Enum.TryParse<Visibility>(value, true, out var r) ? r : default;
        }
    }
}
