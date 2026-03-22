using UnityEngine;

public class SetValueHeightMapModifier: BaseHeightMapModifier
{
    [SerializeField] float TargetHeight;

    public float WorldTargetHeight => TargetHeight;

    public override void Execute(int mapResolutionSize, float[,] heightMap, Vector3 heightmapScale, byte[,] biomeMap, BiomeConfigSO biomeConfig, int biomeIndex)
    {
        for (int y = 0; y < mapResolutionSize; y++)
        {
            for (int x = 0; x < mapResolutionSize; x++)
            {
                if (biomeIndex >= 0 && biomeMap[x, y] != biomeIndex) continue;
                float newHeight = TargetHeight / heightmapScale.y;
                heightMap[y, x] = Mathf.Lerp(heightMap[y, x], newHeight, Strength);
            }
        }
    }
}
