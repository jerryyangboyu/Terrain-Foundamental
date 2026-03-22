# First-Person Player Controller

## Goal

Add a runtime first-person player rig to `SampleScene` that:

- spawns at a random point on the terrain
- uses the Unity Input System action asset already configured for the project
- moves with `WASD`
- looks around in first person with the existing `Look` action

## Scope

- create one controller script under `Assets/Scripts`
- attach it to a single root object in `SampleScene`
- reuse the existing scene camera when possible instead of introducing a new camera setup

## Notes

- prefer simple `CharacterController` movement over a larger character framework
- use `Terrain.activeTerrain` as the primary spawn source
- keep the implementation self-contained so it can be replaced later if a full player system is added
