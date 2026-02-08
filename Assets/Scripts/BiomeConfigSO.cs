using UnityEngine;

[CreateAssetMenu(fileName = "Biome Config", menuName = "Procedural Generation/Biome Configuration", order = -1)]
public class BiomeConfigSO : ScriptableObject
{
    public string Name;

    // higher the intensity, more biome we are going to spread
    [Range(0f, 1f)] public float MinIntensity = 0.5f;
    [Range(0f, 1f)] public float MaxIntensity = 1f;

    // the lower the rate, the more it is going to spread out
    [Range(0f, 1f)] public float MinDecayRate = 0.01f;
    [Range(0f, 1f)] public float MaxDecayRate = 0.02f;

    [Header("Preview")]
    public bool UseCustomPreviewColor = false;
    public Color PreviewColor = Color.white;

    public GameObject HeightModifier;
}
