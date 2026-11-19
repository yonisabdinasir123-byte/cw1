using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Play Again by vote. On the end screen each player sets a Ready custom property. The master
/// client counts the votes whenever a property changes or a player leaves, and restarts the round
/// once every connected player is ready and at least two players remain. Votes are player
/// properties rather than RPCs so a new master client after a switch can still count them.
/// </summary>
public class PlayAgainController : MonoBehaviourPunCallbacks
{
    public static PlayAgainController Instance { get; private set; }

    /// <summary>Fired on every client whenever the ready count or player count may have changed.</summary>
    public event Action VotesChanged;

    /// <summary>True when enough players are connected for another round.</summary>
    public bool EnoughPlayers => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount >= LobbyManager.MinPlayersToStart;

    /// <summary>
    /// True once this client has voted. The local flag covers the moment between clicking and the
    /// server echoing our property back, so the button cannot flicker back on.
    /// </summary>
    public bool LocalIsReady => votedLocally || IsReady(PhotonNetwork.LocalPlayer);

    private bool votedLocally;

    public int PlayerCount => PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0;

    public int ReadyCount
    {
        get
        {
            int count = 0;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (IsReady(player)) count++;
            }
            return count;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RoundManager.Instance.RoundStarted += HandleRoundStarted;
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.RoundStarted -= HandleRoundStarted;
        }
    }

    private void HandleRoundStarted()
    {
        votedLocally = false;
    }

    /// <summary>Called by the Play Again button. Publishes our vote.</summary>
    public void VoteReady()
    {
        if (!EnoughPlayers || LocalIsReady || RoundManager.Instance.IsRoundActive)
        {
            return;
        }
        votedLocally = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { NetworkKeys.Ready, true } });
    }

    public static bool IsReady(Player player)
    {
        return player != null
               && player.CustomProperties.TryGetValue(NetworkKeys.Ready, out object value)
               && value is bool ready && ready;
    }

    // ---------------- Photon callbacks ----------------

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(NetworkKeys.Ready))
        {
            VotesChanged?.Invoke();
            EvaluateVotes();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Fewer players: the vote may now be complete, or no longer possible at all.
        VotesChanged?.Invoke();
        EvaluateVotes();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        VotesChanged?.Invoke();
    }

    /// <summary>Photon callback: if we became master mid-end-screen, we take over counting the votes.</summary>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        VotesChanged?.Invoke();
        if (newMasterClient.IsLocal)
        {
            EvaluateVotes();
        }
    }

    /// <summary>Master client only: restart when everyone connected has voted and there are 2+ players.</summary>
    private void EvaluateVotes()
    {
        if (!PhotonNetwork.IsMasterClient || RoundManager.Instance == null || RoundManager.Instance.IsRoundActive)
        {
            return;
        }
        if (!EnoughPlayers || ReadyCount < PlayerCount)
        {
            return;
        }
        RestartRound();
    }

    /// <summary>
    /// Master client only. Clears every player's score and vote, then publishes a fresh round.
    /// Each client resets its own player and projectiles when the round-start property arrives.
    /// </summary>
    private void RestartRound()
    {
        KillTracker.Instance.ResetAllScores();

        Hashtable notReady = new Hashtable { { NetworkKeys.Ready, false } };
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            player.SetCustomProperties(notReady);
        }

        RoundManager.Instance.StartRound();
    }
}
