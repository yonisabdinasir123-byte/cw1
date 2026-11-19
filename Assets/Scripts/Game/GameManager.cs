using System;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs the Game scene on each client: spawns the local player, picks spawn points away from
/// enemies, handles respawning after a delay and returns to the menu when we leave the room.
/// Each client only ever spawns its own player; Photon creates the copies on other clients.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [SerializeField, Tooltip("Possible spawn positions. The one farthest from other players is chosen.")]
    private Transform[] spawnPoints;

    [SerializeField, Tooltip("Seconds between dying and respawning.")]
    private float respawnDelay = 3f;

    /// <summary>True while players are allowed to spawn and fight. The round manager turns this on and off.</summary>
    public bool RoundActive { get; private set; }

    /// <summary>Fired every frame during the respawn countdown with the seconds remaining, then once with 0.</summary>
    public event Action<float> RespawnCountdown;

    private Coroutine respawnRoutine;
    private bool leaving;

    private void Awake()
    {
        Instance = this;

        // Projectiles should pass through each other instead of colliding.
        int projectileLayer = LayerMask.NameToLayer(GameTags.ProjectileLayer);
        if (projectileLayer >= 0)
        {
            Physics.IgnoreLayerCollision(projectileLayer, projectileLayer, true);
        }
    }

    private void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            // Pressing Play directly in the Game scene is not supported: go through the menu.
            SceneManager.LoadScene(NetworkKeys.MainMenuScene);
            return;
        }

        // Spawn when the round starts. If the round is already running (we loaded late, or the
        // start property arrived before this Start ran), spawn right away.
        RoundManager.Instance.RoundStarted += HandleRoundStarted;
        if (RoundManager.Instance.IsRoundActive)
        {
            HandleRoundStarted();
        }
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.RoundStarted -= HandleRoundStarted;
        }
    }

    /// <summary>
    /// Runs on every client at round start and at every Play Again restart: removes our old player
    /// and projectiles, cancels a pending respawn and spawns a fresh player at full health.
    /// </summary>
    private void HandleRoundStarted()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
            RespawnCountdown?.Invoke(0f);
        }

        foreach (PlayerHealth player in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (player.photonView.IsMine) PhotonNetwork.Destroy(player.gameObject);
        }
        foreach (Projectile projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            if (projectile.photonView.IsMine) PhotonNetwork.Destroy(projectile.gameObject);
        }

        SpawnLocalPlayer();
    }

    /// <summary>Creates this client's player object across the network at a safe spawn point.</summary>
    public void SpawnLocalPlayer()
    {
        Transform spawn = PickSpawnPoint();
        PhotonNetwork.Instantiate(NetworkKeys.PlayerPrefab, spawn.position, spawn.rotation);
    }

    /// <summary>Called by PlayerHealth on the owning client when its player dies.</summary>
    public void HandleLocalPlayerDied()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
        }
        respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    /// <summary>Allows or blocks spawning. Called by the round manager when the round starts or ends.</summary>
    public void SetRoundActive(bool active)
    {
        RoundActive = active;

        // A player who died in the last seconds should not sit behind the end screen counting down.
        if (!active && respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
            RespawnCountdown?.Invoke(0f);
        }
    }

    /// <summary>Leaves the room once, however many times the button is clicked. OnLeftRoom then returns us to the menu.</summary>
    public void LeaveGame()
    {
        if (leaving || !PhotonNetwork.InRoom)
        {
            return;
        }
        leaving = true;
        PhotonNetwork.LeaveRoom();
    }

    /// <summary>Photon callback: the connection dropped mid-match. Go back to the menu and say why.</summary>
    public override void OnDisconnected(DisconnectCause cause)
    {
        LobbyManager.LastDisconnectMessage = "Lost connection during the match (" + cause + "). Press Connect to try again.";
        SceneManager.LoadScene(NetworkKeys.MainMenuScene);
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(NetworkKeys.MainMenuScene);
    }

    private IEnumerator RespawnAfterDelay()
    {
        float remaining = respawnDelay;
        while (remaining > 0f)
        {
            RespawnCountdown?.Invoke(remaining);
            remaining -= Time.deltaTime;
            yield return null;
        }
        RespawnCountdown?.Invoke(0f);

        // No respawn once the round is over or if we are no longer in the room.
        if (RoundActive && PhotonNetwork.InRoom)
        {
            SpawnLocalPlayer();
        }
        respawnRoutine = null;
    }

    private Transform PickSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("GameManager has no spawn points assigned. Run Tools > Arena > Build Game Scene.");
            return transform;
        }

        PlayerHealth[] others = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        if (others.Length == 0)
        {
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        }

        // Choose the point whose nearest enemy is farthest away.
        Transform best = spawnPoints[0];
        float bestDistance = -1f;
        foreach (Transform point in spawnPoints)
        {
            float nearest = float.MaxValue;
            foreach (PlayerHealth other in others)
            {
                float distance = Vector3.Distance(point.position, other.transform.position);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }
            if (nearest > bestDistance)
            {
                bestDistance = nearest;
                best = point;
            }
        }
        return best;
    }
}
