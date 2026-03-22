using UnityEngine;

[System.Serializable]
public class RandomTextureRule
{
    public string TextureID;
    [Range(0f, 2f)] public float BaseWeight = 1f;
    [Range(0.001f, 0.25f)] public float NoiseScale = 0.04f;
    [Range(0.5f, 4f)] public float NoiseExponent = 1.5f;
    public Vector2 NoiseOffset = Vector2.zero;
}

public class RandomTexturePainter : BaseTexturePainter
{
    [SerializeField] RandomTextureRule[] TextureRules;
    [SerializeField] [Range(1f, 8f)] float PatchContrast = 2.5f;

    public override void Execute(in TexturePainterContext context)
    {
        if (TextureRules == null || TextureRules.Length == 0)
        {
            Debug.LogWarning($"RandomTexturePainter on {gameObject.name} has no configured texture rules.");
            return;
        }

        int[] ruleLayers = new int[TextureRules.Length];
        float[] ruleWeights = new float[TextureRules.Length];
        float[] existingWeights = new float[context.AlphaMaps.GetLength(2)];
        float[] targetLayerWeights = new float[context.AlphaMaps.GetLength(2)];

        for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
        {
            RandomTextureRule rule = TextureRules[ruleIndex];
            if (string.IsNullOrWhiteSpace(rule.TextureID))
            {
                Debug.LogWarning($"RandomTexturePainter on {gameObject.name} has a texture rule with no texture ID.");
                return;
            }

            ruleLayers[ruleIndex] = context.GetLayerForTexture(rule.TextureID);
        }

        for (int y = 0; y < context.AlphaMapResolution; ++y)
        {
            for (int x = 0; x < context.AlphaMapResolution; ++x)
            {
                if (!context.TargetsBiomeAtAlpha(x, y))
                    continue;

                float totalRuleWeight = 0f;
                for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
                {
                    float rawWeight = EvaluateRuleNoise(TextureRules[ruleIndex], x, y);
                    float shapedWeight = rawWeight <= Mathf.Epsilon
                        ? 0f
                        : Mathf.Pow(rawWeight, PatchContrast);
                    ruleWeights[ruleIndex] = shapedWeight;
                    totalRuleWeight += shapedWeight;
                }

                if (totalRuleWeight <= Mathf.Epsilon)
                    continue;

                float existingWeightSum = 0f;
                for (int layerIndex = 0; layerIndex < existingWeights.Length; ++layerIndex)
                {
                    float existingWeight = context.GetAlpha(x, y, layerIndex);
                    existingWeights[layerIndex] = existingWeight;
                    targetLayerWeights[layerIndex] = 0f;
                    existingWeightSum += existingWeight;
                }

                for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
                {
                    float ruleWeight = ruleWeights[ruleIndex];
                    if (ruleWeight <= Mathf.Epsilon)
                        continue;

                    int layerIndex = ruleLayers[ruleIndex];
                    targetLayerWeights[layerIndex] += ruleWeight / totalRuleWeight;
                }

                if (existingWeightSum <= Mathf.Epsilon || Strength >= 0.999f)
                {
                    for (int layerIndex = 0; layerIndex < existingWeights.Length; ++layerIndex)
                    {
                        context.SetAlpha(x, y, layerIndex, targetLayerWeights[layerIndex]);
                    }

                    continue;
                }

                float outputWeightSum = 0f;
                for (int layerIndex = 0; layerIndex < existingWeights.Length; ++layerIndex)
                {
                    float existingNormalizedWeight = existingWeights[layerIndex] / existingWeightSum;
                    float blendedWeight = Mathf.Lerp(existingNormalizedWeight, targetLayerWeights[layerIndex], Strength);
                    context.SetAlpha(x, y, layerIndex, blendedWeight);
                    outputWeightSum += blendedWeight;
                }

                if (outputWeightSum <= Mathf.Epsilon)
                    continue;

                for (int layerIndex = 0; layerIndex < existingWeights.Length; ++layerIndex)
                {
                    context.SetAlpha(x, y, layerIndex, context.GetAlpha(x, y, layerIndex) / outputWeightSum);
                }
            }
        }
    }

    private static float EvaluateRuleNoise(RandomTextureRule rule, int x, int y)
    {
        if (rule.NoiseScale <= Mathf.Epsilon)
            return rule.BaseWeight;

        float sampleX = (x + rule.NoiseOffset.x) * rule.NoiseScale;
        float sampleY = (y + rule.NoiseOffset.y) * rule.NoiseScale;
        float primaryNoise = Mathf.PerlinNoise(sampleX, sampleY);
        float detailNoise = Mathf.PerlinNoise(sampleX * 2.13f + 19.1f, sampleY * 2.13f + 7.3f);
        float combinedNoise = (primaryNoise * 0.7f) + (detailNoise * 0.3f);
        return rule.BaseWeight * Mathf.Pow(Mathf.Max(0.0001f, combinedNoise), rule.NoiseExponent);
    }
}
