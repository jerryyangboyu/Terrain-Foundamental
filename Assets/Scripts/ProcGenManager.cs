using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ProcGenManager : MonoBehaviour
{
    [SerializeField] ProcGenConfigSO Config;
    [SerializeField] Terrain TargetTerrain;
    [SerializeField] bool OutputBiomePngFiles = false;
    [SerializeField] bool ShowBiomeOverlayInScene = true;
    [SerializeField, Min(0f)] float BiomeOverlayHeightOffset = 5f;

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
    // base map
    byte[,] BiomeMapLowResolution;
    float[,] BiomeStrengthsLowResolution;

    // upscaled map
    byte[,] BiomeMap;
    float[,] BiomeStrengths;
    BiomeOverlayVisualizer BiomeOverlayVisualizer;
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
        int baseMapResolution = (int) Config.biomeMapResolution;
        int mapResolutionSize = TargetTerrain.terrainData.heightmapResolution;

        // base map generation
        PerformBiomGenerationLowResoluion(baseMapResolution);
        if (OutputBiomePngFiles)
        {
            OutputPngFile("BaseTerrainMap", BiomeMapLowResolution, baseMapResolution);
        }

        // upscale
        PerformUpscaleBiomeMap(baseMapResolution, mapResolutionSize);
        if (OutputBiomePngFiles)
        {
            OutputPngFile("UpScaleTerrainMap", BiomeMap, mapResolutionSize);
        }

        if (ShowBiomeOverlayInScene)
        {
            BiomeOverlayVisualizer ??= new BiomeOverlayVisualizer();
            Texture2D overlayTexture = BuildBiomeTexture(BiomeMap, mapResolutionSize);
            BiomeOverlayVisualizer.Render(transform, TargetTerrain, overlayTexture, BiomeOverlayHeightOffset);
        }
        else
        {
            BiomeOverlayVisualizer?.SetVisible(false);
        }

        PerformHeightMapModification(mapResolutionSize);
    }

    private void PerformHeightMapModification(int mapResolutionSize)
    {
        var heightMap = TargetTerrain.terrainData.GetHeights(0, 0, mapResolutionSize, mapResolutionSize);

        if (Config.InitialHeightModifier != null)
        {
            var modifiers = Config.InitialHeightModifier.GetComponents<BaseHeightMapModifier>();
            foreach (var modifier in modifiers)
            {
                modifier.Execute(mapResolutionSize, heightMap, TargetTerrain.terrainData.heightmapScale);
            }
        }

        for (int biomeIndex = 0; biomeIndex < Config.Biomes.Count; ++biomeIndex)
        {
            var biomeConfig = Config.Biomes[biomeIndex].Biome;
            if (biomeConfig.HeightModifier == null) continue;

            var modifiers = biomeConfig.HeightModifier.GetComponents<BaseHeightMapModifier>();
            foreach (var modifier in modifiers)
            {
                modifier.Execute(mapResolutionSize, heightMap, TargetTerrain.terrainData.heightmapScale, BiomeMap, biomeConfig, biomeIndex);
            }
        }

        if (Config.HeightPostProcessingModifier != null)
        {
            var modifers = Config.HeightPostProcessingModifier.GetComponents<BaseHeightMapModifier>();
            foreach (var modifier in modifers)
            {
                modifier.Execute(mapResolutionSize, heightMap, TargetTerrain.terrainData.heightmapScale);
            }
        }

        TargetTerrain.terrainData.SetHeights(0, 0, heightMap);
    }

    private void PerformUpscaleBiomeMap(int lowResolution, int highResolution)
    {
        BiomeMap = new byte[highResolution, highResolution];
        BiomeStrengths = new float[highResolution, highResolution];

        float mapScale = (float) lowResolution / highResolution;

        for (int y = 0; y < highResolution; y++)
        {
            float scaledY = y * mapScale;
            int yAtLowRes = Mathf.FloorToInt(scaledY);
            float yFraction = scaledY - yAtLowRes;

            for (int x = 0; x < highResolution; x++)
            {
                float scaledX = x * mapScale;
                int xAtLowRes = Mathf.FloorToInt(scaledX);
                float xFraction = scaledX - xAtLowRes;

                // simple upscale
                // BiomeMap[x, y] = BiomeMapLowResolution[xAtLowRes, yAtLowRes];

                // bi-linear interpolation
                BiomeMap[x, y] = CalculateHighResBiomeIndex(lowResolution, xAtLowRes, yAtLowRes, xFraction, yFraction);
            }
        }
    }

    private byte CalculateHighResBiomeIndex(int lowResMapSize, int lowX, int lowY, float fractionX, float fractionY)
    {
        float bottomLeft = BiomeMapLowResolution[lowX, lowY];
        float upperLeft = (lowY + 1 < lowResMapSize) ? BiomeMapLowResolution[lowX, lowY + 1] : bottomLeft;
        float bottomRight = (lowX + 1 < lowResMapSize) ? BiomeMapLowResolution[lowX + 1, lowY] : bottomLeft;

        float upperRight;
        if (lowX + 1 >= lowResMapSize) upperRight = upperLeft;
        else if (lowY + 1 >= lowResMapSize) upperRight = bottomRight;
        else upperRight = BiomeMapLowResolution[lowX + 1, lowY + 1];

        float interpolatedIndex =   bottomLeft * (1 - fractionX) * (1 - fractionY)
                      + upperLeft * (1 - fractionX)* fractionY
                      + bottomRight * fractionX * (1 - fractionY)
                      + upperRight * fractionX * fractionY;

        float[] candidateBiomes = new float[] { bottomLeft, bottomRight, upperLeft, upperRight };
        float bestBiome = candidateBiomes[0];
        float biomeDelta = float.MaxValue;
        foreach (var candidateBiome in candidateBiomes)
        {
            var delta = Mathf.Abs(interpolatedIndex - candidateBiome);
            if (delta < biomeDelta)
            {
                biomeDelta = delta;
                bestBiome = candidateBiome;
            }
        }


        return (byte) bestBiome;
    }

    private void PerformBiomGenerationLowResoluion(int mapResolution)
    {
        BiomeMapLowResolution = new byte[mapResolution, mapResolution];
        BiomeStrengthsLowResolution = new float[mapResolution, mapResolution];

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
    }

    private void OutputPngFile(string fileName, byte[,] resolutonMap, int resolutionSize)
    {
        Texture2D biomeMap = BuildBiomeTexture(resolutonMap, resolutionSize);
        System.IO.Directory.CreateDirectory("Images");
        System.IO.File.WriteAllBytes($"Images/{fileName}.png", biomeMap.EncodeToPNG());
        DestroyImmediate(biomeMap);
    }

    private Texture2D BuildBiomeTexture(byte[,] resolutionMap, int resolutionSize)
    {
        Texture2D biomeMap = new(resolutionSize, resolutionSize, TextureFormat.RGB24, false);
        biomeMap.filterMode = FilterMode.Point;
        biomeMap.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < resolutionSize; y++)
        {
            for (int x = 0; x < resolutionSize; x++)
            {
                float hue = (float) resolutionMap[x, y] / (float) Config.Biomes.Count;
                biomeMap.SetPixel(x, y, Color.HSVToRGB(hue, .75f, .75f));
            }
        }
        biomeMap.Apply();
        return biomeMap;
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
            BiomeMapLowResolution[workingLocation.x, workingLocation.y] = biomeIndex;
            BiomeStrengthsLowResolution[workingLocation.x, workingLocation.y] = targetIntensity[workingLocation.x, workingLocation.y];

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
