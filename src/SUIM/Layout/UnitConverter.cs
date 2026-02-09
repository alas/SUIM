namespace SUIM.Layout;

public class UnitConverter(float rootFontSize = 16f)
{
    public float RootFontSize { get; set; } = rootFontSize;
    public float ParentFontSize { get; set; } = rootFontSize;

    public float ConvertToPixels(UnitValue unitValue, float availableSpace = 0f)
    {
        return unitValue.Type switch
        {
            UnitType.Pixels => unitValue.Value,
            UnitType.Rem => unitValue.Value * RootFontSize,
            UnitType.Em => unitValue.Value * ParentFontSize,
            UnitType.Auto => 0f, // Will be calculated during layout
            UnitType.Star => 0f, // Will be calculated during star distribution
            _ => 0f
        };
    }
    
    public float ConvertToPixels(string value, float availableSpace = 0f)
    {
        return ConvertToPixels(UnitValue.Parse(value), availableSpace);
    }
}
