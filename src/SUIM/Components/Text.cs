namespace SUIM.Components;

public class Text : UIElement
{
    public Text() : base() { }

    public string? Value { get; set; }
    public bool Wrap { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            Value = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
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
}

public class TextArea : UIElement, IPlaceholder
{
    public string? Placeholder { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }

    public TextArea() : base() { }

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
}

public class Label : Text
{
    public string? For { get; set; }

    public Label() : base() { }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("rows", StringComparison.OrdinalIgnoreCase))
        {
            For = value is string s ? s : value?.ToString();
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}

public class PElement : Text { public PElement() : base() { } }

public class H1Element : Text { public H1Element() : base() { } }

public class H2Element : Text { public H2Element() : base() { } }

public class H3Element : Text { public H3Element() : base() { } }

public class H4Element : Text { public H4Element() : base() { } }

public class H5Element : Text { public H5Element() : base() { } }

public class H6Element : Text { public H6Element() : base() { } }

public class H7Element : Text { public H7Element() : base() { } }

public class H8Element : Text { public H8Element() : base() { } }
