namespace SUIM;

public class BaseText : UIElement
{
    public string? Text { get; set; }
    public string? Font { get; set; }
    public int FontSize { get; set; }
    public bool Wrap { get; set; }
}

public class TextArea : UIElement, IPlaceholder
{
    public string? Placeholder { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
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
