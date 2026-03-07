namespace SUIM.Parse;

public sealed class SpanStringIgnoreCaseComparer : IEqualityComparer<string>, IAlternateEqualityComparer<ReadOnlySpan<char>, string>
{
    public static readonly SpanStringIgnoreCaseComparer Instance = new();

    public bool Equals(string? x, string? y)
    {
        return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(string obj)
    {
        return string.GetHashCode(obj, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(ReadOnlySpan<char> alternate, string other)
    {
        return alternate.Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(ReadOnlySpan<char> alternate)
    {
        return string.GetHashCode(alternate, StringComparison.OrdinalIgnoreCase);
    }

    public string Create(ReadOnlySpan<char> alternate)
    {
        return alternate.ToString();
    }
}
