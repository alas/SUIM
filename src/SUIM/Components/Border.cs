namespace SUIM.Components;

public class Border : UIElement
{
    public Thickness BorderThickness { get; set; } = new Thickness(0);
    public string? BorderColor { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("thickness", StringComparison.OrdinalIgnoreCase))
        {
            BorderThickness = Thickness.Parse(value as string);
        }
        else if (name.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            BorderColor = value as string;
        }
        else if (name.Equals("border", StringComparison.OrdinalIgnoreCase))
        {
             // Parse shorthand: "10 White" or "10 5 0 2 White"
            var str = value as string;
             ParseShorthand(str);
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    private void ParseShorthand(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // Heuristic: Last part is color if it's not a number? Or try to parse thickness from start.
        // Spec examples: "10 White", "10 5 0 2 White".
        // Color is last. Numbers are first.
        
        // Find where numbers end.
        int numCount = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            if (char.IsDigit(parts[i][0]) || parts[i] == "0") // Simplistic check
                numCount++;
            else
                break; 
        }
        
        if (numCount > 0)
        {
            string thicknessStr = string.Join(",", parts.Take(numCount));
            BorderThickness = Thickness.Parse(thicknessStr);
        }
        
        if (numCount < parts.Length)
        {
             BorderColor = parts[parts.Length - 1]; // Color is last
        }
    }
}
