using UnityEngine;

[System.Serializable]
public class HeightTextureRule
{
    public string TextureID;
    public bool IsExclusive;
    [Range(0f, 2f)] public float BaseWeight = 1f;
    [Range(0f, 1f)] public float WeightAtLow = 0f;
    [Range(0f, 1f)] public float WeightAtMid = 0f;
    [Range(0f, 1f)] public float WeightAtHigh = 0f;
    [Range(0f, 1f)] public float WeightAtPeak = 0f;
    [Range(0.001f, 0.25f)] public float NoiseScale = 0.04f;
    [Range(0.5f, 4f)] public float NoiseExponent = 1.5f;
    public Vector2 NoiseOffset = Vector2.zero;
}

public class HeightTexturePainter : BaseTexturePainter
{
    private const float LowAnchorRatio = 0.3f;
    private const float MidAnchorRatio = 0.65f;

    [SerializeField] HeightTextureRule[] TextureRules;
    [SerializeField] [Range(0f, 1f)] float PeakStartHeight = 0.7f;

    public override void Execute(in TexturePainterContext context)
    {
        if (TextureRules == null || TextureRules.Length == 0)
        {
            Debug.LogWarning($"HeightTexturePainter on {gameObject.name} has no configured texture rules.");
            return;
        }

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
            for (int x = 0; x < context.AlphaMapResolution; ++x)
            {
                if (!context.TargetsBiomeAtAlpha(x, y))
                    continue;

                float sampledHeight = context.GetNormalizedHeightAtAlpha(x, y);

                float totalRegularWeight = 0f;
                int bestExclusiveRuleIndex = -1;
                float bestExclusiveWeight = 0f;
                float bestRegularWeight = 0f;
                for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
                {
                    HeightTextureRule rule = TextureRules[ruleIndex];
                    float ruleWeight = EvaluateTextureRule(rule, sampledHeight, x, y);
                    ruleWeights[ruleIndex] = ruleWeight;

                    if (rule.IsExclusive)
                    {
                        if (ruleWeight > bestExclusiveWeight)
                        {
                            bestExclusiveWeight = ruleWeight;
                            bestExclusiveRuleIndex = ruleIndex;
                        }
                    }
                    else if (ruleWeight > bestRegularWeight)
                    {
                        bestRegularWeight = ruleWeight;
                        totalRegularWeight += ruleWeight;
                    }
                    else
                    {
                        totalRegularWeight += ruleWeight;
                    }
                }

                if (bestExclusiveRuleIndex >= 0 && bestExclusiveWeight > bestRegularWeight)
                {
                    for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
                    {
                        int layerIndex = ruleLayers[ruleIndex];
                        context.SetAlpha(x, y, layerIndex, 0f);
                    }

                    context.SetAlpha(x, y, ruleLayers[bestExclusiveRuleIndex], Strength);
                    continue;
                }

                if (totalRegularWeight <= Mathf.Epsilon)
                    continue;

                for (int ruleIndex = 0; ruleIndex < TextureRules.Length; ++ruleIndex)
                {
                    if (TextureRules[ruleIndex].IsExclusive)
                        continue;

                    float ruleWeight = ruleWeights[ruleIndex];
                    if (ruleWeight <= Mathf.Epsilon)
                        continue;

                    int layerIndex = ruleLayers[ruleIndex];
                    float contribution = Strength * (ruleWeight / totalRegularWeight);
                    context.SetAlpha(x, y, layerIndex, Mathf.Max(context.GetAlpha(x, y, layerIndex), contribution));
                }
            }
        }
    }

    private float EvaluateTextureRule(HeightTextureRule rule, float normalizedHeight, int x, int y)
    {
        float heightWeight = EvaluateHeightProfile(rule, normalizedHeight);
        if (heightWeight <= Mathf.Epsilon)
            return 0f;

        return rule.BaseWeight * heightWeight * EvaluateRuleNoise(rule, x, y);
    }

    private float EvaluateHeightProfile(HeightTextureRule rule, float normalizedHeight)
    {
        float clampedHeight = Mathf.Clamp01(normalizedHeight);
        float peakStartHeight = Mathf.Clamp(PeakStartHeight, 0.0001f, 0.9999f);
        float lowHeight = peakStartHeight * LowAnchorRatio;
        float midHeight = Mathf.Max(lowHeight + 0.0001f, peakStartHeight * MidAnchorRatio);

        if (clampedHeight <= lowHeight)
            return rule.WeightAtLow;

        if (clampedHeight <= midHeight)
            return Mathf.Lerp(rule.WeightAtLow, rule.WeightAtMid, Mathf.InverseLerp(lowHeight, midHeight, clampedHeight));

        if (clampedHeight <= peakStartHeight)
            return Mathf.Lerp(rule.WeightAtMid, rule.WeightAtHigh, Mathf.InverseLerp(midHeight, peakStartHeight, clampedHeight));

        return Mathf.Lerp(rule.WeightAtHigh, rule.WeightAtPeak, Mathf.InverseLerp(peakStartHeight, 1f, clampedHeight));
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
