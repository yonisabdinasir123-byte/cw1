# Implementation Summary: Customisations

**CW1 Multiplayer Arena Shooter** · Unity 6.0 LTS · Photon PUN 2
Name: ____________  Student ID: ____________

I added four customisations to make rounds faster and easier to read: power-ups, a kill feed, a dash, and hit and death feedback with sound. All four follow the same rule as the core game. Each decision has one authority, either the owner of the PhotonView or the master client, and every other client only displays the result. This keeps the clients in agreement and stops anything happening twice.

## 1. Networked power-ups

Three pick-ups appear in the arena, one every 8 seconds and at most two at a time. **Speed Boost** gives 1.7x movement for 8 s, **Rapid Fire** gives a 3x fire rate for 8 s, and **Shield** blocks all damage for 6 s with a bubble every player can see.

The master client spawns them with `PhotonNetwork.InstantiateRoomObject` and passes the type as `InstantiationData`, so every client builds the same coloured cube. Because they are room objects rather than player-owned objects, they stay in the arena if the host leaves and the new master takes them over. `OnTriggerEnter` runs on every client, but only the client that owns the touching player (`photonView.IsMine`) acts: it sends `RpcRequestPickup` to `RpcTarget.MasterClient`. The master checks the `awarded` flag, so if two players touch the same cube in the same frame only the first request counts. It then broadcasts `RpcApplyPowerUp` to `RpcTarget.All` and destroys the pick-up.

![PowerUp.cs, OnTriggerEnter and RpcRequestPickup](images/code-powerup.png)

[Gameplay screenshot to add: both clients side by side, one player inside the purple shield bubble, "Shield 5s" above their health bar.]

## 2. Kill feed

Lines such as "Alice eliminated Bob" appear in the top-right corner of every screen. The last five are shown, and each fades after 5 seconds.

The master client sends the announcement because it is the only client that knows a kill was accepted. In `RpcReportKill` the master first updates the Kills and Deaths player custom properties. Then it sends `RpcAnnounceKill` to `RpcTarget.All`. A kill that lands after the timer reaches zero is rejected earlier in the same method, so it never reaches the feed. `KillFeedUI` listens to a static C# event, so the UI script does not need its own PhotonView.

![KillTracker.cs, kill feed announcement](images/code-killfeed.png)

[Gameplay screenshot to add: the top right of both windows showing the same kill feed lines under the scoreboard.]

## 3. Dash with cooldown bar

Left Shift dashes the player forward at 30 units per second for 0.15 s, with a 3-second cooldown shown as a bar above the health bar.

The dash only changes the owner's Rigidbody velocity for a few physics steps, and `PhotonTransformView` already syncs position, so the dash needs no extra position message. Only the cosmetic part is networked: `RpcDashEffect` plays the trail and sound on the other clients. It goes to `RpcTarget.Others` rather than `All`, because the owner has already played the effect locally in `StartDash`.

![PlayerMovement.cs, StartDash and RpcDashEffect](images/code-dash.png)

[Gameplay screenshot to add: one player mid-dash with the blue trail visible in both windows, and the DASH bar half full.]

## 4. Hit and death feedback with generated sound

Projectiles burst into orange sparks when they hit a wall or a player. A death shows a red explosion on every screen, and taking damage flashes your own screen red. There are sounds for shooting, hits, deaths, pick-ups, the dash and the end of the round.

Sparks need no network message, because every client already simulates each projectile from its `InstantiationData` and sees the impact itself. A death is different. Only the victim's client knows it has died, so `Die()` sends `RpcDeathEffect` to `RpcTarget.All` just before calling `PhotonNetwork.Destroy`. Photon delivers messages from one sender in order, so every client plays the explosion before the player object disappears. The sounds are generated in code by `SoundBank` from simple waveforms, so the project has no audio files, which also keeps the submission small.

![PlayerHealth.cs, RpcDeathEffect and Die](images/code-death.png)

[Gameplay screenshot to add: the red death burst and orange sparks in one window, and the red damage flash in the other.]

## Problems I ran into

- **Testing two players on one Mac.** ParrelSync would not install through the Package Manager because the Mac had no command-line git, so I used Unity's Multiplayer Play Mode instead. The second window lagged badly at first. The Editor throttles windows that are not in focus, and both were rendering at 2560x1440. Setting Interaction Mode to No Throttling and the Game views to 1920x1080 fixed it. I also raised `PhotonNetwork.SendRate` to 30 and `SerializationRate` to 15 so remote players move more smoothly.
- **Double clicks.** Clicking Create Room or Start Game quickly sent the same request twice, and Photon logged errors. The lobby panels now lock through a `CanvasGroup` from the click until Photon calls back with success or failure.
- **The Play Again button flickering.** After voting, the button became clickable again for a moment. A player's custom properties only update locally once the server echoes the change back. A local `votedLocally` flag now covers that gap.
- **The host leaving.** Power-ups were first spawned with `PhotonNetwork.Instantiate`, so they belonged to the host and vanished when the host quit. `InstantiateRoomObject` fixed that. A related problem was that if an opponent quit, the last player played alone until the timer ran out. The master now ends the round early and the end screen gives the reason.
- **Overlapping menu panels and a console warning.** All three lobby panels were saved as active, so they overlapped in the Editor. The scene now saves with only the Connect panel visible. The warning "Default GameObject Tag: Player already registered" came from adding Unity's built-in Player tag to the custom tag list. Removing it cleared the Console.
