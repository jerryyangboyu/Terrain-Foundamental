using UnityEngine;

[System.Serializable]
public class HeightTextureBand
{
    public string TextureID;
    [Range(0f, 1f)] public float MinHeight = 0f;
    [Range(0f, 1f)] public float MaxHeight = 1f;
    [Range(0f, 0.5f)] public float BlendRange = 0.05f;
    [Range(0f, 1f)] public float Strength = 1f;
}

public class HeightTexturePainter : BaseTexturePainter
{
    [SerializeField] bool NormalizeAgainstCurrentTerrainRange = true;
    [SerializeField] HeightTextureBand[] Bands;

    public override void Execute(in TexturePainterContext context)
    {
        if (Bands == null || Bands.Length == 0)
        {
            Debug.LogWarning($"HeightTexturePainter on {gameObject.name} has no configured texture bands.");
            return;
        }

        float terrainHeightSpan = context.MaxTerrainHeight - context.MinTerrainHeight;
        int[] bandLayers = new int[Bands.Length];

        for (int bandIndex = 0; bandIndex < Bands.Length; ++bandIndex)
        {
            HeightTextureBand band = Bands[bandIndex];
            if (string.IsNullOrWhiteSpace(band.TextureID))
            {
                Debug.LogWarning($"HeightTexturePainter on {gameObject.name} has a band with no texture ID.");
                return;
            }

            if (band.MaxHeight < band.MinHeight)
            {
                Debug.LogWarning($"HeightTexturePainter on {gameObject.name} has a band where MinHeight is greater than MaxHeight.");
                return;
            }

            bandLayers[bandIndex] = context.GetLayerForTexture(band.TextureID);
        }

        for (int y = 0; y < context.AlphaMapResolution; ++y)
        {
            int heightMapY = Mathf.FloorToInt((float)y * context.MapResolution / context.AlphaMapResolution);

            for (int x = 0; x < context.AlphaMapResolution; ++x)
            {
                int heightMapX = Mathf.FloorToInt((float)x * context.MapResolution / context.AlphaMapResolution);

                if (context.BiomeIndex >= 0 && context.BiomeMap[heightMapX, heightMapY] != context.BiomeIndex)
                    continue;

                float sampledHeight = context.HeightMap[heightMapX, heightMapY];
                if (NormalizeAgainstCurrentTerrainRange && terrainHeightSpan > Mathf.Epsilon)
                {
                    sampledHeight = Mathf.InverseLerp(context.MinTerrainHeight, context.MaxTerrainHeight, sampledHeight);
                }

                for (int bandIndex = 0; bandIndex < Bands.Length; ++bandIndex)
                {
                    HeightTextureBand band = Bands[bandIndex];
                    float intensity = EvaluateHeight(sampledHeight, band.MinHeight, band.MaxHeight, band.BlendRange);
                    if (intensity <= 0f)
                        continue;

                    context.AlphaMaps[x, y, bandLayers[bandIndex]] = Mathf.Max(context.AlphaMaps[x, y, bandLayers[bandIndex]], Strength * band.Strength * intensity);
                }
            }
        }
    }

    private static float EvaluateHeight(float normalizedHeight, float minHeight, float maxHeight, float blendRange)
    {
        if (blendRange <= 0f)
            return normalizedHeight >= minHeight && normalizedHeight <= maxHeight ? 1f : 0f;

        if (normalizedHeight < minHeight - blendRange || normalizedHeight > maxHeight + blendRange)
            return 0f;

        if (normalizedHeight < minHeight)
            return Mathf.InverseLerp(minHeight - blendRange, minHeight, normalizedHeight);

        if (normalizedHeight > maxHeight)
            return 1f - Mathf.InverseLerp(maxHeight, maxHeight + blendRange, normalizedHeight);

        return 1f;
    }
}
