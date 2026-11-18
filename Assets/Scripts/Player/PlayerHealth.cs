using System;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Owns the player's hit points. Only the owning client changes its own health: other clients
/// send damage as an RPC addressed to the owner, and the owner streams the resulting value back
/// through OnPhotonSerializeView so health bars match everywhere. When health reaches zero the
/// owner reports the kill, destroys its player object and asks GameManager to respawn it.
/// </summary>
public class PlayerHealth : MonoBehaviourPun, IPunObservable
{
    [SerializeField, Tooltip("Hit points at spawn.")]
    private int maxHealth = 100;

    /// <summary>Fired on any client when this player's health value changes. Arguments: current, max.</summary>
    public event Action<int, int> HealthChanged;

    /// <summary>Fired on the owning client when its own player has spawned. UI and camera listen to this.</summary>
    public static event Action<PlayerHealth> LocalPlayerSpawned;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            LocalPlayerSpawned?.Invoke(this);
        }
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>
    /// RPC sent by the shooter's client and delivered only to the victim's owner.
    /// Applies damage and triggers death when health hits zero.
    /// </summary>
    [PunRPC]
    public void TakeDamage(int amount, int attackerActorNumber)
    {
        if (!photonView.IsMine || IsDead)
        {
            return;
        }

        // Projectiles already in flight when the timer hits zero do nothing.
        if (RoundManager.Instance != null && !RoundManager.Instance.IsRoundActive)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
        {
            Die(attackerActorNumber);
        }
    }

    /// <summary>Streams health from the owner to every other client about ten times a second.</summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(CurrentHealth);
        }
        else
        {
            int received = (int)stream.ReceiveNext();
            if (received != CurrentHealth)
            {
                CurrentHealth = received;
                HealthChanged?.Invoke(CurrentHealth, maxHealth);
            }
        }
    }

    private void Die(int killerActorNumber)
    {
        IsDead = true;

        // The victim is the one client that knows for certain it died, so it reports the kill.
        KillTracker.Instance.ReportKill(killerActorNumber, photonView.OwnerActorNr);
        GameManager.Instance.HandleLocalPlayerDied();

        // Destroying through Photon removes the object on every client.
        PhotonNetwork.Destroy(gameObject);
    }
}
