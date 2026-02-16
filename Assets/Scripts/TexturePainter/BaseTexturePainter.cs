using System.Collections.Generic;
using UnityEngine;

public class BaseTexturePainter: MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] protected float Strength = 1f;

    public virtual void Execute(int mapResolution, float[,] heightMap, Vector3 heightmapScale, float[,,] alphaMaps, int alphamapResolution, Dictionary<string, int> biomeTexture2TerrainLayerIndex, byte[,] biomeMap = null, BiomeConfigSO biome = null, int biomeIndex = -1)
    {
        
    }
}
