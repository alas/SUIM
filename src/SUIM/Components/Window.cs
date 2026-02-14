namespace SUIM.Components;

using System;
using System.Xml.Linq;

public class Window : LayoutElement
{
    private readonly Text _titleText = new();
    public string? Title
    {
        get => _titleText.Value;
        set => _titleText.Value = value;
    }

    // Root grid: top row for title bar (auto), second row fills remaining space (1fr)
    private readonly Grid _rootGrid = new() { Rows = "auto, 1fr" };
    // Title bar grid: two columns - close button (auto) and title (1fr)
    private readonly Grid _titleBarGrid = new() { Columns = "auto, 1fr" };
    // content grid fills remaining space
    private readonly Grid _contentGrid = new();
    private readonly Button _closeButton = new();

    public Window() : base()
    {
        // simple label inside the button
        var closeLabel = new Text { Value = "X" };
        _closeButton.AddChild(closeLabel, null);

        // add close button and title to title bar and set their column positions
        _titleBarGrid.AddChild(_closeButton, null);
        _titleBarGrid.AddChild(_titleText, null);
        // title should be in column 1
        if (_titleBarGrid.GridChildren.Count > 0)
            _titleBarGrid.GridChildren[^1].Column = 1;

        // add title bar (row 0) and content grid (row 1) to root grid
        _rootGrid.AddChild(_titleBarGrid, null);
        _rootGrid.AddChild(_contentGrid, null);
        if (_rootGrid.GridChildren.Count > 1)
            _rootGrid.GridChildren[^1].Row = 1;

        // make the root grid the single visible child of this Window
        base.AddChild(_rootGrid, null);
    }

    // Children declared inside the <window>...</window> markup should be added to the content grid
    public override void AddChild(UIElement child, XElement? element)
    {
        _contentGrid.AddChild(child, element);
    }

    public override void RemoveChild(UIElement child)
    {
        _contentGrid.RemoveChild(child);
    }

    public override void ClearChildren()
    {
        _contentGrid.ClearChildren();
    }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("title", StringComparison.OrdinalIgnoreCase))
        {
            Title = value as string ?? value?.ToString();
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}
