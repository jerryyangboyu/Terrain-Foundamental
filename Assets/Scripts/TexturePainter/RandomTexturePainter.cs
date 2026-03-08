using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexturePainter_Random : BaseTexturePainter
{
    [SerializeField] List<string> TextureIDs;

    public override void Execute(in TexturePainterContext context)
    {
        for (int y = 0; y < context.AlphaMapResolution; ++y)
        {
            int heightMapY = Mathf.FloorToInt((float)y * context.MapResolution / context.AlphaMapResolution);

            for (int x = 0; x < context.AlphaMapResolution; ++x)
            {
                int heightMapX = Mathf.FloorToInt((float)x * context.MapResolution / context.AlphaMapResolution);

                // skip if we have a biome and this is not our biome
                if (context.BiomeIndex >= 0 && context.BiomeMap[heightMapX, heightMapY] != context.BiomeIndex)
                    continue;

                string randomTexture = TextureIDs[Random.Range(0, TextureIDs.Count)];

                context.AlphaMaps[x, y, context.GetLayerForTexture(randomTexture)] = Strength;
            }
        }        
    }
}
