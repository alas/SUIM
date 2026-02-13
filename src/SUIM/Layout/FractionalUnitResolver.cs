namespace SUIM.Layout;

public static class FractionalUnitResolver
{
    public static float[] ResolveFractionalUnits(float[] frValues, float availableSpace)
    {
        if (frValues.Length == 0) return [];
        
        float total = 0f;
        foreach (var value in frValues)
        {
            total += value;
        }
        
        if (total == 0f) return new float[frValues.Length];
        
        float spacePerFr = availableSpace / total;
        float[] resolvedValues = new float[frValues.Length];
        
        for (int i = 0; i < frValues.Length; i++)
        {
            resolvedValues[i] = frValues[i] * spacePerFr;
        }
        
        return resolvedValues;
    }
}
