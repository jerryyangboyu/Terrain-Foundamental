using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class LakeWaterSurface : MonoBehaviour
{
    [SerializeField] Terrain TargetTerrain;
    [SerializeField] bool AutoHeightFromTerrain = true;
    [SerializeField] float HeightAboveTerrainMin = 12f;
    [SerializeField] float WaterHeight = 35f;
    [SerializeField] Vector2 EdgePadding = new(30f, 30f);
    [SerializeField] bool AutoSyncInEditor = true;

    private void OnEnable()
    {
        SyncToTerrain();
    }

    private void OnValidate()
    {
        SyncToTerrain();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && AutoSyncInEditor)
        {
            SyncToTerrain();
        }
#endif
    }

    public void SyncToTerrain()
    {
        if (TargetTerrain == null)
        {
            TargetTerrain = FindFirstObjectByType<Terrain>();
        }

        if (TargetTerrain == null || TargetTerrain.terrainData == null)
        {
            return;
        }

        TerrainData terrainData = TargetTerrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPosition = TargetTerrain.transform.position;
        float surfaceHeight = AutoHeightFromTerrain
            ? EstimateTerrainMinHeight(terrainData, terrainPosition.y) + HeightAboveTerrainMin
            : WaterHeight;

        transform.position = new Vector3(
            terrainPosition.x + terrainSize.x * 0.5f,
            surfaceHeight,
            terrainPosition.z + terrainSize.z * 0.5f);
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(
            terrainSize.x / 10f + EdgePadding.x,
            1f,
            terrainSize.z / 10f + EdgePadding.y);
    }

    private static float EstimateTerrainMinHeight(TerrainData terrainData, float terrainWorldY)
    {
        int resolution = terrainData.heightmapResolution;
        int step = Mathf.Max(1, resolution / 128);
        float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);
        float minHeight = float.MaxValue;

        for (int y = 0; y < resolution; y += step)
        {
            for (int x = 0; x < resolution; x += step)
            {
                float sampleHeight = heights[y, x];
                if (sampleHeight < minHeight)
                {
                    minHeight = sampleHeight;
                }
            }
        }

        if (minHeight == float.MaxValue)
        {
            minHeight = 0f;
        }

        return terrainWorldY + minHeight * terrainData.size.y;
    }
}
