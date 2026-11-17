using System.Collections;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// A bullet that flies in a straight line. Every client moves its own copy from the direction
/// passed as instantiation data, so no position data is streamed. Only the shooter's client
/// (the PhotonView owner) decides hits and sends damage, so damage is never applied twice.
/// The shooter's own body is ignored. Hitting a wall or a player, or running out of lifetime,
/// destroys the projectile on all clients.
/// </summary>
public class Projectile : MonoBehaviourPun
{
    [SerializeField, Tooltip("Travel speed in units per second.")]
    private float speed = 22f;

    [SerializeField, Tooltip("Seconds before the projectile disappears if it hits nothing.")]
    private float lifetime = 2f;

    [SerializeField, Tooltip("Damage dealt to a player on hit.")]
    private int damage = 25;

    private Rigidbody body;
    private Vector3 direction;
    private bool finished;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        object[] data = photonView.InstantiationData;
        direction = data != null && data.Length > 0 && data[0] is Vector3 sent ? sent : transform.forward;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        transform.forward = direction;

        if (photonView.IsMine)
        {
            StartCoroutine(DestroyAfterLifetime());
        }
    }

    private void FixedUpdate()
    {
        body.MovePosition(body.position + direction * (speed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (finished)
        {
            return;
        }

        PlayerHealth victim = other.GetComponentInParent<PlayerHealth>();
        if (victim != null)
        {
            // Never hurt the player who fired it.
            if (victim.photonView.OwnerActorNr == photonView.OwnerActorNr)
            {
                return;
            }

            if (photonView.IsMine)
            {
                // Address the RPC to the victim's owner only: that client is the authority on its health.
                victim.photonView.RPC(nameof(PlayerHealth.TakeDamage), victim.photonView.Owner, damage, photonView.OwnerActorNr);
                Finish();
            }
            else
            {
                HideUntilNetworkDestroy();
            }
            return;
        }

        if (other.CompareTag(GameTags.ArenaTag))
        {
            if (photonView.IsMine)
            {
                Finish();
            }
            else
            {
                HideUntilNetworkDestroy();
            }
        }
    }

    private IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);
        if (!finished)
        {
            Finish();
        }
    }

    /// <summary>Owner only: removes the projectile on every client.</summary>
    private void Finish()
    {
        finished = true;
        PhotonNetwork.Destroy(gameObject);
    }

    /// <summary>
    /// Non-owners cannot call PhotonNetwork.Destroy, so they hide their copy and wait for the
    /// owner's destroy message to arrive a few milliseconds later.
    /// </summary>
    private void HideUntilNetworkDestroy()
    {
        finished = true;
        foreach (Renderer part in GetComponentsInChildren<Renderer>())
        {
            part.enabled = false;
        }
        foreach (Collider part in GetComponentsInChildren<Collider>())
        {
            part.enabled = false;
        }
    }
}
