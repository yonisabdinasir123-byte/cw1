using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// The scorekeeper. Kills and deaths are stored in each player's custom properties so every
/// client (and any future master client) can read them, but only the master client ever
/// writes them. A victim reports its own death to the master, which increments the counters.
/// Having one writer means two kills in the same frame can never overwrite each other.
/// </summary>
public class KillTracker : MonoBehaviourPunCallbacks
{
    public static KillTracker Instance { get; private set; }

    // The master client's own copy of the scores. It is the source of truth while this client is
    // master, because custom property updates only reach the local cache after a server round trip.
    private readonly Dictionary<int, int> killsByActor = new Dictionary<int, int>();
    private readonly Dictionary<int, int> deathsByActor = new Dictionary<int, int>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            RebuildFromProperties();
        }
    }

    /// <summary>
    /// Called on the victim's client when its health reaches zero. Sends the kill to whoever is
    /// the master client right now; Photon routes it to the new master if the old one left.
    /// </summary>
    public void ReportKill(int killerActorNumber, int victimActorNumber)
    {
        photonView.RPC(nameof(RpcReportKill), RpcTarget.MasterClient, killerActorNumber, victimActorNumber);
    }

    /// <summary>RPC handled only by the master client. Increments kills and deaths and publishes them.</summary>
    [PunRPC]
    public void RpcReportKill(int killerActorNumber, int victimActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        // A kill that lands after the final whistle must not change the published result.
        if (RoundManager.Instance != null && !RoundManager.Instance.IsRoundActive)
        {
            return;
        }

        Player killer = PhotonNetwork.CurrentRoom.GetPlayer(killerActorNumber);
        Player victim = PhotonNetwork.CurrentRoom.GetPlayer(victimActorNumber);

        // A player cannot score by dying to their own projectile.
        if (killer != null && killerActorNumber != victimActorNumber)
        {
            killsByActor[killerActorNumber] = GetLocal(killsByActor, killerActorNumber) + 1;
            killer.SetCustomProperties(new Hashtable { { NetworkKeys.Kills, killsByActor[killerActorNumber] } });
        }

        if (victim != null)
        {
            deathsByActor[victimActorNumber] = GetLocal(deathsByActor, victimActorNumber) + 1;
            victim.SetCustomProperties(new Hashtable { { NetworkKeys.Deaths, deathsByActor[victimActorNumber] } });
        }
    }

    /// <summary>Resets every player's score to zero. Master client only. Used by Play Again.</summary>
    public void ResetAllScores()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        killsByActor.Clear();
        deathsByActor.Clear();

        Hashtable zero = new Hashtable { { NetworkKeys.Kills, 0 }, { NetworkKeys.Deaths, 0 } };
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            player.SetCustomProperties(zero);
        }
    }

    /// <summary>Photon callback: if we just became master, load the published scores into our local copy.</summary>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient.IsLocal)
        {
            RebuildFromProperties();
        }
    }

    /// <summary>Reads a player's kill count from their custom properties. Safe to call on any client.</summary>
    public static int GetKills(Player player)
    {
        return GetProperty(player, NetworkKeys.Kills);
    }

    /// <summary>Reads a player's death count from their custom properties. Safe to call on any client.</summary>
    public static int GetDeaths(Player player)
    {
        return GetProperty(player, NetworkKeys.Deaths);
    }

    private static int GetProperty(Player player, string key)
    {
        if (player != null && player.CustomProperties.TryGetValue(key, out object value) && value is int number)
        {
            return number;
        }
        return 0;
    }

    private void RebuildFromProperties()
    {
        killsByActor.Clear();
        deathsByActor.Clear();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            killsByActor[player.ActorNumber] = GetKills(player);
            deathsByActor[player.ActorNumber] = GetDeaths(player);
        }
    }

    private static int GetLocal(Dictionary<int, int> table, int actorNumber)
    {
        return table.TryGetValue(actorNumber, out int value) ? value : 0;
    }
}
