using UnityEngine;

public class RandomHeightMapModifier : BaseHeightMapModifier
{
    // This number is corresponding to our map resolution
    [SerializeField] float HeightDelta;

    public override void Execute(int mapResolutionSize, float[,] heightMap, Vector3 heightmapScale, byte[,] biomeMap, BiomeConfigSO biomeConfig, int biomeIndex)
    {
        for (int y = 0; y < mapResolutionSize; y++)
        {
            for (int x = 0; x < mapResolutionSize; x++)
            {
                if (biomeIndex >= 0 && biomeMap[x, y] != biomeIndex) continue;
                float newHeight = heightMap[x, y] + (Random.Range(-HeightDelta, HeightDelta) / heightmapScale.y);
                heightMap[x, y] = Mathf.Lerp(heightMap[x, y], newHeight, Strength);
            }
        }
    }
}
