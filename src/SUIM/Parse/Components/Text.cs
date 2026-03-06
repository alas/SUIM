namespace SUIM.Parse.Components;

public class Text : UIElement
{
    public static Flexbox.MeasureFunc? MeasureFunc = null;

    public string? Value { get; set; }
    public bool Wrap { get; set; }

    public Text(string? tagName = null) : base(tagName ?? nameof(Text))
    {
        if (MeasureFunc != null)
        {
            Node.SetMeasureFunc(MeasureFunc);
        }
    }

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
        else
        {
            base.SetAttribute(name, value);
        }
    }

    public override string? GetAttribute(string name)
    {
        if (name.Equals("value", StringComparison.OrdinalIgnoreCase)) return Value;
        if (name.Equals("wrap", StringComparison.OrdinalIgnoreCase)) return Wrap.ToString();
        if (name.Equals("font", StringComparison.OrdinalIgnoreCase)) return Parent!.Font;
        if (name.Equals("font-size", StringComparison.OrdinalIgnoreCase) || name.Equals("fontsize", StringComparison.OrdinalIgnoreCase)) return Parent!.FontSize;
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

public class Label() : UIElement(nameof(Label))
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

public class P() : UIElement(nameof(P)) { }

public class H1() : UIElement(nameof(H1)) { }

public class H2() : UIElement(nameof(H2)) { }

public class H3() : UIElement(nameof(H3)) { }

public class H4() : UIElement(nameof(H4)) { }

public class H5() : UIElement(nameof(H5)) { }

public class H6() : UIElement(nameof(H6)) { }

public class H7() : UIElement(nameof(H7)) { }

public class H8() : UIElement(nameof(H8)) { }
