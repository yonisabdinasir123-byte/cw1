using System;
using System.Collections;
using Photon.Pun;
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

    /// <summary>True while players are allowed to spawn and fight. The round manager turns this off at the end.</summary>
    public bool RoundActive { get; private set; } = true;

    /// <summary>Fired every frame during the respawn countdown with the seconds remaining, then once with 0.</summary>
    public event Action<float> RespawnCountdown;

    private Coroutine respawnRoutine;

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
    }

    /// <summary>Leaves the room. OnLeftRoom then returns us to the menu.</summary>
    public void LeaveGame()
    {
        PhotonNetwork.LeaveRoom();
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
