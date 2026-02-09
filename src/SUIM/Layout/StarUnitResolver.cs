using SUIM.Layout;

public static class StarUnitResolver
{
    public static float[] ResolveStarUnits(UnitValue[] starUnits, float availableSpace)
    {
        if (starUnits.Length == 0) return [];
        
        float totalStars = 0f;
        foreach (var unit in starUnits)
        {
            if (unit.Type == UnitType.Star)
                totalStars += unit.Value;
        }
        
        if (totalStars == 0f) return new float[starUnits.Length];
        
        float spacePerStar = availableSpace / totalStars;
        float[] resolvedValues = new float[starUnits.Length];
        
        for (int i = 0; i < starUnits.Length; i++)
        {
            resolvedValues[i] = starUnits[i].Type == UnitType.Star 
                ? starUnits[i].Value * spacePerStar 
                : 0f;
        }
        
        return resolvedValues;
    }
}
