using UnityEngine;

public static class LakeWaterLevelResolver
{
    public static float ResolveSeaLevelWorldY(ProcGenManager procGenManager, float fallbackWorldY)
    {
        return ResolveSeaLevelWorldY(procGenManager != null ? procGenManager.Configuration : null, fallbackWorldY);
    }

    public static float ResolveSeaLevelWorldY(ProcGenConfigSO procGenConfig, float fallbackWorldY)
    {
        return TryResolveSeaLevelWorldY(procGenConfig, out float seaLevelWorldY)
            ? seaLevelWorldY
            : fallbackWorldY;
    }

    public static bool TryResolveSeaLevelWorldY(ProcGenManager procGenManager, out float seaLevelWorldY)
    {
        return TryResolveSeaLevelWorldY(procGenManager != null ? procGenManager.Configuration : null, out seaLevelWorldY);
    }

    public static bool TryResolveSeaLevelWorldY(ProcGenConfigSO procGenConfig, out float seaLevelWorldY)
    {
        seaLevelWorldY = 0f;
        if (procGenConfig == null)
        {
            return false;
        }

        SetValueHeightMapModifier initialHeightModifier = procGenConfig.InitialHeightModifier != null
            ? procGenConfig.InitialHeightModifier.GetComponent<SetValueHeightMapModifier>()
            : null;
        if (initialHeightModifier == null)
        {
            return false;
        }

        BiomeConfigSO lakeBiome = FindLakeBiome(procGenConfig);
        if (lakeBiome?.HeightModifier == null)
        {
            return false;
        }

        OffsetHeightMapModifier[] lakeHeightOffsets = lakeBiome.HeightModifier.GetComponents<OffsetHeightMapModifier>();
        if (lakeHeightOffsets.Length == 0)
        {
            return false;
        }

        seaLevelWorldY = initialHeightModifier.WorldTargetHeight;
        foreach (OffsetHeightMapModifier lakeHeightOffset in lakeHeightOffsets)
        {
            seaLevelWorldY += lakeHeightOffset.WorldOffsetAmount;
        }

        return true;
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
