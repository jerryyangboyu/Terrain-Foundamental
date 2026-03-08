using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class ProcGenManager : MonoBehaviour
{
    [SerializeField] ProcGenConfigSO Config;
    [SerializeField] Terrain TargetTerrain;
    [SerializeField] bool OutputBiomePngFiles = false;
    [SerializeField] bool UseSimpleBiomeVisualization = true;
    [SerializeField] bool RegenerateBiome = true;
    [SerializeField] bool RegenerateLayers = true;

    readonly Dictionary<string, int> BiomeTexture2TerrainLayerIndex = new();

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
        int mapResolution = TargetTerrain.terrainData.heightmapResolution;
        int alphamapResolution = TargetTerrain.terrainData.alphamapResolution;
        Color[] biomeColors = BuildBiomeColors();

        // Generate biome sections
        PerformBiomeGeneration(baseMapResolution, mapResolution);

        // Optional save biome map to disk
        if (OutputBiomePngFiles)
        {
            OutputPngFile("BaseTerrainMap", BiomeMapLowResolution, baseMapResolution, biomeColors);
            OutputPngFile("UpScaleTerrainMap", BiomeMap, mapResolution, biomeColors);
        }

        // Texture painting
        if (UseSimpleBiomeVisualization)
        {
            // Simple visualization by assigning a unique color to each biome
            BiomeOverlayVisualizer.Instance.RenderOnTerrain(TargetTerrain, BiomeMap, mapResolution, biomeColors);
        }
        else if (RegenerateLayers)
        {
            PerformLayerSetup();
        }

        // Generate heightmap
        PerformHeightMapModification(mapResolution);

        // Paint the terrain
        PerformTerrainPainting(mapResolution, alphamapResolution);
    }

    private void PerformTerrainPainting(int mapResolution, int alphaMapResolution)
    {
        float[,] heightMap = TargetTerrain.terrainData.GetHeights(0, 0, mapResolution, mapResolution);
        float[,,] alphaMaps = TargetTerrain.terrainData.GetAlphamaps(0, 0, alphaMapResolution, alphaMapResolution);
        GetHeightRange(heightMap, out float minTerrainHeight, out float maxTerrainHeight);
        float[,] slopeMap = new float[alphaMapResolution, alphaMapResolution];

        for (int y = 0; y < alphaMapResolution; ++y)
        {
            for (int x = 0; x < alphaMapResolution; ++x)
            {
                float interpolatedX = (float)x / alphaMapResolution;
                float interpolatedY = (float)y / alphaMapResolution;
                slopeMap[y, x] = TargetTerrain.terrainData.GetSteepness(interpolatedX, interpolatedY);

                // zero out layer settings
                for (int layerIndex = 0; layerIndex < TargetTerrain.terrainData.alphamapLayers; layerIndex++)
                {
                    alphaMaps[y, x, layerIndex] = 0;
                }
            }
        }

        TexturePainterContext baseContext = new(
            BiomeTexture2TerrainLayerIndex,
            mapResolution,
            heightMap,
            slopeMap,
            alphaMaps,
            alphaMapResolution,
            minTerrainHeight: minTerrainHeight,
            maxTerrainHeight: maxTerrainHeight);

        if (Config.InitialPaintingModifier != null)
        {
            BaseTexturePainter[] modifiers = Config.InitialPaintingModifier.GetComponents<BaseTexturePainter>();

            foreach (var modifier in modifiers)
            {
                modifier.Execute(baseContext);
            }
        }

        for (int biomeIndex = 0; biomeIndex < Config.Biomes.Count; ++biomeIndex)
        {
            var biomeConfig = Config.Biomes[biomeIndex].Biome;
            if (biomeConfig.TerrainPainter == null) continue;

            TexturePainterContext biomeContext = baseContext.WithBiome(BiomeMap, biomeIndex);
            var modifiers = biomeConfig.TerrainPainter.GetComponents<BaseTexturePainter>();
            foreach(var modifier in modifiers)
            {
                modifier.Execute(biomeContext);
            }
        }

        // run texture post processing
        if (Config.PaintingPostProcessingModifier != null)
        {
            BaseTexturePainter[] modifiers = Config.PaintingPostProcessingModifier.GetComponents<BaseTexturePainter>();

            foreach(var modifier in modifiers)
            {
                modifier.Execute(baseContext);
            }    
        }

        TargetTerrain.terrainData.SetAlphamaps(0, 0, alphaMaps);
    }

    private static void GetHeightRange(float[,] heightMap, out float minHeight, out float maxHeight)
    {
        minHeight = float.MaxValue;
        maxHeight = float.MinValue;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                float sample = heightMap[x, y];
                if (sample < minHeight)
                    minHeight = sample;

                if (sample > maxHeight)
                    maxHeight = sample;
            }
        }

        if (minHeight == float.MaxValue)
        {
            minHeight = 0f;
            maxHeight = 1f;
        }
    }

    private void PerformLayerSetup()
    {
        

        // delete any existing layers
        if (TargetTerrain.terrainData.terrainLayers != null || TargetTerrain.terrainData.terrainLayers.Length > 0)
        {
            Undo.RecordObject(TargetTerrain, "Clearing previous layers");
            List<string> layersToDelete = new();
            foreach (var layer in TargetTerrain.terrainData.terrainLayers)
            {
                if (layer == null) 
                    continue;

                layersToDelete.Add(AssetDatabase.GetAssetPath(layer));
            }

            TargetTerrain.terrainData.terrainLayers = null;

            foreach (var layerFile in layersToDelete)
            {
                if (!string.IsNullOrEmpty(layerFile))
                {
                    AssetDatabase.DeleteAsset(layerFile);
                }
            }

            Undo.FlushUndoRecordObjects();
        }

        string scenePath = System.IO.Path.GetDirectoryName(SceneManager.GetActiveScene().path);

        List<TerrainLayer> newLayers = new();
        foreach (var biomeMetaData in Config.Biomes)
        {
            var biome = biomeMetaData.Biome;
            foreach (var biomeTexture in biome.Textures)
            {
                TerrainLayer textureLayer = new()
                {
                    diffuseTexture = biomeTexture.Diffuse,
                    normalMapTexture = biomeTexture.NormalMap
                };

                // save to assets
                string layerPath = System.IO.Path.Combine(scenePath, "Layer_" + biome.Name + "_" + biomeTexture.UniqueID + ".terrainlayer");
                AssetDatabase.CreateAsset(textureLayer, layerPath);

                // store mapping to layer index
                BiomeTexture2TerrainLayerIndex[biomeTexture.UniqueID] = newLayers.Count;
                newLayers.Add(textureLayer);
            }
        }

        Undo.RecordObject(TargetTerrain.terrainData, "Updating terrain layers");
        TargetTerrain.terrainData.terrainLayers = newLayers.ToArray();
    }

    private void PerformBiomeGeneration(int baseMapResolution, int mapResolutionSize)
    {
        if (RegenerateBiome)
        {
            RegenerateBiomeMap(baseMapResolution, mapResolutionSize);
        }
        else if (BiomeMap == null)
        {
            if (BiomeMapCacheStore.Instance.TryLoad(mapResolutionSize, out byte[,] restoredBiomeMap))
            {
                BiomeMap = restoredBiomeMap;
                BiomeStrengths = new float[mapResolutionSize, mapResolutionSize];
            }
            else
            {
                Debug.LogWarning("Biome map cache is missing or outdated. Regenerating.");
                RegenerateBiomeMap(baseMapResolution, mapResolutionSize);
            }
        }
    }

    private void RegenerateBiomeMap(int baseMapResolution, int mapResolutionSize)
    {
        // base map generation
        PerformBiomGenerationLowResoluion(baseMapResolution);
        // upscale
        PerformUpscaleBiomeMap(baseMapResolution, mapResolutionSize);
        // save to disk
        BiomeMapCacheStore.Instance.Save(BiomeMap, mapResolutionSize);
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
                modifier.Execute(mapResolutionSize, heightMap, TargetTerrain.terrainData.heightmapScale, BiomeMap, null, -1);
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

    private void OutputPngFile(string fileName, byte[,] resolutonMap, int resolutionSize, Color[] biomeColors)
    {
        Texture2D biomeMap = BuildBiomeTexture(resolutonMap, resolutionSize, biomeColors);
        System.IO.Directory.CreateDirectory("Images");
        System.IO.File.WriteAllBytes($"Images/{fileName}.png", biomeMap.EncodeToPNG());
        DestroyImmediate(biomeMap);
    }

    private Texture2D BuildBiomeTexture(byte[,] resolutionMap, int resolutionSize, Color[] biomeColors)
    {
        Texture2D biomeMap = new(resolutionSize, resolutionSize, TextureFormat.RGB24, false);
        biomeMap.filterMode = FilterMode.Point;
        biomeMap.wrapMode = TextureWrapMode.Clamp;
        bool hasBiomeColors = biomeColors != null && biomeColors.Length > 0;
        for (int y = 0; y < resolutionSize; y++)
        {
            for (int x = 0; x < resolutionSize; x++)
            {
                if (!hasBiomeColors)
                {
                    biomeMap.SetPixel(x, y, Color.black);
                    continue;
                }

                int biomeIndex = Mathf.Clamp(resolutionMap[x, y], 0, biomeColors.Length - 1);
                biomeMap.SetPixel(x, y, biomeColors[biomeIndex]);
            }
        }
        biomeMap.Apply();
        return biomeMap;
    }

    private Color[] BuildBiomeColors()
    {
        int biomeCount = Config.Biomes.Count;
        Color[] biomeColors = new Color[biomeCount];

        for (int biomeIndex = 0; biomeIndex < biomeCount; biomeIndex++)
        {
            var biome = Config.Biomes[biomeIndex].Biome;
            biomeColors[biomeIndex] = BiomeOverlayVisualizer.GetBiomeColor(biomeIndex, biomeCount, biome);
        }

        return biomeColors;
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
