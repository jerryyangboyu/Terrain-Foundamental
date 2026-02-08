using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BiomeOverlayVisualizer
{
    private TerrainLayer[] biomePreviewLayers;
    private int cachedBiomeCount = -1;
    private int cachedBiomeColorHash = 0;
#if UNITY_EDITOR
    private const string PreviewAssetsFolder = "Assets/Data/BiomePreview";
#endif

    public void RenderOnTerrain(Terrain targetTerrain, byte[,] resolutionMap, int resolutionSize, Color[] biomeColors)
    {
        int biomeCount = biomeColors != null ? biomeColors.Length : 0;
        if (targetTerrain == null || targetTerrain.terrainData == null || resolutionMap == null || biomeCount <= 0 || resolutionSize <= 0)
        {
            return;
        }

        TerrainData terrainData = targetTerrain.terrainData;
        EnsureBiomePreviewLayers(terrainData, biomeColors);

        int alphaResolution = terrainData.alphamapResolution;
        float[,,] alphamaps = new float[alphaResolution, alphaResolution, biomeCount];
        float alphaToSourceScale = (resolutionSize - 1f) / Mathf.Max(1f, alphaResolution - 1f);

        for (int y = 0; y < alphaResolution; y++)
        {
            int sourceY = Mathf.Min(Mathf.RoundToInt(y * alphaToSourceScale), resolutionSize - 1);
            for (int x = 0; x < alphaResolution; x++)
            {
                int sourceX = Mathf.Min(Mathf.RoundToInt(x * alphaToSourceScale), resolutionSize - 1);
                int biomeIndex = Mathf.Clamp(resolutionMap[sourceX, sourceY], 0, biomeCount - 1);
                alphamaps[y, x, biomeIndex] = 1f;
            }
        }

        terrainData.SetAlphamaps(0, 0, alphamaps);
    }

    public static Color GetDefaultBiomeColor(int biomeIndex, int biomeCount)
    {
        float hue = (float)biomeIndex / Mathf.Max(1, biomeCount);
        return Color.HSVToRGB(hue, .75f, .75f);
    }

    public static Color GetBiomeColor(int biomeIndex, int biomeCount, BiomeConfigSO biomeConfig)
    {
        if (biomeConfig != null && biomeConfig.UseCustomPreviewColor)
        {
            return biomeConfig.PreviewColor;
        }

        return GetDefaultBiomeColor(biomeIndex, biomeCount);
    }

    private void EnsureBiomePreviewLayers(TerrainData terrainData, Color[] biomeColors)
    {
        int biomeCount = biomeColors.Length;
        int biomeColorHash = ComputeBiomeColorHash(biomeColors);
        if (biomePreviewLayers != null && cachedBiomeCount == biomeCount && cachedBiomeColorHash == biomeColorHash)
        {
            terrainData.terrainLayers = biomePreviewLayers;
            return;
        }

        biomePreviewLayers = new TerrainLayer[biomeCount];
        cachedBiomeCount = biomeCount;
        cachedBiomeColorHash = biomeColorHash;

#if UNITY_EDITOR
        EnsurePreviewAssetFolderExists();

        for (int biomeIndex = 0; biomeIndex < biomeCount; biomeIndex++)
        {
            Color biomeColor = biomeColors[biomeIndex];

            Texture2D texture = LoadOrCreatePreviewTexture(biomeIndex, biomeColor);
            TerrainLayer terrainLayer = LoadOrCreatePreviewLayer(biomeIndex);
            terrainLayer.diffuseTexture = texture;
            terrainLayer.tileSize = new Vector2(1f, 1f);
            EditorUtility.SetDirty(terrainLayer);

            biomePreviewLayers[biomeIndex] = terrainLayer;
        }

        terrainData.terrainLayers = biomePreviewLayers;
        EditorUtility.SetDirty(terrainData);
        AssetDatabase.SaveAssets();
#endif
    }

    private static int ComputeBiomeColorHash(Color[] biomeColors)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < biomeColors.Length; i++)
            {
                Color32 color = biomeColors[i];
                hash = hash * 31 + color.r;
                hash = hash * 31 + color.g;
                hash = hash * 31 + color.b;
                hash = hash * 31 + color.a;
            }
            return hash;
        }
    }

#if UNITY_EDITOR
    private static void EnsurePreviewAssetFolderExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder(PreviewAssetsFolder))
        {
            AssetDatabase.CreateFolder("Assets/Data", "BiomePreview");
        }
    }

    private static Texture2D LoadOrCreatePreviewTexture(int biomeIndex, Color biomeColor)
    {
        string texturePath = $"{PreviewAssetsFolder}/BiomePreviewTex_{biomeIndex}.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture != null && (texture.width < 2 || texture.height < 2 || texture.mipmapCount <= 1))
        {
            AssetDatabase.DeleteAsset(texturePath);
            texture = null;
        }

        if (texture == null)
        {
            texture = new Texture2D(16, 16, TextureFormat.RGBA32, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            AssetDatabase.CreateAsset(texture, texturePath);
        }

        Color[] pixels = new Color[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = biomeColor;
        }
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static TerrainLayer LoadOrCreatePreviewLayer(int biomeIndex)
    {
        string layerPath = $"{PreviewAssetsFolder}/BiomePreviewLayer_{biomeIndex}.terrainlayer";
        TerrainLayer terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (terrainLayer == null)
        {
            terrainLayer = new TerrainLayer();
            terrainLayer.name = $"BiomePreview_{biomeIndex}";
            AssetDatabase.CreateAsset(terrainLayer, layerPath);
        }

        return terrainLayer;
    }
#endif
}
