using System.Collections.Generic;
using UnityEngine;

public class RandomTexturePainter: BaseTexturePainter
{
    public override void Execute(int mapResolution, float[,] heightMap, Vector3 heightmapScale, float[,,] alphaMaps, int alphamapResolution, Dictionary<string, int> biomeTexture2TerrainLayerIndex, byte[,] biomeMap = null, BiomeConfigSO biome = null, int biomeIndex = -1)
    {
        if (biome == null)
        {
            Debug.LogWarning($"{nameof(RandomTexturePainter)}: Missing biome config, skipping paint.");
            return;
        }

        if (biome.Textures == null || biome.Textures.Count == 0)
        {
            Debug.LogWarning($"{nameof(RandomTexturePainter)}: Biome '{biome.Name}' has no textures, skipping paint.");
            return;
        }

        if (biomeIndex >= 0 && biomeMap == null)
        {
            Debug.LogWarning($"{nameof(RandomTexturePainter)}: Biome map is missing, skipping biome-filtered paint.");
            return;
        }

        for (int y = 0; y < alphamapResolution; ++y)
        {
            for (int x = 0; x < alphamapResolution; ++x)
            {
                if (biomeIndex >= 0 && biomeMap[x, y] != biomeIndex)
                    continue;

                string randomTextureID = biome.Textures[Random.Range(0, biome.Textures.Count)].UniqueID;
                if (!biomeTexture2TerrainLayerIndex.TryGetValue(randomTextureID, out int terrainLayer))
                    continue;

                alphaMaps[x, y, terrainLayer] = Strength;
            }
        }
    }
}
