# Testing guide

## Phase 1: one-time setup in Unity

1. GitHub Desktop: Fetch origin, then Pull origin.
2. Open the project from Unity Hub. Wait for the compile spinner (bottom right) to stop.
3. Menu bar: **Tools > Arena > 1. Run All Steps**. If a dialog asks to save the current scene, click Don't Save.
4. Console should end with `Arena verify: all checks passed`. Red lines mean something is missing: read them.
5. The MainMenu scene is now open. Press Play once alone: status should reach "Connected to Photon, region: ..." and Quick Play should take you into the arena.
6. Stop Play, then in GitHub Desktop commit everything (summary `Phase 1 generated scenes and prefabs`) and Push origin.

## Phase 1: two-player test with Multiplayer Play Mode

1. **Window > Multiplayer > Multiplayer Play Mode**, tick **Player 2**.
2. Press Play. A second window opens for Player 2. Arrange the two windows side by side.
3. In each window type a different nickname and click Quick Play. Both should land in the same room (the HUD says Players: 2).

Alternative: **File > Build Profiles > Build** into a `Builds` folder, run the app, and press Play in the Editor.

## Phase 1 checklist

- [ ] Both players see each other move, including diagonally, with no speed boost on diagonals.
- [ ] Walls and cover blocks stop the player. Nobody can leave the arena.
- [ ] Holding Space fires repeatedly with a visible gap (cooldown). Projectiles go in the facing direction, even when standing still.
- [ ] Projectiles vanish on walls and after about 2 seconds.
- [ ] Shooting your own body does nothing. Hitting the other player drops their health bar on BOTH screens by 25.
- [ ] At 0 HP the victim disappears, sees "Respawning in 3, 2, 1", and reappears far from the killer with full health.
- [ ] The scoreboard shows +1 K for the killer and +1 D for the victim on both screens.
- [ ] Leave button returns to the menu; the other player keeps playing and sees Players: 1.
- [ ] Rejoin with Quick Play lands back in the same room.
- [ ] Console: no red errors during the whole test. Yellow warnings about PhotonView ID or missing EventSystem would be a problem; tell me.
