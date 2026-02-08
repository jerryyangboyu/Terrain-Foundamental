using System;
using System.IO;
using UnityEngine;

public class BiomeMapCacheStore
{
    const int CacheVersion = 1;
    const string CacheDirectoryName = "ProcGenCache";
    const string CacheFileName = "biome_cache.bin";
    private static BiomeMapCacheStore instance;

    public static BiomeMapCacheStore Instance
    {
        get
        {
            instance ??= new BiomeMapCacheStore();
            return instance;
        }
    }

    private BiomeMapCacheStore()
    {
    }

    public void Save(byte[,] biomeMap, int mapResolutionSize)
    {
        if (!IsValidBiomeMap(biomeMap, mapResolutionSize))
        {
            return;
        }

        try
        {
            string path = GetCachePath();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream);
            writer.Write(CacheVersion);
            writer.Write(mapResolutionSize);
            for (int y = 0; y < mapResolutionSize; y++)
            {
                for (int x = 0; x < mapResolutionSize; x++)
                {
                    writer.Write(biomeMap[x, y]);
                }
            }

            Debug.Log("Biome map saved to: " + path);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to persist biome cache: {e.Message}");
        }
    }

    public bool TryLoad(int expectedResolutionSize, out byte[,] biomeMap)
    {
        biomeMap = null;

        try
        {
            string path = GetCachePath();
            if (!File.Exists(path))
            {
                return false;
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);
            int version = reader.ReadInt32();
            if (version != CacheVersion)
            {
                return false;
            }

            int storedResolutionSize = reader.ReadInt32();
            if (storedResolutionSize != expectedResolutionSize)
            {
                return false;
            }

            byte[,] restoredBiomeMap = new byte[storedResolutionSize, storedResolutionSize];
            for (int y = 0; y < storedResolutionSize; y++)
            {
                for (int x = 0; x < storedResolutionSize; x++)
                {
                    restoredBiomeMap[x, y] = reader.ReadByte();
                }
            }

            biomeMap = restoredBiomeMap;
            Debug.Log("Biome map loaded from: " + path);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to restore biome cache: {e.Message}");
            return false;
        }
    }

    private bool IsValidBiomeMap(byte[,] biomeMap, int mapResolutionSize)
    {
        return biomeMap != null
               && biomeMap.GetLength(0) == mapResolutionSize
               && biomeMap.GetLength(1) == mapResolutionSize;
    }

    private string GetCachePath()
    {
        return Path.Combine(Application.persistentDataPath, CacheDirectoryName, CacheFileName);
    }
}
