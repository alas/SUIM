namespace SUIM.Layout;

public class LayoutContext(float rootFontSize = 16f, float availableWidth = 0f, float availableHeight = 0f)
{
    public UnitConverter UnitConverter { get; } = new UnitConverter(rootFontSize);
    public float AvailableWidth { get; set; } = availableWidth;
    public float AvailableHeight { get; set; } = availableHeight;
    public float CurrentFontSize { get => field; set => UnitConverter.ParentFontSize = field = value; } = rootFontSize;
    public float RootFontSize { get; set; } = rootFontSize;

    public LayoutContext CreateChildContext(float childFontSize)
    {
        return new LayoutContext(RootFontSize, AvailableWidth, AvailableHeight)
        {
            CurrentFontSize = childFontSize
        };
    }
}
