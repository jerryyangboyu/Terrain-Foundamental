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
        // TODO: implement perlin noise based terrain generation algorithm
    }
}
