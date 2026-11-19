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

    [Header("Customisations")]
    [SerializeField, Tooltip("Rect whose anchorMax.x shows dash readiness from 0 to 1.")]
    private RectTransform dashFill;

    [SerializeField, Tooltip("Label naming the active power-up and its time left.")]
    private Text powerUpText;

    [SerializeField, Tooltip("Full-screen image flashed red when we take damage.")]
    private Image damageFlash;

    private PlayerHealth localHealth;
    private PlayerMovement localMovement;
    private PlayerPowerUps localPowerUps;
    private float infoRefreshTimer;
    private float flashAlpha;
    private int lastHealth;

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
        leaveButton.onClick.AddListener(() =>
        {
            leaveButton.interactable = false;
            GameManager.Instance.LeaveGame();
        });
        GameManager.Instance.RespawnCountdown += HandleRespawnCountdown;
    }

    private void Update()
    {
        UpdateDashBar();
        UpdatePowerUpText();
        UpdateDamageFlash();

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
        localMovement = health.GetComponent<PlayerMovement>();
        localPowerUps = health.GetComponent<PlayerPowerUps>();
        lastHealth = health.CurrentHealth;
        localHealth.HealthChanged += HandleHealthChanged;
        HandleHealthChanged(health.CurrentHealth, health.MaxHealth);
        respawnText.text = string.Empty;
    }

    private void HandleHealthChanged(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        healthFill.anchorMax = new Vector2(ratio, 1f);
        healthText.text = current + " / " + max;

        if (current < lastHealth)
        {
            flashAlpha = 0.45f;
        }
        lastHealth = current;
    }

    private void UpdateDashBar()
    {
        if (dashFill == null) return;
        float ratio = 1f;
        if (localMovement != null && localMovement.DashCooldown > 0f)
        {
            ratio = 1f - Mathf.Clamp01(localMovement.DashCooldownRemaining / localMovement.DashCooldown);
        }
        dashFill.anchorMax = new Vector2(ratio, 1f);
    }

    private void UpdatePowerUpText()
    {
        if (powerUpText == null) return;
        if (localPowerUps != null && localPowerUps.HasActive)
        {
            powerUpText.text = PowerUp.NameFor(localPowerUps.Active) + "  " + Mathf.CeilToInt(localPowerUps.RemainingSeconds) + "s";
            powerUpText.color = PowerUp.ColorFor(localPowerUps.Active);
        }
        else
        {
            powerUpText.text = string.Empty;
        }
    }

    private void UpdateDamageFlash()
    {
        if (damageFlash == null) return;
        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.deltaTime * 1.5f);
        damageFlash.color = new Color(1f, 0.1f, 0.1f, flashAlpha);
    }

    private void HandleRespawnCountdown(float secondsLeft)
    {
        respawnText.text = secondsLeft > 0f ? "Respawning in " + Mathf.CeilToInt(secondsLeft) : string.Empty;
    }
}
