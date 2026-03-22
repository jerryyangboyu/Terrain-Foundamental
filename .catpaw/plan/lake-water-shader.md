# Lake Water Integration

## Goal
Integrate the imported `URPWater` package so the scene gets a lake-water surface at the existing lake biome sea level without changing biome weighting or terrain painting rules.

## Approach
1. Reuse the existing lake sea-level logic from gameplay code by extracting it into a shared helper.
2. Add a small scene helper component that:
   - reads the active terrain bounds
   - resolves the lake sea level from the proc-gen configuration
   - creates one shared quad mesh surface aligned to the terrain extents
3. Default the helper to a package material from `Assets/URPWater/Demo/Materials/Water`.
4. Attach the helper to `ProcGenManager` in `SampleScene` so the water surface can be refreshed without manual scene setup.

## Constraints
- Do not modify biome painter weighting or layer distribution logic.
- Keep the first pass scene-local and easy to tune from the Inspector.
- Prefer one shared water surface over biome-shaped mesh generation for now.
- Reuse package assets and existing sea-level logic instead of introducing a parallel water system.
- For shoreline polish, prefer reusing `URPWater` demo material features such as edge fade, foam, and foam ripples before adding custom shoreline systems.
- For the second pass, prioritize de-stylizing the demo material before considering any shoreline mesh or emitter work.

## Validation
1. Refresh Unity and verify scripts/shader import cleanly.
2. Confirm `ProcGenManager` can create/update the lake-water surface in `SampleScene`.
3. Verify the water sits at the lake biome sea level and spans the terrain bounds.
4. Test a shoreline-focused water material variant that produces visible near-shore foam/ripple breakup.
5. Capture a scene screenshot before deciding whether a second pass needs biome-shaped masking or dynamic shoreline emitters.
6. If the shoreline still reads as too synthetic, retune color, reflection, edge width, and wave amplitude before adding new systems.
