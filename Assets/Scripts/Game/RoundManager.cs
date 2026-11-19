using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Owns the round timer and the winner decision. The master client writes the round start time
/// (Photon's synchronised server clock) and the duration into room properties; every client then
/// computes the remaining time locally, so all timers agree and a master-client switch changes
/// nothing. When time runs out the master works out the winner and publishes it as a room
/// property, which every client turns into the end screen.
/// </summary>
public class RoundManager : MonoBehaviourPunCallbacks
{
    public static RoundManager Instance { get; private set; }

    [SerializeField, Tooltip("Round length in seconds. The master client's value is used for everyone.")]
    private float roundDurationSeconds = 60f;

    /// <summary>True while players may fight. False before the first round and after the timer ends.</summary>
    public bool IsRoundActive => state == NetworkKeys.RoundPlaying;

    /// <summary>Seconds left in the round, computed from the synchronised clock.</summary>
    public float RemainingSeconds { get; private set; }

    /// <summary>Fired on every client when a round starts or restarts.</summary>
    public event Action RoundStarted;

    /// <summary>Fired on every client when the round ends, with the published result.</summary>
    public event Action<RoundResult> RoundEnded;

    private int state;
    private double startTime;
    private float duration;
    private bool endRequested;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        Room room = PhotonNetwork.CurrentRoom;
        if (PhotonNetwork.IsMasterClient && !room.CustomProperties.ContainsKey(NetworkKeys.RoundState))
        {
            StartRound();
        }
        else
        {
            // Joined a room whose round is already running (or ended): read the published state.
            ReadRoomProperties(room.CustomProperties);
        }
    }

    /// <summary>
    /// Master client only. Publishes a fresh round: start time from the server clock, duration and
    /// state Playing. Every client reacts in OnRoomPropertiesUpdate, including the master itself.
    /// </summary>
    public void StartRound()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        endRequested = false;
        Hashtable props = new Hashtable
        {
            { NetworkKeys.RoundStartTime, PhotonNetwork.Time },
            { NetworkKeys.RoundDuration, roundDurationSeconds },
            { NetworkKeys.RoundState, NetworkKeys.RoundPlaying },
            { NetworkKeys.Winners, new int[0] },
            { NetworkKeys.WinnerNames, string.Empty },
            { NetworkKeys.EndReason, string.Empty }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void Update()
    {
        if (state != NetworkKeys.RoundPlaying)
        {
            return;
        }

        double elapsed = PhotonNetwork.Time - startTime;
        RemainingSeconds = Mathf.Max(0f, (float)(duration - elapsed));

        // Only the referee ends the round, and only once.
        if (RemainingSeconds <= 0f && PhotonNetwork.IsMasterClient && !endRequested)
        {
            EndRound(string.Empty);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        EndIfTooFewPlayers();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        EndIfTooFewPlayers();
    }

    /// <summary>
    /// Master only: a round cannot continue with one player, so it ends at once and the remaining
    /// player is told why. Called from both callbacks because Photon does not guarantee which of
    /// "player left" and "master switched" arrives first when the master is the one who left.
    /// </summary>
    private void EndIfTooFewPlayers()
    {
        if (PhotonNetwork.IsMasterClient && IsRoundActive && !endRequested
            && PhotonNetwork.CurrentRoom.PlayerCount < LobbyManager.MinPlayersToStart)
        {
            EndRound("Round ended early: not enough players");
        }
    }

    /// <summary>
    /// Master client only. Ranks players by kills (fewest deaths breaks a tie) and publishes the
    /// result. Several players still level on both counts means a draw between them.
    /// </summary>
    private void EndRound(string reason)
    {
        endRequested = true;

        List<Player> ranked = PhotonNetwork.PlayerList
            .OrderByDescending(KillTracker.GetKills)
            .ThenBy(KillTracker.GetDeaths)
            .ToList();

        List<Player> winners = new List<Player>();
        if (ranked.Count > 0)
        {
            Player top = ranked[0];
            foreach (Player player in ranked)
            {
                bool sameScore = KillTracker.GetKills(player) == KillTracker.GetKills(top)
                                 && KillTracker.GetDeaths(player) == KillTracker.GetDeaths(top);
                if (sameScore)
                {
                    winners.Add(player);
                }
            }
        }

        Hashtable props = new Hashtable
        {
            { NetworkKeys.RoundState, NetworkKeys.RoundEnded },
            { NetworkKeys.Winners, winners.Select(player => player.ActorNumber).ToArray() },
            { NetworkKeys.WinnerNames, JoinNames(winners.Select(player => player.NickName).ToList()) },
            { NetworkKeys.EndReason, reason }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    /// <summary>Photon callback: room properties changed on the server. This is how every client learns about start and end.</summary>
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(NetworkKeys.RoundState))
        {
            ReadRoomProperties(PhotonNetwork.CurrentRoom.CustomProperties);
        }
    }

    private void ReadRoomProperties(Hashtable props)
    {
        int previousState = state;

        if (props.TryGetValue(NetworkKeys.RoundStartTime, out object start)) startTime = (double)start;
        if (props.TryGetValue(NetworkKeys.RoundDuration, out object length)) duration = (float)length;
        if (props.TryGetValue(NetworkKeys.RoundState, out object roundState)) state = (int)roundState;

        if (state == NetworkKeys.RoundPlaying && previousState != NetworkKeys.RoundPlaying)
        {
            HandleRoundStarted();
        }
        else if (state == NetworkKeys.RoundEnded && previousState != NetworkKeys.RoundEnded)
        {
            HandleRoundEnded(props);
        }
    }

    private void HandleRoundStarted()
    {
        RemainingSeconds = duration;
        GameManager.Instance.SetRoundActive(true);
        RoundStarted?.Invoke();
    }

    private void HandleRoundEnded(Hashtable props)
    {
        RemainingSeconds = 0f;
        GameManager.Instance.SetRoundActive(false);
        SetLocalInputEnabled(false);

        int[] winners = props.TryGetValue(NetworkKeys.Winners, out object list) && list is int[] ids ? ids : new int[0];
        string names = props.TryGetValue(NetworkKeys.WinnerNames, out object text) ? (string)text : string.Empty;
        string reason = props.TryGetValue(NetworkKeys.EndReason, out object why) && why is string value ? value : string.Empty;

        RoundResult result = new RoundResult
        {
            WinnerNames = names,
            EndReason = reason,
            IsDraw = winners.Length != 1,
            LocalPlayerIsWinner = winners.Contains(PhotonNetwork.LocalPlayer.ActorNumber)
        };
        RoundEnded?.Invoke(result);
    }

    /// <summary>Freezes or frees the local player's movement and shooting.</summary>
    public static void SetLocalInputEnabled(bool enabled)
    {
        foreach (PlayerMovement movement in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
        {
            if (movement.photonView.IsMine) movement.InputEnabled = enabled;
        }
        foreach (PlayerShooting shooting in FindObjectsByType<PlayerShooting>(FindObjectsSortMode.None))
        {
            if (shooting.photonView.IsMine) shooting.InputEnabled = enabled;
        }
    }

    private static string JoinNames(List<string> names)
    {
        if (names.Count == 0) return "Nobody";
        if (names.Count == 1) return names[0];
        return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
    }
}

/// <summary>What the end screen needs to know, built on each client from the room properties.</summary>
public struct RoundResult
{
    /// <summary>"Alice", or "Alice and Bob" for a draw.</summary>
    public string WinnerNames;

    /// <summary>True when more than one player shares the top score.</summary>
    public bool IsDraw;

    /// <summary>True when this client's player is among the winners.</summary>
    public bool LocalPlayerIsWinner;

    /// <summary>Empty when the timer ran out, otherwise why the round ended early.</summary>
    public string EndReason;
}
