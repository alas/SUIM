namespace Chess3d.SUIM.Components;

using SUIMComponent = global::SUIM.Parse.Components.VirtualComponent;

public class Popup() : SUIMComponent(nameof(Popup))
{
    public void OnYesInternal()
    {
        if (Model!.onYes is Action action)
        {
            action();
        }
        else if (Model.onYes is Action<SUIMComponent> actionComp)
        {
            actionComp(this);
        }
        else if (Model.onYes is Action<object?> actionObj)
        {
            actionObj(this);
        }
    }

    public void OnClosingInternal()
    {
        var handler = Model!.onClosing;
        var cancel = false;
        if (handler is Func<bool> f)
        {
            cancel = f();
        }
        else if (handler is Func<SUIMComponent, bool> fcomp)
        {
            cancel = fcomp(this);
        }
        else if (handler is Func<object?, bool> fobj)
        {
            cancel = fobj(this);
        }

        if (!cancel)
        {
            Model.visibility = "collapsed";
        }
    }

    public void OnNoInternal()
    {
        var handled = false;
        if (Model!.onNo is Action action)
        {
            action();
            handled = true;
        }
        else if (Model.onNo is Action<SUIMComponent> actionComp)
        {
            actionComp(this);
            handled = true;
        }
        else if (Model.onNo is Action<object?> actionObj)
        {
            actionObj(this);
            handled = true;
        }

        if (!handled)
        {
            Model.visibility = "collapsed";
        }
    }
}
