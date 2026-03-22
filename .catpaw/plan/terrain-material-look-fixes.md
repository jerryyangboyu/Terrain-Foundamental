# Terrain Material Look Fixes

## Goal
Reduce the "plastic" look of the generated terrain by reusing tuned terrain-layer settings and improving the most impactful scene rendering settings.

## Approach
1. Extend biome texture data so each generated terrain texture can point at a tuned `TerrainLayer` template.
2. Update terrain layer generation to clone relevant visual settings from the template layer instead of creating flat default layers.
3. Populate current biome configuration assets with the matching sample `TerrainLayer` templates.
4. Improve scene rendering defaults that strongly affect terrain readability:
   - increase terrain shadow coverage
   - enable instanced terrain drawing
   - strengthen post/lighting support only where it directly helps terrain definition

## Validation
1. Refresh Unity and verify scripts compile cleanly.
2. Confirm generated `Layer_*.terrainlayer` assets inherit tuned values like tile size, normal scale, and mask remap.
3. Check scene/quality assets for the updated terrain and shadow settings.
