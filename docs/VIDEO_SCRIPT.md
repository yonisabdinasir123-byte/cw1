# Demo Video Script (6:00)

**Before recording**
- Make a Mac build: File > Build Profiles > Build, into a `Builds` folder. Put the build window on the left and the Editor on the right. Press Play in the Editor. If the two cannot see each other's rooms, set Fixed Region to `eu` in PhotonServerSettings and build again.
- Use the nicknames `Alice` (build) and `Bob` (Editor). The round length is 60 s.
- Record the gameplay and the code separately (for example with QuickTime > New Screen Recording), then cut them together. Speak slowly. Each row below is about 20 to 40 seconds.
- Have VS Code open with these files in tabs: LobbyManager, PlayerMovement, Projectile, PlayerHealth, KillTracker, RoundManager, PlayAgainController, PowerUp, PowerUpSpawner.

## Showing the code (VS Code)

Explain the code in **VS Code**, not on the GitHub website: it is clearer on camera and lets you jump straight to a method.

1. VS Code > **File > Open Folder** > `Documents/GitHub/cw1`.
2. Press **Cmd and +** two or three times so the code is readable in the recording.
3. **Cmd+P** opens a file by name (type `RoundManager`). **Cmd+Shift+O** jumps to a method by name. **Ctrl+G** jumps to a line number.
4. Open every file below as a tab before recording, so you only click between tabs.
5. Select the key lines with the mouse while you talk about them, so viewers can see what you mean.
6. Practise each method out loud first: what it does, and why only one client does it. `docs/LEARNING.md` has the explanation for every script.

All files are in `Assets/Scripts/`. Line numbers shift if a file is edited, but Cmd+Shift+O always finds the method.

| Video time | File | Method | Line | Point at this |
|---|---|---|---|---|
| 1:05 | Network/LobbyManager.cs | `OnRoomListUpdate` | 189 | The `cachedRooms` dictionary, and how removed rooms are deleted from it |
| 1:05 | Network/LobbyManager.cs | `StartGame` | 139 | The master and 2+ player check, `IsOpen = false`, and `LoadLevel` |
| 2:30 | Player/PlayerMovement.cs | `Update` | 68 | `photonView.IsMine`, `Normalize()`, and `FacingDirection` |
| 2:30 | Combat/Projectile.cs | `OnTriggerEnter` | 51 | `photonView.IsMine` and the RPC sent only to `victim.photonView.Owner` |
| 2:30 | Player/PlayerHealth.cs | `TakeDamage` | 48 | `[PunRPC]`, health reduced, `Die` called at 0 |
| 2:30 | Network/KillTracker.cs | `RpcReportKill` | 49 | The master-only check and `SetCustomProperties` for kills |
| 3:45 | Combat/PowerUp.cs | `OnTriggerEnter`, `RpcRequestPickup` | 55, 70 | The request goes to `MasterClient`, and the `awarded` flag means the first request wins |
| 3:45 | Game/PowerUpSpawner.cs | `AwardPowerUp` | 99 | The RPC sent to `All` |
| 4:35 | Game/RoundManager.cs | `StartRound` | 68 | `PhotonNetwork.Time` stored in the room properties |
| 4:35 | Game/RoundManager.cs | `Update` | 88 | Remaining time calculated, and only the master ends the round |
| 4:35 | Game/RoundManager.cs | `EndRound` | 133 | Players sorted by kills then deaths, ties become a draw |
| 5:05 | Game/PlayAgainController.cs | `EvaluateVotes`, `RestartRound` | 121, 138 | The `>= 2` players check, and every player has voted |

## Script

| Time | On screen | Voice-over script | Criteria proven |
|---|---|---|---|
| 0:00 to 0:15 | Title card, then both windows side by side on the Connect panel | "This is my multiplayer arena shooter, built in Unity 6 with Photon PUN 2. On the left is a Mac build, on the right the Unity Editor: two separate clients." | Intro |
| 0:15 to 0:45 | Type Alice and Bob, click Connect in both. Alice sets Max players 2 and clicks Create Room. Bob's room list updates; Bob clicks the room. Show the Room panel: names with "(host)" and "(you)". Bob's Start is greyed out | "Each player chooses a nickname and connects. Alice creates a room for two players. Bob's room list updates by itself and he joins. Only the host can start, and only when at least two players are in." | 2 (nickname, create, room list, join, start rule) |
| 0:45 to 1:05 | Bob clicks Leave Room: Alice's Start greys out with "Waiting for another player (1 / 2)". Bob rejoins, Alice clicks Start Game. Both load the arena | "If Bob leaves, Alice's Start button disables again. When he rejoins, Alice starts the game, and Photon loads the arena on both clients, because AutomaticallySyncScene is on." | 2 (leave, start) |
| 1:05 to 1:35 | Code: `LobbyManager.OnRoomListUpdate`, then `LobbyManager.StartGame` | "Photon sends room list changes as deltas, so OnRoomListUpdate keeps a dictionary keyed by room name and redraws from it. StartGame checks the player is master with two or more players, closes the room so nobody joins mid-match, and calls LoadLevel." | 2 |
| 1:35 to 2:05 | Alice moves in all eight directions along the walls and cover. The timer counts down in both windows. Alice turns and shoots without moving | "The arena is enclosed by walls that block players and projectiles. Movement is eight-directional with WASD, and diagonals are normalised so they are not faster. Projectiles fire in the last direction moved, even when standing still." | 1 (arena, 8-way, projectile direction) |
| 2:05 to 2:30 | Bob shoots Alice: her health bar drops on both screens. Bob kills her: Alice disappears, "Respawning in 3" shows, and she reappears far away. Scoreboard: Bob 1 K, Alice 1 D on both | "Hits reduce health on both screens. At zero Alice dies, waits three seconds and respawns at the spawn point farthest from enemies. The scoreboard updates on both clients." | 1 (damage, death, respawn, kill count) |
| 2:30 to 3:05 | Code: `PlayerMovement.Update` (normalise, FacingDirection), `Projectile.OnTriggerEnter`, `PlayerHealth.TakeDamage`, `KillTracker.RpcReportKill` | "Only the owner reads input, checked with photonView.IsMine, and PhotonTransformView syncs the position. Only the shooter's client decides a hit, and sends TakeDamage as an RPC to the victim's owner only, so damage is applied exactly once. The victim reports its death to the master client, which is the single writer of kills and deaths in player custom properties. The scoreboard redraws in OnPlayerPropertiesUpdate." | 1, 3 |
| 3:05 to 3:45 | Customisations: Alice dashes with Shift and the trail shows on both screens. A cube spawns; Bob picks up the purple Shield, and the bubble appears on both screens while Alice's shots do nothing. Alice grabs a yellow Rapid Fire. Sparks, the red death burst and the kill feed line "Alice eliminated Bob" on both | "My customisations: a dash with a cooldown bar, three networked power-ups, a kill feed, and hit and death effects with sound. All of them are synced: both players see the shield, the trail and the same kill feed." | 8 |
| 3:45 to 4:10 | Code: `PowerUp.OnTriggerEnter` and `RpcRequestPickup`, then `PowerUpSpawner.AwardPowerUp` | "Power-ups are spawned by the master as room objects. A touch only sends a request to the master, which awards it once, tells every client with one RPC, and destroys it. So two players touching it in the same frame cannot both get it." | 8 |
| 4:10 to 4:35 | Timer turns red under 10 s, reaches 0:00. Both windows show the end screen: "Alice wins!" with "You Won!" in green on Alice's screen and "You Lost" in red on Bob's, plus the final scores. Try to move: nothing happens | "The winner has the most kills when the 60-second timer runs out. Both clients show the winner's name and a personal message. Input is frozen and nobody respawns." | 4, 5, 6 |
| 4:35 to 5:05 | Code: `RoundManager.StartRound`, `Update`, `EndRound` | "The master stores the round start time from PhotonNetwork.Time in a room property. Every client subtracts it from the same server clock, so the timers agree and survive a host change. At zero, only the master ranks players by kills, then fewest deaths. Players still tied share a draw. It publishes the winners as a room property, which every client turns into the end screen." | 4, 5, 6 |
| 5:05 to 5:30 | Alice clicks Play Again: "1 / 2 ready". Bob clicks Play Again: both return to the arena with full health, 0 K 0 D and a fresh timer. Then code: `PlayAgainController.EvaluateVotes` | "Play Again is a vote. Each player sets a Ready property. The master restarts only when everyone still connected has voted and at least two players remain, resetting kills, health, timer, spawns and projectiles on every client." | 7 |
| 5:30 to 5:50 | Let the round end. Bob clicks Leave. Alice's Play Again greys out: "Not enough players to play again". Alice clicks Leave and returns to the lobby, then Quit | "If only one player is left, Play Again is disabled and the player is told why. Leave returns to the lobby, and Quit closes the game." | 7, 2 (leave, quit), 3 |
| 5:50 to 6:00 | Both windows on the lobby | "That is a complete networked match, from the lobby to the winner and Play Again, with no bugs. Thank you for watching." | Wrap-up |

## Full voice-over

About 900 words, roughly 6 to 6.5 minutes at a calm pace. Lines in **[brackets]** are what to show; do not read them out.
Read it through out loud a few times before recording and change any wording that does not sound like you.
Markers reward understanding, so you should be able to explain every sentence if asked.

**0:00 Intro**
[Both windows side by side on the Connect panel]

"Hi, my name is ______, and this is my multiplayer arena shooter, built in Unity 6 with Photon PUN 2. On the left is a Mac build and on the right is the Unity Editor, so these are two separate clients talking over the internet."

**0:15 How Photon works**
[Optional: in Unity, Window > Photon Unity Networking > Highlight Server Settings, and point at App Id and Fixed Region in the Inspector]

"First, how the networking works. Photon is a cloud relay, so I did not write a server. When I press Connect, ConnectUsingSettings reads my App ID from the Photon Server Settings and contacts Photon's Name Server. That picks the closest region, here Europe, and sends us to that region's Master Server, which holds the lobby and the room list. When we create or join a room, Photon moves us to a Game Server that hosts the room and relays every message between its players.

Inside a room, one player is the master client, normally whoever created it. I use the master client as the referee for anything that needs one decision: scores, the timer, the winner and restarting."

**0:50 Lobby**
[Type Alice and Bob, Connect. Alice: Max players 2, Create Room. Bob clicks it in the list. Bob's Start is greyed. Alice clicks Start Game]

"Each player types a nickname, which is stored in PhotonNetwork.NickName so everyone sees it. Alice creates a room for two. It appears in Bob's list, he joins, and both see the player list with the host marked. Bob's Start button is disabled because only the host can start, and Alice's only turns on with two players."

**1:10 Lobby code**
[VS Code: Cmd+P `LobbyManager`, Cmd+Shift+O `OnRoomListUpdate`. Select the foreach loop]

"OnRoomListUpdate is a Photon callback. Photon does not send the whole list each time, only what changed. So I keep a dictionary keyed by room name: I update rooms that changed, remove rooms marked RemovedFromList or closed, and redraw the list from the dictionary."

[Cmd+Shift+O `StartGame`. Select the IsOpen, IsVisible and LoadLevel lines]

"StartGame checks that I am the master client and there are at least two players. It sets IsOpen and IsVisible to false so nobody can join halfway through, then calls PhotonNetwork.LoadLevel. Because AutomaticallySyncScene is on, every other client loads the arena automatically."

**1:45 Gameplay**
[Alice moves in all eight directions, along walls. Bob shoots Alice until she dies, respawns. Scoreboard updates]

"The arena is closed in by walls that stop players and bullets. I move in eight directions with WASD, and bullets fire in the direction I last moved, even when standing still. When Bob hits me, my health drops on both screens. At zero I die, wait three seconds and respawn at the spawn point furthest from enemies, and the scoreboard updates everywhere."

**2:10 Three ways to sync data**
[Keep the gameplay on screen]

"Every networked object has a PhotonView: an ID that is the same on all clients, and an owner. Photon then gives three ways to share data, and I picked one for each job. Serialization streams values about fifteen times a second and does not resend lost packets. That suits things that change constantly, where only the latest value matters, like position and health. RPCs, remote procedure calls, are reliable one-off messages that run a method on other clients, for events like taking damage. Custom properties are stored on Photon's server for a player or the room, so anyone can read them later, even a new master client. I use them for scores, the timer and the winner."

**2:40 Movement code**
[VS Code: Cmd+P `PlayerMovement`, Cmd+Shift+O `Update`. Select `photonView.IsMine`, then `Normalize`]

"In PlayerMovement.Update, the first check is photonView.IsMine. Only the owner reads the keyboard; the copy on the other screen just follows the network. I normalise the input so diagonals are not faster, and remember the facing direction for shooting. A PhotonTransformView on the prefab sends the position and smooths it on other screens."

**3:00 Damage and kills code**
[Cmd+P `Projectile`, Cmd+Shift+O `OnTriggerEnter`. Select the RPC line]

"Bullets are made with PhotonNetwork.Instantiate, so each has an owner. Every client sees the hit, but only the shooter's client sends damage, as a TakeDamage RPC to the victim's owner only. One decision, one message, so damage is never applied twice."

[Cmd+P `PlayerHealth`, Cmd+Shift+O `TakeDamage`, then Cmd+P `KillTracker`, Cmd+Shift+O `RpcReportKill`]

"The victim applies the damage. At zero it reports the kill to the master client. In RpcReportKill the master is the only one who writes kills and deaths into player custom properties, so two kills at the same moment cannot overwrite each other. Every scoreboard redraws in OnPlayerPropertiesUpdate."

**3:35 Customisations**
[Alice dashes with Shift. A power-up spawns. Bob takes the purple shield; Alice's shots do nothing. A kill shows the kill feed on both screens]

"For customisation I added a dash, power-ups, a kill feed, and hit and death effects with sound. Shift dashes with a trail both players see. Power-ups appear every eight seconds: blue for speed, yellow for rapid fire, purple for a shield. Bob takes the shield, the bubble shows on both screens and my shots do nothing. Kills appear in the same kill feed on both screens."

**4:00 Power-up code**
[Cmd+P `PowerUp`, Cmd+Shift+O `OnTriggerEnter`, then `RpcRequestPickup`. Select `RpcTarget.MasterClient` and `awarded`]

"The master spawns power-ups with InstantiateRoomObject, so they belong to the room and survive the host leaving. When one is touched, only the toucher's own client reacts, and it just asks the master. The master checks an awarded flag, so if two players touch it together only the first wins. It then tells everyone with one RPC and destroys the pick-up."

**4:25 Round end**
[Let the timer reach 0:00. Show both end screens. Press keys to show nothing moves]

"Rounds last sixty seconds, and the timer matches on both screens. At zero both clients show the end screen: Alice wins, with You Won on her screen and You Lost on Bob's, and the controls are frozen."

**4:40 Timer and winner code**
[Cmd+P `RoundManager`, Cmd+Shift+O `StartRound`, then `Update`, then `EndRound`]

"In StartRound the master saves the start time in a room property using PhotonNetwork.Time, the server clock every client is synced to. In Update each client works out the time left from that start time, so nothing is sent each frame and all timers agree. If the host leaves, the start time is still in the room, so the timer carries on. Only the master calls EndRound. It sorts players by kills, then fewest deaths, treats anyone still level as a draw, and saves the winner as a room property. Every client shows the end screen when that property changes, and works out for itself whether it won."

**5:15 Play Again**
[Alice clicks Play Again: 1 / 2 ready. Bob clicks: both back in with full health, 0 kills, new timer. Then Cmd+P `PlayAgainController`, Cmd+Shift+O `EvaluateVotes`]

"Play Again is a vote that I designed myself. I click it and it shows one of two ready. When Bob votes, we both restart with full health, zero kills and a new timer. In EvaluateVotes the master only restarts when every connected player has voted and there are at least two players. A vote is fairer than a host-only button, and if the host leaves, the new master simply keeps counting."

**5:50 One player left**
[Let a round end. Bob clicks Leave. Alice's Play Again is greyed with "Not enough players". Alice clicks Leave, then Quit]

"If Bob leaves, my Play Again is disabled and the game tells me there are not enough players, so I return to the lobby. The same check ends a round early if someone quits mid-match."

**6:05 Wrap-up**
[Both windows back on the lobby]

"To sum up: Photon relays the messages, the owner of each object controls it, and the master client is the referee for scores, time and the winner. That keeps the whole match in sync, from the lobby to Play Again. Thanks for watching."
