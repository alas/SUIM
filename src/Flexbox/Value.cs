namespace Flexbox;

public class Value(float v, Unit u)
{
    public float value = v;
    public Unit unit = u;

    public static Value UndefinedValue
    {
        get
        {
            return new Value(float.NaN, Unit.Undefined);
        }
    }

    public static void CopyValue(Value[] dest, Value[] src)
    {
        for (int i = 0; i < src.Length; i++)
        {
            dest[i].value = src[i].value;
            dest[i].unit = src[i].unit;
        }
    }

    public Value Clone() => new(value, unit);
}
