namespace SUIM.Flexbox;

public readonly record struct Value(float ValueUnit, Unit Unit)
{
    public static readonly Value UndefinedValue = new(float.NaN, Unit.Undefined);
    public static readonly Value Zero = new(0, Unit.Point);
    public static readonly Value Auto = new(float.NaN, Unit.Auto);

    public static void CopyValue(Value[] dest, Value[] src)
    {
        for (int i = 0; i < src.Length; i++)
        {
            dest[i]= src[i];
        }
    }
}
