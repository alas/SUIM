namespace SUIM.Layout;

public class LayoutContext(float rootFontSize = 16f, float availableWidth = 0f, float availableHeight = 0f)
{
    public float RootFontSize { get; } = rootFontSize;
    public float AvailableWidth { get; } = availableWidth;
    public float AvailableHeight { get; } = availableHeight;
    public UnitConverter UnitConverter { get; } = new UnitConverter(rootFontSize);
}
