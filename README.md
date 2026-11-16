# CW1: Multiplayer Arena Shooter (Unity + Photon PUN 2)

Top-down multiplayer arena shooter built in Unity with Photon PUN 2.

## Folder layout

- `Assets/` game content, scripts, scenes, prefabs and the Photon package
- `Packages/` Unity package manifest
- `ProjectSettings/` Unity project settings
- `docs/` audit, learning notes, implementation summary and video script

## Opening the project

1. Clone this repository.
2. Open the folder in Unity Hub with the version listed in `ProjectSettings/ProjectVersion.txt`.
3. Unity rebuilds the `Library/` folder on first open. This is normal and takes a few minutes.

## Testing with two players

Use ParrelSync (Window > Package Manager > Add package from git URL):

```
https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync
```

Then ParrelSync > Clones Manager > Create new clone > Open in New Editor, and press Play in both editors.
