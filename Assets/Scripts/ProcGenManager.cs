using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ProcGenManager : MonoBehaviour
{
    [SerializeField] ProcGenConfigSO Config;
    [SerializeField] Terrain TargetTerrain;

    private static readonly Vector2Int[] NeighbourOffsets = new Vector2Int[]
    {
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
        new(1, 1),
        new(-1, 1),
        new(1, -1),
        new(-1, -1)
    };

#if UNITY_EDITOR
    byte[,] BiomeMap;
    float[,] BiomeStrengths;
    Texture2D BiomeMapPreview;
#endif

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

#if UNITY_EDITOR
    public void RegenerateWorld()
    {
        if (Config == null || TargetTerrain == null)
        {
            Debug.LogError("Missing config or target terrain.");
            return;
        }

        int mapResolution = TargetTerrain.terrainData.heightmapResolution;

        PerformBiomGeneration(mapResolution);

    }

    private void PerformBiomGeneration(int mapResolution)
    {
        if (Config.Biomes == null || Config.Biomes.Count == 0)
        {
            Debug.LogError("No biomes configured.");
            return;
        }

        if (Config.BiomeWeights == null || Config.BiomeWeights.Length < Config.Biomes.Count)
        {
            Debug.LogError("BiomeWeights count must match Biomes count.");
            return;
        }

        BiomeMap = new byte[mapResolution, mapResolution];
        BiomeStrengths = new float[mapResolution, mapResolution];

        int numSeedPoints = Mathf.FloorToInt(mapResolution * mapResolution * Config.BiomeSeedPointDensity);
        List<byte> biomesToSpawn = new(numSeedPoints);

        float[] biomesWeights = Config.BiomeWeights;
        for (int biomeIndex = 0; biomeIndex < Config.Biomes.Count; ++biomeIndex)
        {
            int numEntries = Mathf.RoundToInt(numSeedPoints * biomesWeights[biomeIndex]);
            Debug.Log("Will spawn " + numEntries + " seepoints for " + Config.Biomes[biomeIndex].Biome.Name);

            for (int entryIndex = 0; entryIndex < numEntries; ++entryIndex)
            {
                biomesToSpawn.Add((byte)biomeIndex);
            }
        }

        for (int upperBoundary = biomesToSpawn.Count; upperBoundary > 0; --upperBoundary)
        {
            int seedPointIndex = Random.Range(0, upperBoundary);
            int lastIndex = upperBoundary - 1;
            byte biomeIndex = biomesToSpawn[seedPointIndex];

            biomesToSpawn[seedPointIndex] = biomesToSpawn[lastIndex];

            PerformSpwanIndividualBiome(biomeIndex, mapResolution);
        }

        Texture2D biomeMap = new(mapResolution, mapResolution, TextureFormat.RGB24, false);
        for (int y = 0; y < mapResolution; y++)
        {
            for (int x = 0; x < mapResolution; x++)
            {
                float hue = (float) BiomeMap[x, y] / (float) Config.Biomes.Count;
                biomeMap.SetPixel(x, y, Color.HSVToRGB(hue, .75f, .75f));
            }
        }
        biomeMap.Apply();

        System.IO.File.WriteAllBytes("BiomeMap.png", biomeMap.EncodeToPNG());
    }

    private void PerformSpwanIndividualBiome(byte biomeIndex, int mapResolution)
    {
        // biome config
        BiomeConfigSO biomeConfig = Config.Biomes[biomeIndex].Biome;

        // random starting location
        Vector2Int spwanLocation = new(Random.Range(0, mapResolution), Random.Range(0, mapResolution));
        
        // start intensity
        float startIntensity = Random.Range(biomeConfig.MinIntensity, biomeConfig.MaxIntensity);

        // working list
        Queue<Vector2Int> workingList = new();
        workingList.Enqueue(spwanLocation);

        // visit map
        bool[,] visited = new bool[mapResolution, mapResolution];

        // spread intensity
        float[,] targetIntensity = new float[mapResolution, mapResolution];
        targetIntensity[spwanLocation.x, spwanLocation.y] = startIntensity;

        while (workingList.Count > 0)
        {
            Vector2Int workingLocation = workingList.Dequeue();

            visited[workingLocation.x, workingLocation.y] = true;
            BiomeMap[workingLocation.x, workingLocation.y] = biomeIndex;
            BiomeStrengths[workingLocation.x, workingLocation.y] = targetIntensity[workingLocation.x, workingLocation.y];

            // traverse the neighbours
            for (int neighbourIndex = 0; neighbourIndex < NeighbourOffsets.Length; ++neighbourIndex)
            {
                var neighbourLocation = workingLocation + NeighbourOffsets[neighbourIndex];
                if (!CheckBoundaryValidity(neighbourLocation, mapResolution))
                    continue;

                if (visited[neighbourLocation.x, neighbourLocation.y])
                    continue;

                // allow our biomes to be a little bit patchy
                visited[neighbourLocation.x, neighbourLocation.y] = true;

                float decayAmount = Random.Range(biomeConfig.MinDecayRate, biomeConfig.MaxDecayRate) * NeighbourOffsets[neighbourIndex].magnitude;
                float neighbourStrength = targetIntensity[workingLocation.x, workingLocation.y] - decayAmount;
                targetIntensity[neighbourLocation.x, neighbourLocation.y] = neighbourStrength;
                if (neighbourStrength <= 0) continue;

                workingList.Enqueue(neighbourLocation);
                
            }
        }

    }

    private bool CheckBoundaryValidity(Vector2Int location, int mapResolution)
    {
        return location.x >= 0 && location.y >= 0 &&
               location.x < mapResolution && location.y < mapResolution;
    }
#endif
}
