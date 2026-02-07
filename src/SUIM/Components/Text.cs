using static System.Net.Mime.MediaTypeNames;

namespace SUIM;

public class BaseText : UIElement
{
    public string? Text { get; set; }
    public string? Font { get; set; }
    public int FontSize { get; set; }
    public bool Wrap { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        switch (name)
        {
            case "text":
                Text = Convert.ToString(value);
                break;
            case "font":
                Font = Convert.ToString(value);
                break;
            case "fontsize":
                FontSize = Convert.ToInt32(value);
                break;
            case "color":
                Color = Convert.ToString(value);
                break;
            case "wrap":
                Wrap = Convert.ToBoolean(value);
                break;
            default:
                base.SetAttribute(name, value);
                break;
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
        switch (name)
        {
            case "rows":
                Rows = Convert.ToInt32(value);
                break;
            case "columns":
                Columns = Convert.ToInt32(value);
                break;
            default:
                base.SetAttribute(name, value);
                break;
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
