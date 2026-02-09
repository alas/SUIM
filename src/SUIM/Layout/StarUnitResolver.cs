namespace SUIM.Layout;

public static class StarUnitResolver
{
    public static float[] ResolveStarUnits(float[] starValues, float availableSpace)
    {
        if (starValues.Length == 0) return [];
        
        float totalStars = 0f;
        foreach (var value in starValues)
        {
            totalStars += value;
        }
        
        if (totalStars == 0f) return new float[starValues.Length];
        
        float spacePerStar = availableSpace / totalStars;
        float[] resolvedValues = new float[starValues.Length];
        
        for (int i = 0; i < starValues.Length; i++)
        {
            resolvedValues[i] = starValues[i] * spacePerStar;
        }
        
        return resolvedValues;
    }
}
