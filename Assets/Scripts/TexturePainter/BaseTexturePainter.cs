using System.Collections.Generic;
using UnityEngine;

public readonly struct TexturePainterContext
{
    public readonly IReadOnlyDictionary<string, int> TextureLayerIndices;
    public readonly int MapResolution;
    public readonly float[,] HeightMap;
    public readonly float[,] SlopeMap;
    public readonly float[,,] AlphaMaps;
    public readonly int AlphaMapResolution;
    public readonly byte[,] BiomeMap;
    public readonly int BiomeIndex;
    public readonly float MinTerrainHeight;
    public readonly float MaxTerrainHeight;

    public TexturePainterContext(
        IReadOnlyDictionary<string, int> textureLayerIndices,
        int mapResolution,
        float[,] heightMap,
        float[,] slopeMap,
        float[,,] alphaMaps,
        int alphaMapResolution,
        byte[,] biomeMap = null,
        int biomeIndex = -1,
        float minTerrainHeight = 0f,
        float maxTerrainHeight = 1f)
    {
        TextureLayerIndices = textureLayerIndices;
        MapResolution = mapResolution;
        HeightMap = heightMap;
        SlopeMap = slopeMap;
        AlphaMaps = alphaMaps;
        AlphaMapResolution = alphaMapResolution;
        BiomeMap = biomeMap;
        BiomeIndex = biomeIndex;
        MinTerrainHeight = minTerrainHeight;
        MaxTerrainHeight = maxTerrainHeight;
    }

    public TexturePainterContext WithBiome(byte[,] biomeMap, int biomeIndex)
    {
        return new TexturePainterContext(
            TextureLayerIndices,
            MapResolution,
            HeightMap,
            SlopeMap,
            AlphaMaps,
            AlphaMapResolution,
            biomeMap,
            biomeIndex,
            MinTerrainHeight,
            MaxTerrainHeight);
    }

    public int GetLayerForTexture(string textureId)
    {
        return TextureLayerIndices[textureId];
    }

    // Unity terrain alphamaps are indexed as [y, x, layer].
    public float GetAlpha(int alphaMapX, int alphaMapY, int layerIndex)
    {
        return AlphaMaps[alphaMapY, alphaMapX, layerIndex];
    }

    public void SetAlpha(int alphaMapX, int alphaMapY, int layerIndex, float value)
    {
        AlphaMaps[alphaMapY, alphaMapX, layerIndex] = value;
    }

    public int GetMapXForAlpha(int alphaMapX)
    {
        return MapAlphaToSourceIndex(alphaMapX, MapResolution, AlphaMapResolution);
    }

    public int GetMapYForAlpha(int alphaMapY)
    {
        return MapAlphaToSourceIndex(alphaMapY, MapResolution, AlphaMapResolution);
    }

    public bool TargetsBiome(int heightMapX, int heightMapY)
    {
        return BiomeIndex < 0 || BiomeMap[heightMapX, heightMapY] == BiomeIndex;
    }

    public bool TargetsBiomeAtAlpha(int alphaMapX, int alphaMapY)
    {
        return TargetsBiome(GetMapXForAlpha(alphaMapX), GetMapYForAlpha(alphaMapY));
    }

    public float GetHeight(int heightMapX, int heightMapY)
    {
        return HeightMap[heightMapY, heightMapX];
    }

    public float GetNormalizedHeight(int heightMapX, int heightMapY)
    {
        float terrainHeightSpan = MaxTerrainHeight - MinTerrainHeight;
        if (terrainHeightSpan <= Mathf.Epsilon)
            return 0f;

        return Mathf.InverseLerp(MinTerrainHeight, MaxTerrainHeight, GetHeight(heightMapX, heightMapY));
    }

    public float GetNormalizedHeightAtAlpha(int alphaMapX, int alphaMapY)
    {
        return GetNormalizedHeight(GetMapXForAlpha(alphaMapX), GetMapYForAlpha(alphaMapY));
    }

    private static int MapAlphaToSourceIndex(int alphaIndex, int sourceResolution, int alphaResolution)
    {
        if (sourceResolution <= 1 || alphaResolution <= 1)
            return 0;

        float scale = (sourceResolution - 1f) / Mathf.Max(1f, alphaResolution - 1f);
        return Mathf.Clamp(Mathf.RoundToInt(alphaIndex * scale), 0, sourceResolution - 1);
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
