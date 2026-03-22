# Global Height Texture Rules

1. Use a 4-anchor height profile: `Low`, `Mid`, `High`, `Peak`.
2. Remove `WeightAtZero` and `WeightAtOne`; the bottom and top ends are covered by the nearest anchors.
3. Use `PeakStartHeight` as the single global height boundary; derive the lower anchors from it instead of exposing extra anchor-height fields.
4. Add `snow` as a terrain texture source and blend it only into the highest elevations.
5. Use a deterministic noise-based painter for `Lake` biome replacement so local biome textures can fully override the global base coat without point-sampled random speckling.
