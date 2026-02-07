namespace SUIM.Components;

public class BaseText : UIElement
{
    public string? Text { get; set; }
    public string? Font { get; set; }
    public int FontSize { get; set; }
    public bool Wrap { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            Text = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("font", StringComparison.OrdinalIgnoreCase))
        {
            Font = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
        }
        else if (name.Equals("fontsize", StringComparison.OrdinalIgnoreCase))
        {
            FontSize = value is int i ? i : Convert.ToInt32(value);
        }
        else if (name.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            Color = value as string ?? throw new ArgumentException($"Value for attribute '{name}' must be a non-null string.");
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

public class Label : BaseText
{
    public string? For { get; set; }
}

public class H1Element : BaseText { }

public class H2Element : BaseText { }

public class H3Element : BaseText { }

public class H4Element : BaseText { }

public class H5Element : BaseText { }

public class H6Element : BaseText { }

public class H7Element : BaseText { }

public class H8Element : BaseText { }

public class PElement : BaseText { }
