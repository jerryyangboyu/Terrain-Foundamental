using UnityEngine;

public class NoiseHeightMapModifier : BaseHeightMapModifier
{
    [SerializeField] float HeightDelta = 5f;
    [SerializeField] float XScale = 1f;
    [SerializeField] float YScale = 1f;

    public override void Execute(int mapResolutionSize, float[,] heightMap, Vector3 heightmapScale, byte[,] biomeMap, BiomeConfigSO biomeConfig, int biomeIndex)
    {
        for (int y = 0; y < mapResolutionSize; y++)
        {
            for (int x = 0; x < mapResolutionSize; x++)
            {
                if (biomeIndex >= 0 && biomeMap[x, y] != biomeIndex) continue;
                float noise = Mathf.PerlinNoise(x * XScale, y * YScale) * 2f - 1f;
                float newHeight = heightMap[x, y] + (noise * HeightDelta / heightmapScale.y);
                heightMap[x, y] = Mathf.Lerp(heightMap[x, y], newHeight, Strength);
            }
        }
    }
}
