namespace SUIM.Parse.Components;

public class Text(string? tagName = null) : UIElement(tagName ?? nameof(Text))
{
    public string? Value { get; set; }
    public bool Wrap { get; set; }
    public string? Font { get; set; }
    public string? FontSize { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            Value = value as string;
        }
        else if (name.Equals("wrap", StringComparison.OrdinalIgnoreCase))
        {
            Wrap = value is bool b ? b : Convert.ToBoolean(value);
        }
        else if (name.Equals("font", StringComparison.OrdinalIgnoreCase))
        {
            Font = value as string;
        }
        else if (name.Equals("font-size", StringComparison.OrdinalIgnoreCase) || name.Equals("fontsize", StringComparison.OrdinalIgnoreCase))
        {
            FontSize = value as string;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override string? GetAttribute(string name)
    {
        if (name.Equals("value", StringComparison.OrdinalIgnoreCase)) return Value;
        if (name.Equals("wrap", StringComparison.OrdinalIgnoreCase)) return Wrap.ToString();
        if (name.Equals("font", StringComparison.OrdinalIgnoreCase)) return Font;
        if (name.Equals("font-size", StringComparison.OrdinalIgnoreCase) || name.Equals("fontsize", StringComparison.OrdinalIgnoreCase)) return FontSize;
        return base.GetAttribute(name);
    }
}

public class TextArea() : Text(nameof(TextArea)), IPlaceholder
{
    public string? Placeholder { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("rows", StringComparison.OrdinalIgnoreCase))
        {
            Rows = value is int i ? i : Convert.ToInt32(value);
        }
        else if (name.Equals("columns", StringComparison.OrdinalIgnoreCase))
        {
            Columns = value is int i ? i : Convert.ToInt32(value);
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override string? GetAttribute(string name)
    {
        if (name.Equals("rows", StringComparison.OrdinalIgnoreCase)) return Rows.ToString();
        if (name.Equals("columns", StringComparison.OrdinalIgnoreCase)) return Columns.ToString();
        return base.GetAttribute(name);
    }
}

public class Label() : Text(nameof(Label))
{
    public string? For { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("for", StringComparison.OrdinalIgnoreCase))
        {
            For = value is string s ? s : value?.ToString();
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override string? GetAttribute(string name)
    {
        if (name.Equals("for", StringComparison.OrdinalIgnoreCase)) return For;
        return base.GetAttribute(name);
    }
}

public class P() : Text(nameof(P)) { }

public class H1() : Text(nameof(H1)) { }

public class H2() : Text(nameof(H2)) { }

public class H3() : Text(nameof(H3)) { }

public class H4() : Text(nameof(H4)) { }

public class H5() : Text(nameof(H5)) { }

public class H6() : Text(nameof(H6)) { }

public class H7() : Text(nameof(H7)) { }

public class H8() : Text(nameof(H8)) { }
