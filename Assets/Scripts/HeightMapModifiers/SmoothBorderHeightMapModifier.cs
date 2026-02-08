using UnityEngine;

public class SmoothBorderHeightMapModifier: BaseHeightMapModifier
{
    [SerializeField] [Min(1)] int BorderWidth = 6;
    [SerializeField] [Min(3)] int KernelSize = 3;
    [SerializeField] [Min(1)] int Stride = 1;
    [SerializeField] [Min(1)] int SmoothPasses = 3;

    private void OnValidate()
    {
        if (KernelSize < 3)
        {
            KernelSize = 3;
        }
        if (KernelSize % 2 == 0)
        {
            KernelSize += 1;
        }
        if (Stride < 1)
        {
            Stride = 1;
        }
    }

    public override void Execute(int mapResolutionSize, float[,] heightMap, Vector3 heightmapScale, byte[,] biomeMap, BiomeConfigSO biomeConfig, int biomeIndex)
    {
        if (biomeMap == null || mapResolutionSize <= 0)
        {
            return;
        }

        int effectiveBorderWidth = Mathf.Min(BorderWidth, mapResolutionSize - 1);
        if (effectiveBorderWidth <= 0)
        {
            return;
        }

        for (int pass = 0; pass < SmoothPasses; pass++)
        {
            float[,] sourceHeightMap = (float[,])heightMap.Clone();

            for (int y = 0; y < mapResolutionSize; y++)
            {
                for (int x = 0; x < mapResolutionSize; x++)
                {
                    int currentBiomeIndex = biomeMap[x, y];
                    if (biomeIndex >= 0 && currentBiomeIndex != biomeIndex)
                    {
                        continue;
                    }

                    if (!IsNearBiomeBorder(x, y, mapResolutionSize, biomeMap, currentBiomeIndex, effectiveBorderWidth))
                    {
                        continue;
                    }

                    int kernelRadius = Mathf.Max(1, (KernelSize - 1) / 2);
                    float smoothedHeight = CalculateKernelSmoothedHeight(x, y, mapResolutionSize, sourceHeightMap, kernelRadius, Stride);
                    heightMap[y, x] = Mathf.Lerp(sourceHeightMap[y, x], smoothedHeight, Strength);
                }
            }
        }
    }

    private static bool IsNearBiomeBorder(int x, int y, int mapResolutionSize, byte[,] biomeMap, int biomeIndex, int borderWidth)
    {
        int minY = Mathf.Max(0, y - borderWidth);
        int maxY = Mathf.Min(mapResolutionSize - 1, y + borderWidth);
        int minX = Mathf.Max(0, x - borderWidth);
        int maxX = Mathf.Min(mapResolutionSize - 1, x + borderWidth);

        for (int scanY = minY; scanY <= maxY; scanY++)
        {
            for (int scanX = minX; scanX <= maxX; scanX++)
            {
                if (biomeMap[scanX, scanY] != biomeIndex)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static float CalculateKernelSmoothedHeight(int x, int y, int mapResolutionSize, float[,] sourceHeightMap, int sampleRadius, int stride)
    {
        int minY = Mathf.Max(0, y - sampleRadius);
        int maxY = Mathf.Min(mapResolutionSize - 1, y + sampleRadius);
        int minX = Mathf.Max(0, x - sampleRadius);
        int maxX = Mathf.Min(mapResolutionSize - 1, x + sampleRadius);

        float sum = 0f;
        int count = 0;

        for (int sampleY = minY; sampleY <= maxY; sampleY += stride)
        {
            for (int sampleX = minX; sampleX <= maxX; sampleX += stride)
            {
                sum += sourceHeightMap[sampleY, sampleX];
                count++;
            }
        }

        return count > 0 ? sum / count : sourceHeightMap[y, x];
    }
}
