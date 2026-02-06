namespace SUIM;

public class BaseText : UIElement
{
    public string Text { get; set; } = string.Empty;
}

public class TextAreaElement : UIElement
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
