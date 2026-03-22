# Biome Object Placers

## Goal
Add a biome-scoped object placement pipeline that mirrors the existing texture painter flow and lets each biome spawn its own prefab set.

## Approach
1. Extend `BiomeConfigSO` with an `ObjectPlacer` hook, parallel to `TerrainPainter`.
2. Add `ObjectPlacerContext` and `BaseObjectPlacer` so biome-aware placement logic can reuse the current terrain and biome-map data.
3. Implement a first `RandomObjectPlacer` that scatters weighted prefabs on a jittered grid with biome, height, and slope filtering.
4. Run placers from `ProcGenManager` after terrain height/texture generation and rebuild a dedicated generated-objects root each time.

## Validation
1. Refresh Unity and verify the new scripts compile.
2. Create biome placer GameObjects, attach `RandomObjectPlacer`, and assign sample prefabs from `Assets/TerrainSampleAssets/Prefabs`.
3. Regenerate the world and confirm each biome only spawns its configured prefabs under `_GeneratedObjects`.
