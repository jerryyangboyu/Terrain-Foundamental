using UnityEngine;

public static class LakeWaterLevelResolver
{
    public static float ResolveSeaLevelWorldY(ProcGenManager procGenManager, float fallbackWorldY)
    {
        return ResolveSeaLevelWorldY(procGenManager != null ? procGenManager.Configuration : null, fallbackWorldY);
    }

    public static float ResolveSeaLevelWorldY(ProcGenConfigSO procGenConfig, float fallbackWorldY)
    {
        if (procGenConfig == null)
        {
            return fallbackWorldY;
        }

        SetValueHeightMapModifier initialHeightModifier = procGenConfig.InitialHeightModifier != null
            ? procGenConfig.InitialHeightModifier.GetComponent<SetValueHeightMapModifier>()
            : null;
        if (initialHeightModifier == null)
        {
            return fallbackWorldY;
        }

        BiomeConfigSO lakeBiome = FindLakeBiome(procGenConfig);
        if (lakeBiome?.HeightModifier == null)
        {
            return fallbackWorldY;
        }

        float seaLevelWorldY = initialHeightModifier.WorldTargetHeight;
        OffsetHeightMapModifier[] lakeHeightOffsets = lakeBiome.HeightModifier.GetComponents<OffsetHeightMapModifier>();
        if (lakeHeightOffsets.Length == 0)
        {
            return fallbackWorldY;
        }

        foreach (OffsetHeightMapModifier lakeHeightOffset in lakeHeightOffsets)
        {
            seaLevelWorldY += lakeHeightOffset.WorldOffsetAmount;
        }

        return seaLevelWorldY;
    }

    static BiomeConfigSO FindLakeBiome(ProcGenConfigSO procGenConfig)
    {
        if (procGenConfig.Biomes == null)
        {
            return null;
        }

        foreach (BiomeConfig biomeConfig in procGenConfig.Biomes)
        {
            if (biomeConfig.Biome != null
                && string.Equals(biomeConfig.Biome.Name, "Lake", System.StringComparison.OrdinalIgnoreCase))
            {
                return biomeConfig.Biome;
            }
        }

        return null;
    }
}
