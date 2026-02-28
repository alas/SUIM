namespace SUIM.Layout;

public static class FractionalUnit
{
    public static float[] Resolve(float[] frValues, float availableSpace)
    {
        if (frValues.Length == 0) return [];
        availableSpace = Sanitize(availableSpace);
        
        var total = 0f;
        foreach (var value in frValues)
        {
            total += value;
        }
        
        if (total == 0f) return new float[frValues.Length];
        
        var spacePerFr = availableSpace / total;
        var resolvedValues = new float[frValues.Length];
        
        for (int i = 0; i < frValues.Length; i++)
        {
            resolvedValues[i] = frValues[i] * spacePerFr;
        }
        
        return resolvedValues;
    }

    public static float Sanitize(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0) return 0;
        return value;
    }

    public static bool IsInvalid(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) || value < 0;
    }
}
