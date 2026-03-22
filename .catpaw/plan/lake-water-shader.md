# Lake Water Shader

## Goal
Add a simple lake-water rendering path that visually fills low-altitude lake basins without changing biome weighting or terrain painting rules.

## Approach
1. Create a lightweight URP water shader with:
   - shallow/deep color blending from scene depth
   - subtle animated waves from world-space noise
   - shoreline foam/fade near terrain intersections
   - fresnel-based sky tint to avoid a flat plastic look
2. Add a tiny scene helper component that keeps a single water surface aligned to the active terrain bounds and water level.
3. Create a material from the shader and apply it to one lake-water surface object in `SampleScene`.

## Constraints
- Do not modify biome painter weighting or layer distribution logic.
- Keep the implementation scene-local and easy to tune from the Inspector.
- Prefer one shared water surface over procedural lake mesh generation for now.

## Validation
1. Refresh Unity and verify scripts/shader import cleanly.
2. Confirm the water surface tracks the terrain extents in `SampleScene`.
3. Capture a scene screenshot that shows the lake-water effect over low-altitude areas.
