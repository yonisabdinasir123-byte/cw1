using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Master-client authority for power-ups: spawns one every few seconds at a free spawn point
/// (up to a maximum), awards pick-ups to players through an RPC to all clients, and clears
/// the arena when a round restarts. If the master leaves, the new master simply continues
/// because the logic runs on whoever IsMasterClient is true for.
/// </summary>
public class PowerUpSpawner : MonoBehaviourPunCallbacks
{
    public static PowerUpSpawner Instance { get; private set; }

    [SerializeField, Tooltip("Positions where power-ups may appear.")]
    private Transform[] spawnPoints;

    [SerializeField, Tooltip("Seconds between spawn attempts.")]
    private float spawnInterval = 8f;

    [SerializeField, Tooltip("Maximum power-ups in the arena at once.")]
    private int maxActive = 2;

    private float timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.RoundStarted += HandleRoundStarted;
        }
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.RoundStarted -= HandleRoundStarted;
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || RoundManager.Instance == null || !RoundManager.Instance.IsRoundActive)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            TrySpawn();
        }
    }

    /// <summary>Master only: spawns a random power-up at a spawn point that is currently empty.</summary>
    private void TrySpawn()
    {
        PowerUp[] existing = FindObjectsByType<PowerUp>(FindObjectsSortMode.None);
        if (existing.Length >= maxActive || spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        // Try a few random points until one has no power-up on it.
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            bool occupied = false;
            foreach (PowerUp powerUp in existing)
            {
                if (Vector3.Distance(powerUp.transform.position, point.position) < 1f)
                {
                    occupied = true;
                    break;
                }
            }
            if (occupied)
            {
                continue;
            }

            PowerUpType type = (PowerUpType)Random.Range(0, 3);
            object[] data = { (int)type };
            // A room object belongs to the room, not to this player, so it survives the master
            // leaving and control passes to the new master automatically.
            PhotonNetwork.InstantiateRoomObject(NetworkKeys.PowerUpPrefab, point.position, Quaternion.identity, 0, data);
            return;
        }
    }

    /// <summary>Master only: tells every client that a player picked up a power-up.</summary>
    public void AwardPowerUp(int playerViewId, PowerUpType type)
    {
        // Sent from this long-lived view rather than the pick-up's own view, which is about to be destroyed.
        photonView.RPC(nameof(RpcApplyPowerUp), RpcTarget.All, playerViewId, (int)type);
    }

    /// <summary>RPC on all clients: apply the effect to that player's local copy so visuals match everywhere.</summary>
    [PunRPC]
    public void RpcApplyPowerUp(int playerViewId, int type)
    {
        PhotonView view = PhotonView.Find(playerViewId);
        if (view == null)
        {
            return;
        }

        PlayerPowerUps target = view.GetComponent<PlayerPowerUps>();
        if (target != null)
        {
            target.Apply((PowerUpType)type);
        }
        if (view.IsMine)
        {
            AudioManager.Play(SoundId.PickUp);
        }
    }

    private void HandleRoundStarted()
    {
        timer = 0f;
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }
        // Only the owner or the master may destroy a networked object; we are the master.
        foreach (PowerUp powerUp in FindObjectsByType<PowerUp>(FindObjectsSortMode.None))
        {
            PhotonNetwork.Destroy(powerUp.gameObject);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        timer = 0f;
    }
}
