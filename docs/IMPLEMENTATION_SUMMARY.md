# Implementation Summary: Customisations

**Game:** top-down multiplayer arena shooter, Unity 6.0 LTS and Photon PUN 2. Four customisations make
rounds more engaging. Each one follows the same rule as the core game: **one client has authority over
each decision, and everyone else only displays it.**

## 1. Networked power-ups (Speed Boost, Rapid Fire, Shield)
A spinning cube appears every 8 seconds, two at most. Its colour shows its type. Speed Boost gives 1.7x
movement for 8 s, Rapid Fire gives 3x fire rate for 8 s, and Shield blocks all damage for 6 s with a bubble
that every player can see. The **master client** spawns them with `PhotonNetwork.InstantiateRoomObject`, so a
pick-up belongs to the room and survives the host leaving. When a player touches one, only that player's
own client asks the master for it. The master awards it once, so two players can never both collect it.

```csharp
private void OnTriggerEnter(Collider other)
{
    PlayerPowerUps player = other.GetComponentInParent<PlayerPowerUps>();
    if (player == null || !player.photonView.IsMine || awarded) return;
    photonView.RPC(nameof(RpcRequestPickup), RpcTarget.MasterClient, player.photonView.ViewID);
}

[PunRPC]
public void RpcRequestPickup(int playerViewId)
{
    if (!PhotonNetwork.IsMasterClient || awarded) return;   // first request wins
    awarded = true;
    PowerUpSpawner.Instance.AwardPowerUp(playerViewId, Type); // RPC to All: apply effect
    PhotonNetwork.Destroy(gameObject);
}
```
[SCREENSHOT: both clients side by side, one player inside the purple shield bubble, the HUD label "Shield 5s" above its health bar, and a yellow Rapid Fire cube spinning nearby.]

## 2. Kill feed
"Alice eliminated Bob" appears in the top-right corner of every screen and fades after 5 seconds. The **master**
sends the announcement, because the master is the only client that knows a kill counted. A kill after the
final whistle is rejected there, so it never appears in the feed.

```csharp
// KillTracker.RpcReportKill, master only, after updating the kill and death properties:
string killerName = killer != null ? killer.NickName : "Someone";
string victimName = victim != null ? victim.NickName : "someone";
photonView.RPC(nameof(RpcAnnounceKill), RpcTarget.All, killerName, victimName);

[PunRPC]
public void RpcAnnounceKill(string killerName, string victimName)
{
    KillAnnounced?.Invoke(killerName, victimName);   // KillFeedUI listens to this event
}
```
[SCREENSHOT: top-right of both windows showing the same two kill feed lines under the scoreboard.]

## 3. Dash with cooldown UI
Left Shift dashes the player forward at 30 units per second for 0.15 s. A blue trail follows the player, and
the DASH bar above the health bar refills over the 3-second cooldown. The movement needs no extra network
message, because `PhotonTransformView` already syncs position. Only the trail and sound are sent, to Others,
since the owner has already played them.

```csharp
private void StartDash()
{
    dashDirection = FacingDirection;
    dashTimeLeft = dashDuration;
    DashCooldownRemaining = dashCooldown;   // drives the HUD bar
    PlayDashEffect();                         // local trail and whoosh
    photonView.RPC(nameof(RpcDashEffect), RpcTarget.Others);
}
```
[SCREENSHOT: one player mid-dash with the blue trail visible in BOTH windows, and the half-filled DASH bar in the dasher's HUD.]

## 4. Hit and death feedback with synthesised audio
Projectiles burst into orange sparks on walls and players. Each client plays these locally, because every
client already simulates the projectile. A death shows a red explosion on every screen. Only the victim
knows it died, so it sends one RPC just before `PhotonNetwork.Destroy`. Taking damage flashes your screen
red. Shots, hits, deaths, pick-ups, dashes and the round end each have a sound. `SoundBank` builds every
sound in code from simple waves, so the project contains no audio files and stays small (criterion 10).

```csharp
private void Die(int killerActorNumber)
{
    IsDead = true;
    KillTracker.Instance.ReportKill(killerActorNumber, photonView.OwnerActorNr);
    GameManager.Instance.HandleLocalPlayerDied();
    photonView.RPC(nameof(RpcDeathEffect), RpcTarget.All, transform.position); // before Destroy
    PhotonNetwork.Destroy(gameObject);
}
```
[SCREENSHOT: the red death burst and orange hit sparks in one window, the red damage flash at the screen edges in the other.]
