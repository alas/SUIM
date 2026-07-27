namespace SUIM.Binding;

using System;
using SUIM.Parse.Components;

public static class BindingDelegates
{
    public static Action? ToAction(Delegate? handler, UIElement context)
    {
        if (handler == null) return null;

        return handler switch
        {
            Action a => a,
            Action<UIElement> a => () => a(context),
            //Action<object?> a => () => a(context),
            EventHandler eh => () => eh(context, EventArgs.Empty),
            _ => null
        };
    }

    public static Func<bool>? ToFuncBool(Delegate? handler, UIElement context)
    {
        if (handler == null) return null;

        return handler switch
        {
            Func<bool> f => f,
            Func<UIElement, bool> f => () => f(context),
            //Func<object?, bool> f => () => f(context),
            _ => null
        };
    }
}
