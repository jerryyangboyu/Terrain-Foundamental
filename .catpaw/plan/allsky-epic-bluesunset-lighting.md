# AllSky Epic BlueSunset Lighting

## Goal
Reuse the `AllSkyFree/Epic_BlueSunset` lighting baseline in `SampleScene` without replacing the project's existing post-processing stack.

## Approach
1. Copy the `Epic_BlueSunset` environment render settings into `SampleScene`:
   - fog
   - ambient colors and mode
   - reflection intensity
2. Preview the scene and confirm the baseline mood is moving in the right direction.
3. Tune the main directional light to better match the skybox sun direction and color.
4. Refresh the reflection probe and decide whether lighting data should be rebaked or cleared.

## Validation
1. Open `SampleScene` and compare the horizon, shadow contrast, and terrain readability before and after step 1.
2. Confirm the scene still renders correctly with the existing global volume profile.
3. Only proceed to light-angle and rebake work after the first pass is visually approved.
