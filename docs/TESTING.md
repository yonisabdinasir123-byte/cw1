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

## Phase 2: lobby

Setup: GitHub Desktop Fetch and Pull, open Unity, wait for compile, then **Tools > Arena > 4. Build Main Menu Scene**
(only step 4; the Game scene is unchanged). Commit and push the new MainMenu scene afterwards.

Run two players (Multiplayer Play Mode, Player 2 ticked) and check:

- [ ] Connect panel: empty nickname gets a random "PlayerNNN" name; a typed name is remembered next time.
- [ ] Status line shows "Connected, region: ..." then "In lobby as <name>".
- [ ] Player 1: Create Room with a name and Max players 2. Player 2's list shows it within a second as "name (1 / 2)".
- [ ] Player 2 clicks the room: both see the Room panel with both names, host marked "(host)", themselves "(you)".
- [ ] Player 1 (host) has Start Game enabled; Player 2 has it greyed with "Waiting for the host...".
- [ ] Player 2 clicks Leave Room: Player 1's Start greys out with "Waiting for another player (1 / 2)".
- [ ] Player 2 rejoins. Player 1 clicks Start Game: both load the arena.
- [ ] While they play, a third client (or Player 2 after leaving) does NOT see the room in the list (closed and hidden).
- [ ] In the arena, Leave returns to the menu, and the lobby panel appears automatically with the room list.
- [ ] Create a room with the same name as an existing one: status says Photon's error, nothing crashes.
- [ ] Join a room that just became full: "Could not join room: ..." appears.
- [ ] Quit button stops Play mode (closes the app in a build).
- [ ] Console has no red errors.

## Phase 3: timer, winner and end screen

Setup: Fetch and Pull, open Unity, wait for compile, then **Tools > Arena > 3. Build Game Scene**.
For a quick test, select the GameManager object in the Game scene and set Round Manager > Round Duration Seconds to 30, then save the scene (Cmd+S). Commit and push.

- [ ] Both windows show the same m:ss timer at the top, within a second of each other.
- [ ] Timer turns red in the last 10 seconds.
- [ ] Get one kill, then wait for 0:00. Both windows show the end screen at the same moment.
- [ ] The killer's window: "<name> wins!" and "You Won!" in green. The other: same winner name and "You Lost" in red.
- [ ] Final scoreboard on both screens matches the live scoreboard.
- [ ] After the end, WASD and Space do nothing, and a dead player does not respawn.
- [ ] Draw test: let the timer run out with no kills. Both see "Draw: A and B" and "Draw!".
- [ ] Master-switch test: start a round, then have the host (Player 1) click Leave mid-round. Player 2's timer keeps counting without a jump, and when it reaches 0 Player 2 sees the end screen (Player 2 is now master).
- [ ] Leave on the end screen returns to the lobby.
- [ ] Console has no red errors.
