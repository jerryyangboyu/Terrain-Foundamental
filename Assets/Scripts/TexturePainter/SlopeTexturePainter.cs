using UnityEngine;

[System.Serializable]
public class SlopeTextureRule
{
    public string TextureID;
    [Range(0f, 2f)] public float BaseWeight = 1f;
    [Range(0f, 90f)] public float MinSlopeDegrees = 35f;
    [Range(0f, 90f)] public float MaxSlopeDegrees = 70f;
    [Range(0.1f, 20f)] public float BlendRangeDegrees = 6f;
    [Range(0.001f, 0.25f)] public float NoiseScale = 0.04f;
    [Range(0f, 1f)] public float NoiseStrength = 0.35f;
    [Range(0.5f, 4f)] public float NoiseExponent = 1.5f;
    public Vector2 NoiseOffset = Vector2.zero;
}

public class SlopeTexturePainter : BaseTexturePainter
{
    [SerializeField] SlopeTextureRule[] TextureRules;
    [SerializeField] [Range(0, 4)] int SlopeSmoothingRadius = 1;
    [SerializeField] [Range(0f, 1f)] float SlopeSmoothing = 0.65f;
    [SerializeField] [Range(0f, 90f)] float CliffSuppressionStartDegrees = 78f;
    [SerializeField] [Range(0.1f, 20f)] float CliffSuppressionBlendDegrees = 6f;

    public override void Execute(in TexturePainterContext context)
    {
        if (TextureRules == null || TextureRules.Length == 0)
        {
            Debug.LogWarning($"SlopeTexturePainter on {gameObject.name} has no configured texture rules.");
            return;
        }

        int[] ruleLayers = new int[TextureRules.Length];
        float[] targetWeights = new float[TextureRules.Length];
        float[] existingWeights = new float[context.AlphaMaps.GetLength(2)];
        float[] targetLayerWeights = new float[context.AlphaMaps.GetLength(2)];

        for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
        {
            SlopeTextureRule rule = TextureRules[ruleIndex];
            if (string.IsNullOrWhiteSpace(rule.TextureID))
            {
                Debug.LogWarning($"SlopeTexturePainter on {gameObject.name} has a texture rule with no texture ID.");
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

                float slopeDegrees = EvaluateSlopeDegrees(context.SlopeMap, x, y);
                float cliffSuppression = EvaluateCliffSuppression(slopeDegrees);
                if (cliffSuppression <= Mathf.Epsilon)
                    continue;

                float totalWeight = 0f;
                float blendMask = 0f;
                for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
                {
                    SlopeTextureRule rule = TextureRules[ruleIndex];
                    float slopeCoverage = EvaluateSlopeCoverage(rule, slopeDegrees) * cliffSuppression;
                    blendMask = Mathf.Max(blendMask, slopeCoverage);

                    float ruleWeight = slopeCoverage <= Mathf.Epsilon
                        ? 0f
                        : rule.BaseWeight * slopeCoverage * EvaluateRuleNoise(rule, x, y);
                    targetWeights[ruleIndex] = ruleWeight;
                    totalWeight += ruleWeight;
                }

                blendMask = Mathf.Clamp01(blendMask * Strength);
                if (blendMask <= Mathf.Epsilon || totalWeight <= Mathf.Epsilon)
                    continue;

                float existingWeightSum = 0f;
                for (int layerIndex = 0; layerIndex < existingWeights.Length; ++layerIndex)
                {
                    float existingWeight = context.GetAlpha(x, y, layerIndex);
                    existingWeights[layerIndex] = existingWeight;
                    targetLayerWeights[layerIndex] = 0f;
                    existingWeightSum += existingWeight;
                }

                if (existingWeightSum <= Mathf.Epsilon)
                    continue;

                for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
                {
                    float ruleWeight = targetWeights[ruleIndex];
                    if (ruleWeight <= Mathf.Epsilon)
                        continue;

                    int layerIndex = ruleLayers[ruleIndex];
                    targetLayerWeights[layerIndex] += ruleWeight / totalWeight;
                }

                float outputWeightSum = 0f;
                for (int layerIndex = 0; layerIndex < existingWeights.Length; ++layerIndex)
                {
                    float existingNormalizedWeight = existingWeights[layerIndex] / existingWeightSum;
                    float blendedWeight = Mathf.Lerp(existingNormalizedWeight, targetLayerWeights[layerIndex], blendMask);
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

    private float EvaluateSlopeDegrees(float[,] slopeMap, int x, int y)
    {
        float centerSlope = slopeMap[y, x];
        if (SlopeSmoothingRadius <= 0 || SlopeSmoothing <= Mathf.Epsilon)
            return centerSlope;

        float smoothedSlope = 0f;
        int sampleCount = 0;
        int height = slopeMap.GetLength(0);
        int width = slopeMap.GetLength(1);

        for (int offsetY = -SlopeSmoothingRadius; offsetY <= SlopeSmoothingRadius; ++offsetY)
        {
            int sampleY = Mathf.Clamp(y + offsetY, 0, height - 1);

            for (int offsetX = -SlopeSmoothingRadius; offsetX <= SlopeSmoothingRadius; ++offsetX)
            {
                int sampleX = Mathf.Clamp(x + offsetX, 0, width - 1);
                smoothedSlope += slopeMap[sampleY, sampleX];
                ++sampleCount;
            }
        }

        if (sampleCount <= 0)
            return centerSlope;

        smoothedSlope /= sampleCount;
        return Mathf.Lerp(centerSlope, smoothedSlope, SlopeSmoothing);
    }

    private float EvaluateCliffSuppression(float slopeDegrees)
    {
        if (CliffSuppressionBlendDegrees <= Mathf.Epsilon)
            return slopeDegrees >= CliffSuppressionStartDegrees ? 0f : 1f;

        float suppression = Mathf.SmoothStep(
            0f,
            1f,
            Mathf.InverseLerp(
                CliffSuppressionStartDegrees,
                CliffSuppressionStartDegrees + CliffSuppressionBlendDegrees,
                slopeDegrees));
        return 1f - suppression;
    }

    private static float EvaluateSlopeCoverage(SlopeTextureRule rule, float slopeDegrees)
    {
        float minSlope = Mathf.Min(rule.MinSlopeDegrees, rule.MaxSlopeDegrees);
        float maxSlope = Mathf.Max(rule.MinSlopeDegrees, rule.MaxSlopeDegrees);
        float blendRange = Mathf.Max(0.01f, rule.BlendRangeDegrees);

        float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(minSlope - blendRange, minSlope + blendRange, slopeDegrees));
        float fall = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(maxSlope - blendRange, maxSlope + blendRange, slopeDegrees));
        return Mathf.Clamp01(Mathf.Min(rise, fall));
    }

    private static float EvaluateRuleNoise(SlopeTextureRule rule, int x, int y)
    {
        if (rule.NoiseScale <= Mathf.Epsilon)
            return 1f;

        float sampleX = (x + rule.NoiseOffset.x) * rule.NoiseScale;
        float sampleY = (y + rule.NoiseOffset.y) * rule.NoiseScale;
        float noise = Mathf.PerlinNoise(sampleX, sampleY);
        float shapedNoise = Mathf.Pow(Mathf.Max(0.0001f, noise), rule.NoiseExponent);
        return Mathf.Lerp(1f, shapedNoise, rule.NoiseStrength);
    }
}
