using System;
using UnityEngine;

public class BaseHeightMapModifier : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] protected float Strength = 1f;

    public virtual void Execute(int mapResolutionSize, float[,] heightMap, Vector3 heightmapScale)
    {
        Execute(mapResolutionSize, heightMap, heightmapScale, null, null, -1);  
    }

    public virtual void Execute(int mapResolutionSize, float[,] heightMap, Vector3 heightmapScale, byte[,] biomeMap, BiomeConfigSO biomeConfig, int biomeIndex)
    {
        Debug.LogError("No implementation of Execution function for " + gameObject.name);
    }
}
