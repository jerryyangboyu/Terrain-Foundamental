using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct TexturePainterContext
{
    public readonly IReadOnlyDictionary<string, int> TextureLayerIndices;
    public readonly int MapResolution;
    public readonly float[,] HeightMap;
    public readonly Vector3 HeightmapScale;
    public readonly float[,] SlopeMap;
    public readonly float[,,] AlphaMaps;
    public readonly int AlphaMapResolution;
    public readonly byte[,] BiomeMap;
    public readonly int BiomeIndex;
    public readonly BiomeConfigSO Biome;
    public readonly float MinTerrainHeight;
    public readonly float MaxTerrainHeight;

    public TexturePainterContext(
        IReadOnlyDictionary<string, int> textureLayerIndices,
        int mapResolution,
        float[,] heightMap,
        Vector3 heightmapScale,
        float[,] slopeMap,
        float[,,] alphaMaps,
        int alphaMapResolution,
        byte[,] biomeMap = null,
        int biomeIndex = -1,
        BiomeConfigSO biome = null,
        float minTerrainHeight = 0f,
        float maxTerrainHeight = 1f)
    {
        TextureLayerIndices = textureLayerIndices;
        MapResolution = mapResolution;
        HeightMap = heightMap;
        HeightmapScale = heightmapScale;
        SlopeMap = slopeMap;
        AlphaMaps = alphaMaps;
        AlphaMapResolution = alphaMapResolution;
        BiomeMap = biomeMap;
        BiomeIndex = biomeIndex;
        Biome = biome;
        MinTerrainHeight = minTerrainHeight;
        MaxTerrainHeight = maxTerrainHeight;
    }

    public TexturePainterContext WithBiome(byte[,] biomeMap, int biomeIndex, BiomeConfigSO biome)
    {
        return new TexturePainterContext(
            TextureLayerIndices,
            MapResolution,
            HeightMap,
            HeightmapScale,
            SlopeMap,
            AlphaMaps,
            AlphaMapResolution,
            biomeMap,
            biomeIndex,
            biome,
            MinTerrainHeight,
            MaxTerrainHeight);
    }

    public int GetLayerForTexture(string textureId)
    {
        return TextureLayerIndices[textureId];
    }
}

public class BaseTexturePainter : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] protected float Strength = 1f;

    public virtual void Execute(in TexturePainterContext context)
    {
        Debug.LogError("No implementation of Execute function for " + gameObject.name);
    }
}
