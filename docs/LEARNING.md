# Learning notes

Read this alongside the code. Each script has: what it is for, its key methods, how its data
travels over the network and why, and questions you may be asked with model answers.
Phase sections are added as the project grows. The network tables and the flow diagram at the
end are updated every phase.

## Big ideas you should be able to explain

- **Photon PUN 2 is client to cloud.** There is no server we write. Every player's game is a
  client connected to Photon's cloud, which relays messages. One client is the **master client**
  and takes decisions that need a single referee (scores, later the timer and winner).
- **PhotonView** is the identity card of a networked object. It has an ID that is the same on
  every client and an **owner** (the player who created it). `photonView.IsMine` is true only
  on the owner's machine. That is how a script knows whether to read input or just display.
- **Three ways data moves**, and when we use each:
  1. **Serialize view** (`IPunObservable`, roughly 10 times a second, unreliable): for values that
     change constantly and where the latest value is all that matters. Position and health.
  2. **RPC** (remote procedure call, reliable, ordered per sender): for events that must arrive
     exactly once. "You took 25 damage", "player 2 killed player 1".
  3. **Custom properties** (on a `Player` or the `Room`, stored by the server): for state that
     late readers and new master clients must be able to look up. Kill counts, later the timer.

## Phase 1 scripts

### NetworkKeys (Assets/Scripts/Network/NetworkKeys.cs)
Purpose: one static class holding every string both clients must agree on: property keys,
prefab names and scene names. A typo in a string would silently break sync, a typo in a
constant will not compile.

### GameTags (Assets/Scripts/Game/GameTags.cs)
Purpose: the tag and layer names, for the same reason.

### LobbyManager (Assets/Scripts/Network/LobbyManager.cs)
Purpose: every lobby network action and every Photon callback the menu cares about. It drives
`LobbyUI` but never touches widgets itself.
Key methods:
- `Start`: sets `AutomaticallySyncScene`, raises `SendRate` / `SerializationRate` to 30 / 15 for
  smoother remote movement, and if we came back from a match, rejoins the lobby.
- `Connect(nickname)`: stores `PhotonNetwork.NickName`, then `ConnectUsingSettings()`.
- `OnConnectedToMaster`: calls `PhotonNetwork.JoinLobby()`. Without a lobby there is no room list.
- `OnRoomListUpdate(List<RoomInfo>)`: Photon sends **deltas**, not the whole list. Rooms that were
  removed arrive with `RemovedFromList = true`. We keep a `Dictionary<string, RoomInfo>` keyed by
  room name, apply each delta, drop rooms that are closed, hidden or empty, and redraw from the dictionary.
- `CreateRoom(name, maxPlayers)`: `PhotonNetwork.CreateRoom` with `RoomOptions` (max players, visible, open).
- `JoinRoom(name)`, `QuickPlay()`, `LeaveRoom()`: thin wrappers with on-screen status.
- `StartGame()`: master only and only with 2+ players. Sets `IsOpen = false` and `IsVisible = false`
  so nobody joins mid-match, then `PhotonNetwork.LoadLevel("Game")`.
- `OnJoinedRoom`: sets our kills and deaths to 0 and shows the Room panel.
- `OnPlayerEnteredRoom`, `OnPlayerLeftRoom`, `OnMasterClientSwitched`: redraw the player list and
  re-evaluate whether Start is clickable.
- `OnCreateRoomFailed`, `OnJoinRoomFailed`, `OnJoinRandomFailed`, `OnDisconnected`: human-readable
  status text instead of a silent log.
- `Quit()`: disconnects, then `Application.Quit()` (stops Play mode in the Editor).
Questions:
- *Why a dictionary for the room list?* Because the callback is incremental. If we rebuilt the UI
  from each callback's list alone, we would lose rooms that did not change. The dictionary is the
  full picture; the callback only patches it.
- *Why close the room when the game starts?* A player joining mid-round would miss the round
  start, have no spawn and break the "full game for all players" criterion. Closing also removes the
  room from everyone else's list.
- *Why can only the master client start?* One client must be the referee for scene loading and,
  later, the timer. Photon already elects a master, so we reuse it.
- *What does the player see if a room name is taken?* `OnCreateRoomFailed` fires with Photon's
  message and the status line shows it. The lobby panel stays open so they can retry.

### LobbyUI (Assets/Scripts/UI/LobbyUI.cs)
Purpose: the three panels (Connect, Lobby, Room) and their widgets. Buttons call `LobbyManager`.
Key methods: `ShowConnectPanel`, `ShowLobbyPanel`, `ShowRoomPanel`, `SetStatus`,
`RefreshRoomList(rooms)` (one button per room, cloned from an inactive template),
`RefreshPlayerList()` (names with "(host)" and "(you)", Start button interactable only for the
master with 2+ players, hint text otherwise). The nickname is remembered with `PlayerPrefs`.
Questions:
- *Why clone a template instead of a prefab?* Fewer references to wire by hand and the template
  lives in the same scene, so it cannot go missing.
- *Why is the Start button sometimes greyed out?* Either you are not the host or fewer than two
  players are in the room. The hint text under it says which.

### GameManager (Assets/Scripts/Game/GameManager.cs)
Purpose: per-client coordinator in the Game scene: spawns the local player, respawns it after a
delay, and returns to the menu after leaving.
Key methods:
- `Start`: bounces to the menu if we are not in a room, otherwise `SpawnLocalPlayer`.
- `SpawnLocalPlayer`: `PhotonNetwork.Instantiate("Player", ...)` at the spawn point farthest from enemies.
- `HandleLocalPlayerDied`: starts the respawn countdown coroutine.
- `RespawnAfterDelay`: counts down, then respawns only if `RoundActive` is still true.
- `OnLeftRoom`: loads the menu scene.
Network: `PhotonNetwork.Instantiate` sends an instantiate event to all clients, each of which
loads the prefab from Resources and gives it a PhotonView with the same ID and our ownership.
Questions:
- *Why does each client spawn only its own player?* So the PhotonView owner is the player
  controlling it. Ownership drives `IsMine`, which gates input and authority.
- *Why pick the farthest spawn?* To avoid spawning next to the enemy who just killed you.

### PlayerMovement (Assets/Scripts/Player/PlayerMovement.cs)
Purpose: 8-direction movement from WASD or arrows with normalised diagonals, and a remembered
facing direction for shooting.
Key methods:
- `Update` (owner only): reads `Input.GetAxisRaw`, normalises, updates `FacingDirection`, rotates the body.
- `FixedUpdate` (owner only): sets `Rigidbody.linearVelocity` so walls block the player physically.
- `Start`: on remote copies makes the Rigidbody kinematic so physics does not fight the network position.
Network: position and rotation are sent by the `PhotonTransformView` component (serialize view).
It interpolates on remote clients, which hides the gaps between packets.
Questions:
- *Why normalise the input?* (1, 0, 1) has length 1.41, so diagonals would be 41% faster.
- *Why velocity and not transform.position?* Moving the transform teleports through walls;
  velocity lets the physics engine resolve collisions with the arena.
- *Why PhotonTransformView and not an RPC per move?* Movement changes every frame; a
  lossy, frequent stream is cheaper and the latest value is all that matters.

### PlayerShooting (Assets/Scripts/Player/PlayerShooting.cs)
Purpose: fire a projectile in the facing direction with a cooldown.
Key methods: `Update` checks the key and cooldown; `Fire` calls `PhotonNetwork.Instantiate`
with the direction as instantiation data.
Network: instantiate, not RPC, because the projectile must have an **owner** so that only the
shooter decides hits and so `PhotonNetwork.Destroy` is allowed from one well-defined client.
Questions:
- *Why send the direction as instantiation data?* Every client then simulates the straight
  flight locally and no position needs streaming. Cheaper and smooth.
- *Why a cooldown?* Prevents spam that would flood the network and make the game unfair.

### Projectile (Assets/Scripts/Combat/Projectile.cs)
Purpose: straight-line bullet; applies damage once, ignores its shooter, dies on walls and after a lifetime.
Key methods:
- `Start`: reads the direction from `photonView.InstantiationData`; owner starts the lifetime timer.
- `FixedUpdate`: moves with `Rigidbody.MovePosition` on every client.
- `OnTriggerEnter`: on a player, if owner, sends `TakeDamage` as an RPC to the victim's owner and destroys itself;
  non-owners hide their copy and wait for the network destroy. On arena geometry, same pattern.
Network: RPC addressed to one player (`victim.photonView.Owner`), then `PhotonNetwork.Destroy`.
Questions:
- *Why does only the owner apply damage?* Each client sees the hit at slightly different times.
  If all of them sent damage, the victim would be hit several times. One authority, one RPC.
- *Why can a non-owner not destroy the projectile?* Photon only lets the owner (or master)
  destroy a networked object. Non-owners hide it locally and the owner's destroy arrives shortly after.

### PlayerHealth (Assets/Scripts/Player/PlayerHealth.cs)
Purpose: owner-authoritative hit points, death and kill reporting.
Key methods:
- `TakeDamage` [PunRPC]: runs only on the owner; subtracts, raises `HealthChanged`, calls `Die` at 0.
- `OnPhotonSerializeView`: owner writes health, others read it, so health bars match everywhere.
- `Die`: reports the kill to `KillTracker`, tells `GameManager` to respawn, calls `PhotonNetwork.Destroy`.
Network: RPC in (damage), serialize view out (health), destroy (death).
Questions:
- *Why does the victim decide that it died, not the shooter?* The victim's client is the only one
  with the true health value and the only one allowed to destroy its own object.
- *What stops a double death?* `IsDead` is set before anything else, so a second RPC in the same
  frame returns early.

### KillTracker (Assets/Scripts/Network/KillTracker.cs)
Purpose: the master client's scorekeeper.
Key methods:
- `ReportKill`: victim calls it; sends `RpcReportKill` with `RpcTarget.MasterClient`.
- `RpcReportKill` [PunRPC]: master increments kills for the killer and deaths for the victim in its
  own dictionary and publishes them with `SetCustomProperties`.
- `OnMasterClientSwitched`: a new master rebuilds its dictionary from the published properties.
- `GetKills`, `GetDeaths`: static readers for the UI.
Network: RPC to master, then player custom properties.
Questions:
- *Why not let each player increment their own kill count?* Only the victim knows it died, and
  the killer would have to trust a message anyway. Funnelling through one writer (master) also
  prevents two simultaneous kills from overwriting each other.
- *Why keep a dictionary if the properties exist?* After `SetCustomProperties`, the local cache
  updates only when the server echoes the change. Two kills within that window would both read
  the old value. The dictionary is the master's immediate truth.
- *What if the master leaves?* Photon picks a new master and `OnMasterClientSwitched` fires on
  every client. The new one rebuilds from properties, which the server kept.

### RoundManager (Assets/Scripts/Game/RoundManager.cs)
Purpose: the synchronised round timer and the winner decision. Master writes, everyone reads.
Key methods:
- `StartRound` (master only): writes room properties `rs` = `PhotonNetwork.Time`, `rd` = duration,
  `st` = Playing, and clears the winner keys. `PhotonNetwork.Time` is Photon's server clock, which
  every client keeps in sync, so one start time is enough for everybody.
- `Update`: every client computes `remaining = duration - (PhotonNetwork.Time - startTime)`. Nothing
  is sent per frame. The master, and only the master, calls `EndRound` when it reaches zero, guarded
  by `endRequested` so it fires once.
- `EndRound` (master only): sorts players by kills descending, then deaths ascending. Every player
  with the same kills and deaths as the top player is a winner. One winner = win, several = draw.
  Publishes `st` = Ended, `w` = winner actor numbers (int array) and `wn` = display names.
- `OnRoomPropertiesUpdate`: when `st` changes, each client calls `HandleRoundStarted` or
  `HandleRoundEnded`. The end handler freezes local input, blocks respawning and raises `RoundEnded`
  with a `RoundResult` (names, draw flag, did I win).
Network: room custom properties, not RPC, on purpose. Properties are stored by the server, so a
client that is slow to load, or a new master after the old one quits, can always read the current
truth. An RPC is a one-off message; if you missed it, it is gone.
Questions:
- *How do all timers agree without sending the time?* They all subtract the same stored start
  time from the same server clock. Only the start time crosses the network, once.
- *What happens to the timer if the master client leaves?* Nothing. The start time lives in the
  room, not on the master. The new master simply becomes the one allowed to end the round.
- *How are draws handled?* Tie-break rule: most kills, then fewest deaths. Players still level
  after that are all winners, the screen says "Draw: A and B", and each of them sees "Draw!" while
  everyone else sees "You Lost".
- *What stops a kill in the final frame from changing the result?* `KillTracker.RpcReportKill`
  and `PlayerHealth.TakeDamage` both return early once `IsRoundActive` is false, and the master
  reads the scores at the moment it ends the round.

### EndScreenUI and RoundTimerUI
`RoundTimerUI` formats `RoundManager.RemainingSeconds` as m:ss and turns red under ten seconds.
`EndScreenUI` subscribes to `RoundEnded`, writes the winner's name in large text, the personal
line ("You Won!", "You Lost" or "Draw!"), the final scoreboard via `ScoreboardUI.BuildScoreboardText`,
and shows the Leave button. `RoundStarted` hides it again, which Play Again will use.

### ScoreboardUI, HudUI, CameraFollow, PlayerNameTag
UI reads, never writes. `ScoreboardUI` redraws on `OnPlayerPropertiesUpdate`, join and leave.
`HudUI` hooks the local `PlayerHealth` each time one spawns via the static `LocalPlayerSpawned`
event, so it survives the player being destroyed and recreated. `CameraFollow` does the same for
the camera. `PlayerNameTag` displays `photonView.Owner.NickName`.

## Network reference tables

### RPCs

| Method | Defined in | Sender | Target | Parameters | Purpose |
|---|---|---|---|---|---|
| `TakeDamage` | PlayerHealth | Projectile owner | Victim's owner only | damage, attacker actor number | Apply damage once |
| `RpcReportKill` | KillTracker | Victim's client | MasterClient | killer actor, victim actor | Increment scores (ignored after the round ends) |

### Custom properties

| Key (NetworkKeys) | Scope | Written by | Read by | Meaning |
|---|---|---|---|---|
| `k` (Kills) | Player | Master client (0 on join by the player) | ScoreboardUI, future RoundManager | Kills this round |
| `d` (Deaths) | Player | Master client (0 on join by the player) | ScoreboardUI | Deaths this round |
| `rs` (RoundStartTime) | Room | Master client | RoundManager on every client | Server time the round began |
| `rd` (RoundDuration) | Room | Master client | RoundManager | Round length in seconds |
| `st` (RoundState) | Room | Master client | RoundManager | 1 playing, 2 ended |
| `w` (Winners) | Room | Master client | RoundManager, EndScreenUI | Actor numbers of the winner(s) |
| `wn` (WinnerNames) | Room | Master client | EndScreenUI | Display names of the winner(s) |

### Serialize views

| Component | Sends | Rate | Why not RPC |
|---|---|---|---|
| PhotonTransformView | position, rotation | ~10 Hz unreliable | Changes every frame, latest value wins |
| PlayerHealth | current health | ~10 Hz on change | Keeps remote health bars right without extra messages |

## Match flow (grows each phase)

```mermaid
sequenceDiagram
    participant A as Client A (master)
    participant P as Photon Cloud
    participant B as Client B
    A->>P: ConnectUsingSettings, JoinLobby
    B->>P: ConnectUsingSettings, JoinLobby
    A->>P: CreateRoom("A's room", max 4)
    P-->>B: OnRoomListUpdate (delta) -> dictionary -> list button
    B->>P: JoinRoom("A's room")
    P-->>A: OnPlayerEnteredRoom -> Start button enabled (2 players, A is master)
    A->>P: Room IsOpen = false, IsVisible = false
    A->>P: LoadLevel("Game") sets room scene property
    P-->>B: Room scene = Game, auto load
    A->>P: Room props: rs = PhotonNetwork.Time, rd = 90, st = Playing
    P-->>B: OnRoomPropertiesUpdate -> timer runs locally
    A->>P: Instantiate Player (owner A)
    B->>P: Instantiate Player (owner B)
    P-->>A: B's player appears
    P-->>B: A's player appears
    loop every ~100 ms
        A-->>P: PhotonTransformView, health
        B-->>P: PhotonTransformView, health
    end
    A->>P: Instantiate Projectile (dir)
    P-->>B: Projectile appears, moves locally
    Note over A: Projectile owner detects hit on B
    A->>P: RPC TakeDamage -> B only
    P-->>B: TakeDamage(25)
    Note over B: Health 0, IsDead
    B->>P: RPC RpcReportKill -> master
    P-->>A: RpcReportKill(killer A, victim B)
    A->>P: SetCustomProperties(A kills+1, B deaths+1)
    P-->>A: OnPlayerPropertiesUpdate -> scoreboard
    P-->>B: OnPlayerPropertiesUpdate -> scoreboard
    B->>P: PhotonNetwork.Destroy(player B)
    Note over B: 3 s countdown
    B->>P: Instantiate Player (respawn)
    Note over A: Timer reaches 0 (master only acts)
    A->>P: Room props: st = Ended, w = [A], wn = "A"
    P-->>A: OnRoomPropertiesUpdate -> end screen "You Won!"
    P-->>B: OnRoomPropertiesUpdate -> end screen "You Lost"
```
