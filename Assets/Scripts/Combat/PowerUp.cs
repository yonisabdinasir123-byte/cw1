using Photon.Pun;
using UnityEngine;

/// <summary>Kinds of power-up. The int value is what travels over the network.</summary>
public enum PowerUpType
{
    Speed = 0,
    RapidFire = 1,
    Shield = 2
}

/// <summary>
/// A pick-up in the arena. Spawned by the master client with its type as instantiation data.
/// When the owning client of a player touches it, that client asks the master to award it.
/// The master awards it once, tells everyone through PowerUpSpawner, and destroys the pick-up.
/// </summary>
public class PowerUp : MonoBehaviourPun
{
    [SerializeField, Tooltip("Degrees per second the pick-up spins.")]
    private float spinSpeed = 120f;

    [SerializeField, Tooltip("Renderer whose colour shows the type.")]
    private Renderer visual;

    public PowerUpType Type { get; private set; }

    private bool awarded;
    private float bobOffset;

    private void Start()
    {
        object[] data = photonView.InstantiationData;
        Type = data != null && data.Length > 0 && data[0] is int value ? (PowerUpType)value : PowerUpType.Speed;
        bobOffset = Random.value * 10f;

        if (visual != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            Color color = ColorFor(Type);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            visual.SetPropertyBlock(block);
        }
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        if (visual != null)
        {
            visual.transform.localPosition = new Vector3(0f, 1f + Mathf.Sin(Time.time * 3f + bobOffset) * 0.15f, 0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerPowerUps player = other.GetComponentInParent<PlayerPowerUps>();
        if (player == null || !player.photonView.IsMine || awarded)
        {
            return;
        }

        // Only the toucher's own client asks, and only the master decides, so two players
        // touching in the same frame cannot both get it.
        photonView.RPC(nameof(RpcRequestPickup), RpcTarget.MasterClient, player.photonView.ViewID);
    }

    /// <summary>RPC handled by the master: award once, broadcast through the spawner, remove the pick-up.</summary>
    [PunRPC]
    public void RpcRequestPickup(int playerViewId)
    {
        if (!PhotonNetwork.IsMasterClient || awarded)
        {
            return;
        }
        awarded = true;
        PowerUpSpawner.Instance.AwardPowerUp(playerViewId, Type);
        PhotonNetwork.Destroy(gameObject);
    }

    /// <summary>Display colour for each type, shared by the pick-up and the HUD.</summary>
    public static Color ColorFor(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.RapidFire: return new Color(1f, 0.85f, 0.2f);
            case PowerUpType.Shield: return new Color(0.6f, 0.5f, 1f);
            default: return new Color(0.2f, 0.95f, 1f);
        }
    }

    public static string NameFor(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.RapidFire: return "Rapid Fire";
            case PowerUpType.Shield: return "Shield";
            default: return "Speed Boost";
        }
    }
}
