using UnityEngine;

[System.Serializable]
public class HeightTextureRule
{
    public string TextureID;
    [Range(0f, 2f)] public float BaseWeight = 1f;
    [Range(0f, 1f)] public float WeightAtZero = 0f;
    [Range(0f, 1f)] public float WeightAtLow = 0f;
    [Range(0f, 1f)] public float WeightAtMid = 0f;
    [Range(0f, 1f)] public float WeightAtHigh = 0f;
    [Range(0f, 1f)] public float WeightAtOne = 0f;
    [Range(0.001f, 0.25f)] public float NoiseScale = 0.04f;
    [Range(0.5f, 4f)] public float NoiseExponent = 1.5f;
    public Vector2 NoiseOffset = Vector2.zero;
}

[System.Serializable]
public class HeightTexturePainter : BaseTexturePainter
{
    private static readonly float[] HeightAnchors = { 0f, 0.25f, 0.5f, 0.75f, 1f };

    [SerializeField] bool NormalizeAgainstCurrentTerrainRange = true;
    [SerializeField] HeightTextureRule[] TextureRules;

    public override void Execute(in TexturePainterContext context)
    {
        if (TextureRules == null || TextureRules.Length == 0)
        {
            Debug.LogWarning($"HeightTexturePainter on {gameObject.name} has no configured texture rules.");
            return;
        }

        float terrainHeightSpan = context.MaxTerrainHeight - context.MinTerrainHeight;
        int[] ruleLayers = new int[TextureRules.Length];
        float[] ruleWeights = new float[TextureRules.Length];

        for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
        {
            HeightTextureRule rule = TextureRules[ruleIndex];
            if (string.IsNullOrWhiteSpace(rule.TextureID))
            {
                Debug.LogWarning($"HeightTexturePainter on {gameObject.name} has a texture rule with no texture ID.");
                return;
            }

            ruleLayers[ruleIndex] = context.GetLayerForTexture(rule.TextureID);
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

                float totalWeight = 0f;
                for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
                {
                    float ruleWeight = EvaluateTextureRule(TextureRules[ruleIndex], sampledHeight, x, y);
                    ruleWeights[ruleIndex] = ruleWeight;
                    totalWeight += ruleWeight;
                }

                if (totalWeight <= Mathf.Epsilon)
                    continue;

                for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
                {
                    float ruleWeight = ruleWeights[ruleIndex];
                    if (ruleWeight <= Mathf.Epsilon)
                        continue;

                    int layerIndex = ruleLayers[ruleIndex];
                    float contribution = Strength * (ruleWeight / totalWeight);
                    context.AlphaMaps[x, y, layerIndex] = Mathf.Max(context.AlphaMaps[x, y, layerIndex], contribution);
                }
            }
        }
    }

    private static float EvaluateTextureRule(HeightTextureRule rule, float normalizedHeight, int x, int y)
    {
        float heightWeight = EvaluateHeightProfile(rule, normalizedHeight);
        if (heightWeight <= Mathf.Epsilon)
            return 0f;

        return rule.BaseWeight * heightWeight * EvaluateRuleNoise(rule, x, y);
    }

    private static float EvaluateHeightProfile(HeightTextureRule rule, float normalizedHeight)
    {
        float clampedHeight = Mathf.Clamp01(normalizedHeight);
        float[] weights =
        {
            rule.WeightAtZero,
            rule.WeightAtLow,
            rule.WeightAtMid,
            rule.WeightAtHigh,
            rule.WeightAtOne
        };

        for (int anchorIndex = 0; anchorIndex < HeightAnchors.Length - 1; ++anchorIndex)
        {
            float minAnchor = HeightAnchors[anchorIndex];
            float maxAnchor = HeightAnchors[anchorIndex + 1];
            if (clampedHeight > maxAnchor)
                continue;

            return Mathf.Lerp(weights[anchorIndex], weights[anchorIndex + 1], Mathf.InverseLerp(minAnchor, maxAnchor, clampedHeight));
        }

        return weights[weights.Length - 1];
    }

    private static float EvaluateRuleNoise(HeightTextureRule rule, int x, int y)
    {
        if (rule.NoiseScale <= Mathf.Epsilon)
            return 1f;

        float sampleX = (x + rule.NoiseOffset.x) * rule.NoiseScale;
        float sampleY = (y + rule.NoiseOffset.y) * rule.NoiseScale;
        float noise = Mathf.PerlinNoise(sampleX, sampleY);
        return Mathf.Pow(Mathf.Max(0.0001f, noise), rule.NoiseExponent);
    }

}
