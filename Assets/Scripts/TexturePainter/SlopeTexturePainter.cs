using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexturePainter_Slope : BaseTexturePainter
{
    [SerializeField] string TextureID;
    [SerializeField] AnimationCurve IntensityVsSlope;

    public override void Execute(in TexturePainterContext context)
    {
        int textureLayer = context.GetLayerForTexture(TextureID);

        for (int y = 0; y < context.AlphaMapResolution; ++y)
        {
            int heightMapY = Mathf.FloorToInt((float)y * context.MapResolution / context.AlphaMapResolution);

            for (int x = 0; x < context.AlphaMapResolution; ++x)
            {
                int heightMapX = Mathf.FloorToInt((float)x * context.MapResolution / context.AlphaMapResolution);

                // skip if we have a biome and this is not our biome
                if (context.BiomeIndex >= 0 && context.BiomeMap[heightMapX, heightMapY] != context.BiomeIndex)
                    continue;

                context.AlphaMaps[x, y, textureLayer] = Strength * IntensityVsSlope.Evaluate(1f - context.SlopeMap[x, y]);
            }
        }        
    }
}
