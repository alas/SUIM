namespace SUIM;

public static class SUIM
{
    public static dynamic Create(object model)
    {
        var observable = new ObservableObject();
        observable.Initialize(model);
        return observable;
    }
}
