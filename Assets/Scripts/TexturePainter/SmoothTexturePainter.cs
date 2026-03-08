using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexturePainter_Smooth : BaseTexturePainter
{
    [SerializeField] int SmoothingKernelSize = 5;
    
    public override void Execute(in TexturePainterContext context)
    {
        if (context.BiomeMap != null)
        {
            Debug.LogError("TexturePainter_Smooth is not supported as a per biome modifier [" + gameObject.name + "]");
            return;
        }

        for (int layer = 0; layer < context.AlphaMaps.GetLength(2); ++layer)
        {
            float[,] smoothedAlphaMap = new float[context.AlphaMapResolution, context.AlphaMapResolution];

            for (int y = 0; y < context.AlphaMapResolution; ++y)
            {
                for (int x = 0; x < context.AlphaMapResolution; ++x)
                {
                    float alphaSum = 0f;
                    int numValues = 0;

                    // sum the neighbouring values
                    for (int yDelta = -SmoothingKernelSize; yDelta <= SmoothingKernelSize; ++yDelta)
                    {
                        int workingY = y + yDelta;
                        if (workingY < 0 || workingY >= context.AlphaMapResolution)
                            continue;

                        for (int xDelta = -SmoothingKernelSize; xDelta <= SmoothingKernelSize; ++xDelta)
                        {
                            int workingX = x + xDelta;
                            if (workingX < 0 || workingX >= context.AlphaMapResolution)
                                continue;

                            alphaSum += context.AlphaMaps[workingX, workingY, layer];
                            ++numValues;
                        }                    
                    }

                    // store the smoothed (aka average) alpha
                    smoothedAlphaMap[x, y] = alphaSum / numValues;
                }
            }

            for (int y = 0; y < context.AlphaMapResolution; ++y)
            {
                for (int x = 0; x < context.AlphaMapResolution; ++x)
                {
                    // blend based on strength
                    context.AlphaMaps[x, y, layer] = Mathf.Lerp(context.AlphaMaps[x, y, layer], smoothedAlphaMap[x, y], Strength);
                }
            }  
        }
    
    }
}
