namespace Chess3d.SUIM.Components;

using SUIMComponent = global::SUIM.Parse.Components.VirtualComponent;

public class Popup() : SUIMComponent(nameof(Popup))
{
    public void OnYesInternal()
    {
        Model!.onYes?.Invoke();
    }

    public void OnClosingInternal()
    {
        var handler = Model!.onClosing;
        var cancel = handler != null && handler();
        if (!cancel)
        {
            Model.visibility = "collapsed";
        }
    }

    public void OnNoInternal()
    {
        var handler = Model!.onNo;
        if (handler != null)
        {
            handler();
        }
        else
        {
            Model.visibility = "collapsed";
        }
    }
}
