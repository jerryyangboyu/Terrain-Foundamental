using UnityEngine;

public class BiomeOverlayVisualizer
{
    private const string OverlayName = "BiomePreviewOverlay";

    private GameObject overlayQuad;
    private Material overlayMaterial;
    private Texture2D overlayTexture;

    public void Render(Transform parent, Terrain targetTerrain, Texture2D newOverlayTexture, float heightOffset)
    {
        Renderer overlayRenderer = EnsureRenderer(parent);
        if (overlayRenderer == null || targetTerrain == null || targetTerrain.terrainData == null || newOverlayTexture == null)
        {
            return;
        }

        if (overlayTexture != null)
        {
            Object.DestroyImmediate(overlayTexture);
        }

        overlayTexture = newOverlayTexture;
        overlayRenderer.sharedMaterial.mainTexture = overlayTexture;
        SetVisible(true);

        Vector3 terrainPosition = targetTerrain.transform.position;
        Vector3 terrainSize = targetTerrain.terrainData.size;
        Transform overlayTransform = overlayRenderer.transform;
        overlayTransform.position = terrainPosition + new Vector3(terrainSize.x * 0.5f, heightOffset, terrainSize.z * 0.5f);
        overlayTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        overlayTransform.localScale = new Vector3(terrainSize.x, terrainSize.z, 1f);
    }

    public void SetVisible(bool visible)
    {
        if (overlayQuad != null)
        {
            overlayQuad.SetActive(visible);
        }
    }

    private Renderer EnsureRenderer(Transform parent)
    {
        if (overlayQuad == null)
        {
            overlayQuad = GameObject.Find(OverlayName);
            if (overlayQuad == null)
            {
                overlayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                overlayQuad.name = OverlayName;
                overlayQuad.transform.SetParent(parent, true);

                Collider overlayCollider = overlayQuad.GetComponent<Collider>();
                if (overlayCollider != null)
                {
                    Object.DestroyImmediate(overlayCollider);
                }
            }
        }

        if (overlayMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }
            if (shader == null)
            {
                return null;
            }
            overlayMaterial = new Material(shader);
        }

        Renderer overlayRenderer = overlayQuad.GetComponent<Renderer>();
        overlayRenderer.sharedMaterial = overlayMaterial;
        return overlayRenderer;
    }
}
