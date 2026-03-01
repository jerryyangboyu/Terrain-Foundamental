using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BiomeConfig
{
    public BiomeConfigSO Biome;
    [Range(0f, 1f)] public float Weighting = 1f;
}

[CreateAssetMenu(fileName = "ProcGen Config", menuName = "Procedural Generation/ProcGen Configuration", order = -1)]
public class ProcGenConfigSO : ScriptableObject
{
    public List<BiomeConfig> Biomes;

    public enum BiomeMapBaseResolution
    {
        Size_64x64 = 64,
        Size_256x256 = 256,
        Size_512x512 = 512
    }
    public BiomeMapBaseResolution biomeMapResolution = BiomeMapBaseResolution.Size_64x64;
    [Range(0f, 1f)] public float BiomeSeedPointDensity = 0.1f;

    public GameObject InitialHeightModifier;
    public GameObject HeightPostProcessingModifier;
    public GameObject PaintingPostProcessingModifier;

    public float[] BiomeWeights
    {
        get
        {
            if (Biomes == null || Biomes.Count == 0)
            {
                return Array.Empty<float>();
            }

            float sum = 0f;
            for (int i = 0; i < Biomes.Count; i++)
            {
                sum += Mathf.Max(0f, Biomes[i].Weighting);
            }

            float[] weights = new float[Biomes.Count];
            if (sum <= 0f)
            {
                float even = 1f / Biomes.Count;
                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] = even;
                }
                return weights;
            }

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = Mathf.Max(0f, Biomes[i].Weighting) / sum;
            }

            return weights;
        }
    }
    
}
