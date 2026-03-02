namespace SUIM.Parse.Components;

public class Border() : UIElement(nameof(Border))
{
    public string? Thickness { get; set; }

    public override void SetAttribute(string name, object? value)
    {
        if (name.Equals("thickness", StringComparison.OrdinalIgnoreCase))
        {
            Thickness = value as string;
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
            var thicknessStr = string.Join(",", parts.Take(numCount));
            Thickness = thicknessStr;
        }
        
        if (numCount < parts.Length)
        {
             Color = parts[^1]; // Color is last
        }
    }
}
