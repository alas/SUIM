namespace Chess3d.SUIM.Components;

using SUIMComponent = global::SUIM.Parse.Components.UIComponent;

public class Popup() : SUIMComponent(nameof(Popup))
{
    protected override void InitializeComponent()
    {
    }

    public void OnClosingInternal()
    {
        var onClosingHandler = Model!.onClosing;
        var cancel = onClosingHandler != null && onClosingHandler();
        if (!cancel)
        {
            Model.visibility = "collapsed";
        }
    }

    public void OnNoInternal()
    {
        var onNoHandler = Model!.onNo;
        if (onNoHandler == null)
        {
            Model.visibility = "collapsed";
        }
        else
        {
            onNoHandler();
        }
    }
}
