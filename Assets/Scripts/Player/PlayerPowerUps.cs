using System;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// The power-up currently active on this player and what it does: a speed multiplier read by
/// PlayerMovement, a fire-rate multiplier read by PlayerShooting, and a shield checked by
/// PlayerHealth. Applied on every client by the same RPC, so the shield bubble shows everywhere,
/// while the gameplay effect only matters on the owning client where input and damage are decided.
/// </summary>
public class PlayerPowerUps : MonoBehaviourPun
{
    [SerializeField, Tooltip("Visual shown while the shield is active.")]
    private GameObject shieldVisual;

    [SerializeField, Tooltip("Seconds a speed boost lasts.")]
    private float speedDuration = 8f;

    [SerializeField, Tooltip("Seconds rapid fire lasts.")]
    private float rapidFireDuration = 8f;

    [SerializeField, Tooltip("Seconds the shield lasts.")]
    private float shieldDuration = 6f;

    /// <summary>Fired when a power-up starts or ends, for the HUD.</summary>
    public event Action Changed;

    public bool HasActive { get; private set; }
    public PowerUpType Active { get; private set; }
    public float RemainingSeconds { get; private set; }

    public float SpeedMultiplier => HasActive && Active == PowerUpType.Speed ? 1.7f : 1f;
    public float FireCooldownMultiplier => HasActive && Active == PowerUpType.RapidFire ? 0.3f : 1f;
    public bool HasShield => HasActive && Active == PowerUpType.Shield;

    private void Awake()
    {
        if (shieldVisual != null) shieldVisual.SetActive(false);
    }

    /// <summary>Starts (or replaces) the active power-up.</summary>
    public void Apply(PowerUpType type)
    {
        HasActive = true;
        Active = type;
        RemainingSeconds = type == PowerUpType.Speed ? speedDuration
            : type == PowerUpType.RapidFire ? rapidFireDuration
            : shieldDuration;
        if (shieldVisual != null) shieldVisual.SetActive(type == PowerUpType.Shield);
        Changed?.Invoke();
    }

    private void Update()
    {
        if (!HasActive)
        {
            return;
        }
        RemainingSeconds -= Time.deltaTime;
        if (RemainingSeconds <= 0f)
        {
            HasActive = false;
            RemainingSeconds = 0f;
            if (shieldVisual != null) shieldVisual.SetActive(false);
            Changed?.Invoke();
        }
    }
}
