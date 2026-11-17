using Photon.Pun;
using UnityEngine;

/// <summary>
/// Fires a projectile in the player's facing direction when the fire key is held, limited by a
/// cooldown. The projectile is created with PhotonNetwork.Instantiate so it exists on every
/// client and has a PhotonView whose owner is the shooter. Owner is what lets the projectile
/// know whose hits count and whose body to ignore.
/// </summary>
public class PlayerShooting : MonoBehaviourPun
{
    [SerializeField, Tooltip("Where projectiles appear. Should sit just outside the player's collider.")]
    private Transform muzzle;

    [SerializeField, Tooltip("Minimum seconds between shots.")]
    private float fireCooldown = 0.25f;

    [SerializeField, Tooltip("Key that fires. Left mouse button also fires.")]
    private KeyCode fireKey = KeyCode.Space;

    /// <summary>Set to false to ignore input, for example when the round has ended.</summary>
    public bool InputEnabled { get; set; } = true;

    private PlayerMovement movement;
    private float nextFireTime;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (!photonView.IsMine || !InputEnabled)
        {
            return;
        }

        bool wantsToFire = Input.GetKey(fireKey) || Input.GetMouseButton(0);
        if (wantsToFire && Time.time >= nextFireTime)
        {
            Fire();
        }
    }

    /// <summary>Spawns one projectile across the network, travelling in the facing direction.</summary>
    private void Fire()
    {
        nextFireTime = Time.time + fireCooldown;

        Vector3 direction = movement.FacingDirection;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        // The direction is sent as instantiation data so every client can move the projectile
        // locally without streaming its position every frame.
        object[] data = { direction };
        PhotonNetwork.Instantiate(NetworkKeys.ProjectilePrefab, muzzle.position, rotation, 0, data);
    }
}
