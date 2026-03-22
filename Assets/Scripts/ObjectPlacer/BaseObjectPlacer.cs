using UnityEngine;

public readonly struct ObjectPlacerContext
{
    public readonly Terrain TargetTerrain;
    public readonly TerrainData TerrainData;
    public readonly int MapResolution;
    public readonly byte[,] BiomeMap;
    public readonly int BiomeIndex;
    public readonly Transform Parent;
    public readonly float MinTerrainHeight;
    public readonly float MaxTerrainHeight;
    public readonly float SeaLevelWorldY;

    public ObjectPlacerContext(
        Terrain targetTerrain,
        int mapResolution,
        byte[,] biomeMap,
        int biomeIndex,
        Transform parent,
        float minTerrainHeight = 0f,
        float maxTerrainHeight = 1f,
        float seaLevelWorldY = float.NegativeInfinity)
    {
        TargetTerrain = targetTerrain;
        TerrainData = targetTerrain != null ? targetTerrain.terrainData : null;
        MapResolution = mapResolution;
        BiomeMap = biomeMap;
        BiomeIndex = biomeIndex;
        Parent = parent;
        MinTerrainHeight = minTerrainHeight;
        MaxTerrainHeight = maxTerrainHeight;
        SeaLevelWorldY = seaLevelWorldY;
    }

    public ObjectPlacerContext WithBiome(byte[,] biomeMap, int biomeIndex, Transform parent)
    {
        return new ObjectPlacerContext(
            TargetTerrain,
            MapResolution,
            biomeMap,
            biomeIndex,
            parent,
            MinTerrainHeight,
            MaxTerrainHeight,
            SeaLevelWorldY);
    }

    public bool TargetsBiomeAtNormalized(float normalizedX, float normalizedY)
    {
        if (BiomeIndex < 0 || BiomeMap == null)
            return true;

        return BiomeMap[GetMapX(normalizedX), GetMapY(normalizedY)] == BiomeIndex;
    }

    public float GetNormalizedHeight(float normalizedX, float normalizedY)
    {
        float rawHeight = GetRawNormalizedHeight(normalizedX, normalizedY);
        float terrainHeightSpan = MaxTerrainHeight - MinTerrainHeight;
        if (terrainHeightSpan <= Mathf.Epsilon)
            return 0f;

        return Mathf.InverseLerp(MinTerrainHeight, MaxTerrainHeight, rawHeight);
    }

    public float GetSlope(float normalizedX, float normalizedY)
    {
        if (TerrainData == null)
            return 0f;

        return TerrainData.GetSteepness(Mathf.Clamp01(normalizedX), Mathf.Clamp01(normalizedY));
    }

    public Vector3 GetSurfaceNormal(float normalizedX, float normalizedY)
    {
        if (TerrainData == null)
            return Vector3.up;

        return TerrainData.GetInterpolatedNormal(Mathf.Clamp01(normalizedX), Mathf.Clamp01(normalizedY));
    }

    public Vector3 GetWorldPosition(float normalizedX, float normalizedY, float verticalOffset = 0f)
    {
        if (TargetTerrain == null || TerrainData == null)
            return Vector3.zero;

        float clampedX = Mathf.Clamp01(normalizedX);
        float clampedY = Mathf.Clamp01(normalizedY);
        Vector3 terrainPosition = TargetTerrain.transform.position;
        Vector3 terrainSize = TerrainData.size;
        float worldHeight = terrainPosition.y + TerrainData.GetInterpolatedHeight(clampedX, clampedY) + verticalOffset;
        return new Vector3(
            terrainPosition.x + (clampedX * terrainSize.x),
            worldHeight,
            terrainPosition.z + (clampedY * terrainSize.z));
    }

    public float GetSurfaceWorldHeight(float normalizedX, float normalizedY)
    {
        if (TargetTerrain == null || TerrainData == null)
            return 0f;

        return TargetTerrain.transform.position.y + TerrainData.GetInterpolatedHeight(
            Mathf.Clamp01(normalizedX),
            Mathf.Clamp01(normalizedY));
    }

    public bool IsBelowSeaLevel(float normalizedX, float normalizedY)
    {
        return GetSurfaceWorldHeight(normalizedX, normalizedY) < SeaLevelWorldY;
    }

    private int GetMapX(float normalizedX)
    {
        return MapNormalizedToSourceIndex(normalizedX, MapResolution);
    }

    private int GetMapY(float normalizedY)
    {
        return MapNormalizedToSourceIndex(normalizedY, MapResolution);
    }

    private float GetRawNormalizedHeight(float normalizedX, float normalizedY)
    {
        if (TerrainData == null || TerrainData.size.y <= Mathf.Epsilon)
            return 0f;

        return Mathf.Clamp01(TerrainData.GetInterpolatedHeight(Mathf.Clamp01(normalizedX), Mathf.Clamp01(normalizedY)) / TerrainData.size.y);
    }

    private static int MapNormalizedToSourceIndex(float normalizedValue, int resolution)
    {
        if (resolution <= 1)
            return 0;

        return Mathf.Clamp(
            Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * (resolution - 1)),
            0,
            resolution - 1);
    }
}

public class BaseObjectPlacer : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] protected float Strength = 1f;

    public virtual void Execute(in ObjectPlacerContext context)
    {
        Debug.LogError("No implementation of Execute function for " + gameObject.name);
    }
}
