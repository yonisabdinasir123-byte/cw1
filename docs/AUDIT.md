# Step 0 Audit: cw1 (Unity 6000.0.85f2 + Photon PUN 2)

Date: 16 Nov 2026. Nothing has been changed yet. This file records what exists, what is missing against every marking criterion, the risks found, and the proposed plan.

## 1. What exists

| Item | Finding |
|---|---|
| Unity version | 6000.0.85f2 (Unity 6.0 LTS), Universal Render Pipeline template |
| Photon | PUN 2 imported at `Assets/Photon/` with PhotonChat, PhotonRealtime, PhotonUnityNetworking and all demo scenes |
| Photon settings | `PhotonServerSettings.asset` has the App ID set, Use Name Server on, Fixed Region empty, App Version empty, Run In Background on |
| Scenes | `Assets/Scenes/SampleScene.unity` (template) and `Assets/Scenes/MainMenu.slnx.unity` (empty: camera, light, volume). Both in Build Settings, SampleScene first |
| Scripts | None of ours. Only the template's `TutorialInfo/Readme.cs` and `ReadmeEditor.cs` |
| Prefabs, Resources, tags, layers | None. Tag list empty, no custom layers |
| Input | New Input System only (`activeInputHandler: 1`), with the template `InputSystem_Actions.inputactions` |
| Player settings | Product name `cw1-unity`, default 1024x768, fullscreen mode Fullscreen Window, Run In Background off |
| TextMesh Pro | Essentials not imported yet (Unity 6 prompts on first TMP use) |
| Git | `main` branch, Unity `.gitignore`, LFS rules in `.gitattributes`. DLLs, textures and audio are LFS objects (98 files). Repo is 13 MB |

**Headline: this is a fresh project.** The tutorial work described in the brief (character, camera, shooting, enemies, health bar, lobby, room list) is not present. Every feature must be built from scratch, which changes the phase plan slightly (see section 4).

## 2. Existing scripts

| Script | What it does | Keep? |
|---|---|---|
| `Assets/TutorialInfo/Scripts/Readme.cs` | ScriptableObject for the template welcome page | Delete at submission |
| `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs` | Draws that page in the Inspector | Delete at submission |
| `Assets/Photon/**` | Photon library and demos | Keep `Code`, `Resources`, `UtilityScripts`, `PhotonLibs`, `PhotonRealtime`. Delete `Demos` and `PhotonChat` at submission |

## 3. Criteria status

| # | Marks | Criterion | Status |
|---|---|---|---|
| 1 | 15 | Arena, kills per round, 1920x1080 UI, 8-way movement, projectile, damage, death and respawn | Missing |
| 2 | 15 | Lobby: nickname, create, join, room list, leave, start, quit | Missing |
| 3 | 10 | Full game playable to completion, no bugs | Missing |
| 4 | 5 | Winner calculated and announced to all clients | Missing |
| 5 | 7 | "You Won!" / "You Lost" / draw on each client | Missing |
| 6 | 7 | Winner by kills when synced timer ends | Missing |
| 7 | 10 | Play Again, only with 2+ players, own logic | Missing |
| 8 | 10 | Customisations | Missing |
| 9 | 6 | 1-page implementation summary | Missing |
| 10 | 5 | Project reduced in size | Partly: `.gitignore` excludes Library etc. Photon demos and template leftovers still present |
| 11 | 10 | 5 to 7 minute demo video | Missing (script to be written in Phase 7) |

## 4. Risks and issues found

| # | Issue | Why it matters | Proposed fix |
|---|---|---|---|
| R1 | Input System set to "new only" | Simple `Input.GetAxisRaw` code (easiest to explain in the video) throws at runtime; PUN demo scripts also use the old API | Set Player > Active Input Handling to **Both**. One line in ProjectSettings, I will apply it in Phase 1 |
| R2 | Scene file named `MainMenu.slnx.unity` | Confusing name in Build Settings and in the video | Rename to `MainMenu.unity` via an Editor helper |
| R3 | `SampleScene` is scene 0 in Build Settings | Build would open the template scene, not the menu | Build Settings order: `MainMenu` (0), `Game` (1). Remove SampleScene |
| R4 | No Resources folder | `PhotonNetwork.Instantiate` only finds prefabs under a `Resources/` folder | Create `Assets/Resources/` and put Player and Projectile prefabs there |
| R5 | Default resolution 1024x768, Run In Background off in Player settings | UI criterion asks for 1920x1080; a background Editor window would pause and time out from Photon | Set 1920x1080, Windowed, Run In Background on |
| R6 | No `Game` scene, no tags or layers | Needed for arena, spawn points, projectile collision filtering | Editor helper `Tools/Arena/Build Arena` creates the scene, walls, spawn points, layers |
| R7 | Photon App Version empty | Fine for now; PUN defaults to "1". Must stay identical on every client | Leave as is, document in LEARNING.md |
| R8 | Editor Dev Region may differ from a build's region | Build and Editor would not see each other's rooms | If it happens, set Fixed Region to `eu` (or your closest) in PhotonServerSettings |
| R9 | Photon demos and PhotonChat in the repo (~8 MB, 13 scenes) | Criterion 10 and build size; demo scenes clutter Build Settings | Delete in Phase 7 after you confirm |
| R10 | No tutorial lobby to reuse | Phase 1 gameplay cannot be tested with 2 clients without a connection path | Phase 1 adds a minimal `NetworkLauncher` (connect, join-or-create, load Game). Phase 2 builds the full lobby UI on top |
| R11 | LFS pointers for DLLs | Works for you (GitHub Desktop) and for this session. Anyone cloning without LFS gets broken Photon DLLs | Note in SUBMISSION_CHECKLIST: zip from your local folder, never from a plain git clone |

## 5. Proposed plan

Architecture, one responsibility per script, all C#:

```
Assets/Scripts/
  Network/   NetworkKeys.cs, NetworkLauncher.cs (Phase 1), LobbyManager.cs (Phase 2)
  Player/    PlayerMovement.cs, PlayerShooting.cs, PlayerHealth.cs, PlayerSetup.cs
  Combat/    Projectile.cs
  Game/      RoundManager.cs, SpawnManager.cs
  UI/        ScoreboardUI.cs, HudUI.cs, EndScreenUI.cs, LobbyUI.cs
Assets/Editor/
  ArenaBuilder.cs      Tools/Arena/Build Arena (Game scene, walls, floor, spawn points, layers, tags)
  PrefabBuilder.cs     Tools/Arena/Build Prefabs (Player, Projectile into Resources/)
  UiBuilder.cs         Tools/Arena/Build Game UI and Build Lobby UI (Canvas 1920x1080, match 0.5)
  ProjectFixer.cs      Tools/Arena/Fix Project Settings (input, resolution, scene order, rename scene)
Assets/Resources/      Player.prefab, Projectile.prefab
Assets/Scenes/         MainMenu.unity, Game.unity
```

Networking decisions (justified in LEARNING.md):

- Movement: `PhotonTransformView` for position and rotation. Simplest to explain, built-in interpolation.
- Projectiles: `PhotonNetwork.Instantiate` by the shooter. The projectile's owner runs hit detection and sends `TakeDamage` as an RPC to the victim's PhotonView, so damage is applied exactly once.
- Kills: `Player.CustomProperties` with keys in `NetworkKeys`. Scoreboard refreshes in `OnPlayerPropertiesUpdate`.
- Timer: master client writes `PhotonNetwork.Time` at round start into room properties. Each client computes the remaining time locally, so a master switch does not reset it.
- Winner: master client computes it when the timer ends and writes it to a room property, so late readers and all clients agree.
- Play Again: master-client restart. Master checks `PlayerCount >= 2`, resets room properties, and calls an RPC on everyone to respawn and clear kills.

Phases, each stopping for your test:

1. **Core gameplay**: settings fixes (R1 to R6), arena, player, movement, shooting, damage, death and respawn, kill tracking, scoreboard, minimal launcher.
2. **Lobby**: nickname, create, join, room list with dictionary cache, leave, start (master only, 2+), quit, error feedback, `AutomaticallySyncScene`.
3. **Round timer and winner**: synced timer, winner by kills, draw handling, end screen with personalised message.
4. **Play Again**: master restart with live 2+ check, lone-player handling, master switch mid-end-screen.
5. **Customisations**: I will propose 4 to 5 ideas for you to pick.
6. **Hardening**: leave mid-round, master leaves, late join blocked, simultaneous deaths, spam clicks, resolutions, console clean.
7. **Deliverables**: implementation summary, video script, submission checklist, cleanup (R9, template leftovers).

## 6. What I need from you

Reply "approved" to start Phase 1, or tell me what to change. Two quick answers would also help:

1. Keep the Universal Render Pipeline (default) or switch to Built-in? URP is fine; I will use simple URP Lit materials.
2. 3D top-down with a camera looking down at the arena (my default) or true 2D with sprites? The brief says top-down or third-person, so 3D top-down is the safer reading.
