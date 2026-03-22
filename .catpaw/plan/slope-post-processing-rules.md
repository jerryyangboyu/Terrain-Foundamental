# Slope Post Processing Rules

1. Replace the current slope post-processing model with a mask-based blend model instead of `Mathf.Max` replacement.
2. Drive the mask from a slope window in degrees so rock can target steep shoulders without automatically taking over vertical cliff walls.
3. Preserve the existing base paint by blending from current alphamaps into a normalized rocky target distribution.
4. Keep the parameters procedural and compact: slope window, falloff, optional local slope smoothing, and per-texture noise/weight.
5. Re-enable and configure a dedicated post-processing prefab once the new painter is in place.
