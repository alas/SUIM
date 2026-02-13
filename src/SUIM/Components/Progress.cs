namespace SUIM.Components;

public class Progress : UIElement
{
    public Progress() : base() { }

    public float Value { get; set; } = 0f;
    public float Maximum { get; set; } = 100f;
}
