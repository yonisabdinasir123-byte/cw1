using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The in-game overlay for the local player: health bar, respawn countdown, room info and a
/// Leave button. It listens to the local PlayerHealth each time one spawns, so it survives
/// the player object being destroyed and recreated on respawn.
/// </summary>
public class HudUI : MonoBehaviour
{
    [SerializeField, Tooltip("Rect whose anchorMax.x is scaled from 0 to 1 with health.")]
    private RectTransform healthFill;

    [SerializeField, Tooltip("Label showing current / max health.")]
    private Text healthText;

    [SerializeField, Tooltip("Centre label used for the respawn countdown.")]
    private Text respawnText;

    [SerializeField, Tooltip("Label showing room name, player count and ping.")]
    private Text roomInfoText;

    [SerializeField, Tooltip("Button that leaves the room.")]
    private Button leaveButton;

    private PlayerHealth localHealth;
    private float infoRefreshTimer;

    private void OnEnable()
    {
        PlayerHealth.LocalPlayerSpawned += HandleLocalPlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerHealth.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RespawnCountdown -= HandleRespawnCountdown;
        }
    }

    private void Start()
    {
        respawnText.text = string.Empty;
        leaveButton.onClick.AddListener(() => GameManager.Instance.LeaveGame());
        GameManager.Instance.RespawnCountdown += HandleRespawnCountdown;
    }

    private void Update()
    {
        infoRefreshTimer += Time.deltaTime;
        if (infoRefreshTimer < 0.5f || !PhotonNetwork.InRoom)
        {
            return;
        }
        infoRefreshTimer = 0f;
        roomInfoText.text = string.Format("Room: {0}   Players: {1}   Ping: {2} ms",
            PhotonNetwork.CurrentRoom.Name, PhotonNetwork.CurrentRoom.PlayerCount, PhotonNetwork.GetPing());
    }

    private void HandleLocalPlayerSpawned(PlayerHealth health)
    {
        if (localHealth != null)
        {
            localHealth.HealthChanged -= HandleHealthChanged;
        }
        localHealth = health;
        localHealth.HealthChanged += HandleHealthChanged;
        HandleHealthChanged(health.CurrentHealth, health.MaxHealth);
        respawnText.text = string.Empty;
    }

    private void HandleHealthChanged(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        healthFill.anchorMax = new Vector2(ratio, 1f);
        healthText.text = current + " / " + max;
    }

    private void HandleRespawnCountdown(float secondsLeft)
    {
        respawnText.text = secondsLeft > 0f ? "Respawning in " + Mathf.CeilToInt(secondsLeft) : string.Empty;
    }
}
