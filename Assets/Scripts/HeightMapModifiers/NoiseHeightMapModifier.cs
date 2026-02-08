using System;
using UnityEngine;

public class NoiseHeightMapModifier : BaseHeightMapModifier
{
    [SerializeField] float amplitude = 20f;
    [SerializeField] float xFrequency = 0.05f;
    [SerializeField] float yFrequency = 0.05f;

    [SerializeField] int octaves = 5;
    [SerializeField] float xLacunlarity = 2f;
    [SerializeField] float yLacunlarity = 2f;
    [SerializeField] float persistence = 0.5f;

    public override void Execute(int mapResolutionSize, float[,] heightMap, Vector3 heightmapScale, byte[,] biomeMap, BiomeConfigSO biomeConfig, int biomeIndex)
    {
        float currentAmplitude = amplitude;
        float currentXFrequency = xFrequency;
        float currentYFrequency = yFrequency;

        for (int pass = 0; pass < octaves; pass++)
        {
            for (int y = 0; y < mapResolutionSize; y++)
            {
                for (int x = 0; x < mapResolutionSize; x++)
                {
                    if (biomeIndex >= 0 && biomeMap[x, y] != biomeIndex) continue;
                    float noise = Mathf.PerlinNoise(x * currentXFrequency, y * currentYFrequency) * 2f - 1f;
                    float newHeight = heightMap[y, x] + (noise * currentAmplitude / heightmapScale.y);
                    heightMap[y, x] = Mathf.Lerp(heightMap[y, x], newHeight, Strength);
                }
            }

            currentAmplitude *= persistence;
            currentXFrequency *= xLacunlarity;
            currentYFrequency *= yLacunlarity;
        }
    }
}
