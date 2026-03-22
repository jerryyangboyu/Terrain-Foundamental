using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class LakeWaterSurfaceController : MonoBehaviour
{
    const string DefaultWaterObjectName = "_LakeWaterSurface";
    const string DefaultMaterialPath = "Assets/URPWater/Demo/Materials/Water/M_URPWater_Demo_Simple.mat";
    static Mesh quadMesh;

    [SerializeField] ProcGenManager ProcGenManager;
    [SerializeField] Terrain TargetTerrain;
    [SerializeField] Material WaterMaterial;
    [SerializeField] string WaterObjectName = DefaultWaterObjectName;
    [SerializeField] float FallbackSeaLevelWorldY = 40f;
    [SerializeField] float WaterSurfaceOffset = 0.05f;

    GameObject waterSurfaceObject;

    void OnEnable()
    {
        SyncWaterSurface();
    }

    void OnValidate()
    {
        SyncWaterSurface();
    }

    [ContextMenu("Refresh Lake Water Surface")]
    public void SyncWaterSurface()
    {
        if (!ResolveReferences())
        {
            return;
        }

        EnsureDefaultMaterial();

        TerrainData terrainData = TargetTerrain.terrainData;
        if (terrainData == null)
        {
            return;
        }

        MeshRenderer meshRenderer = EnsureWaterSurfaceComponents(out Transform waterTransform);
        if (meshRenderer == null)
        {
            return;
        }

        Vector3 terrainOrigin = TargetTerrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        float waterWorldY = LakeWaterLevelResolver.ResolveSeaLevelWorldY(ProcGenManager, FallbackSeaLevelWorldY) + WaterSurfaceOffset;

        waterTransform.position = new Vector3(
            terrainOrigin.x + terrainSize.x * 0.5f,
            waterWorldY,
            terrainOrigin.z + terrainSize.z * 0.5f);
        waterTransform.rotation = Quaternion.identity;
        waterTransform.localScale = new Vector3(terrainSize.x, 1f, terrainSize.z);

        if (WaterMaterial != null)
        {
            meshRenderer.sharedMaterial = WaterMaterial;
        }
    }

    bool ResolveReferences()
    {
        if (ProcGenManager == null)
        {
            ProcGenManager = GetComponent<ProcGenManager>();
        }

        if (TargetTerrain == null)
        {
            TargetTerrain = ProcGenManager != null ? ProcGenManager.GetComponentInChildren<Terrain>() : null;
        }

        if (TargetTerrain == null)
        {
            TargetTerrain = Terrain.activeTerrain;
        }

        if (TargetTerrain == null)
        {
            TargetTerrain = FindFirstObjectByType<Terrain>();
        }

        return ProcGenManager != null && TargetTerrain != null;
    }

    void EnsureDefaultMaterial()
    {
#if UNITY_EDITOR
        if (WaterMaterial == null)
        {
            WaterMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
            if (WaterMaterial != null)
            {
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }

    MeshRenderer EnsureWaterSurfaceComponents(out Transform waterTransform)
    {
        if (waterSurfaceObject == null)
        {
            Transform existingChild = transform.Find(string.IsNullOrWhiteSpace(WaterObjectName) ? DefaultWaterObjectName : WaterObjectName);
            if (existingChild != null)
            {
                waterSurfaceObject = existingChild.gameObject;
            }
        }

        if (waterSurfaceObject == null)
        {
            waterSurfaceObject = new GameObject(string.IsNullOrWhiteSpace(WaterObjectName) ? DefaultWaterObjectName : WaterObjectName);
            waterSurfaceObject.transform.SetParent(transform, false);
            waterSurfaceObject.layer = gameObject.layer;
        }

        waterTransform = waterSurfaceObject.transform;

        MeshFilter meshFilter = waterSurfaceObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = waterSurfaceObject.AddComponent<MeshFilter>();
        }

        MeshRenderer meshRenderer = waterSurfaceObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = waterSurfaceObject.AddComponent<MeshRenderer>();
        }

        MeshCollider existingCollider = waterSurfaceObject.GetComponent<MeshCollider>();
        if (existingCollider != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(existingCollider);
            }
            else
#endif
            {
                Destroy(existingCollider);
            }
        }

        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshFilter.sharedMesh = BuildQuadMesh();

        return meshRenderer;
    }

    static Mesh BuildQuadMesh()
    {
        if (quadMesh != null)
        {
            return quadMesh;
        }

        quadMesh = new Mesh
        {
            name = "LakeWaterQuad",
            hideFlags = HideFlags.HideAndDontSave
        };

        quadMesh.SetVertices(new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, 0.5f)
        });
        quadMesh.SetUVs(0, new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f)
        });
        quadMesh.SetTriangles(new[] { 0, 1, 2, 2, 1, 3 }, 0);
        quadMesh.RecalculateNormals();
        quadMesh.RecalculateBounds();
        return quadMesh;
    }
}
