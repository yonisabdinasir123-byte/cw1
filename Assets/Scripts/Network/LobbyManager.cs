using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// All lobby networking in one place: connect, join the lobby, keep a cached room list,
/// create or join rooms, leave, start the game (master only) and quit. It never touches UI
/// widgets directly; it tells LobbyUI what to show. Every Photon callback that matters for the
/// menu is handled here and turned into on-screen feedback.
/// </summary>
public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField, Tooltip("The view this manager drives.")]
    private LobbyUI ui;

    // Photon sends room list changes as deltas (added, changed, removed), so we keep a dictionary
    // keyed by room name and rebuild the on-screen list from it after every update.
    private readonly Dictionary<string, RoomInfo> cachedRooms = new Dictionary<string, RoomInfo>();

    /// <summary>Minimum players before the master client may start a match.</summary>
    public const int MinPlayersToStart = 2;

    /// <summary>Set when a disconnect happens during a match, so the menu can explain why we are back.</summary>
    public static string LastDisconnectMessage;

    private void Start()
    {
        // The master client loads the Game scene; every other client follows automatically.
        PhotonNetwork.AutomaticallySyncScene = true;

        // Send position updates more often than the default (20 / 10) so remote players look
        // smoother. Still only a few kilobytes per second for a four-player room.
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 15;

        ui.ShowConnectPanel();

        if (!PhotonNetwork.IsConnected)
        {
            ui.SetBusy(false);
            ui.SetStatus(string.IsNullOrEmpty(LastDisconnectMessage) ? "Enter a nickname and press Connect" : LastDisconnectMessage);
            LastDisconnectMessage = null;
            return;
        }

        // We came back from a match. Photon returns us to the master server, then we rejoin the lobby.
        ui.SetBusy(true);
        ui.SetStatus("Returning to the lobby...");
        if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    // ---------------- Actions called by LobbyUI buttons ----------------

    /// <summary>Stores the nickname and connects to the Photon cloud, or reuses the existing connection.</summary>
    public void Connect(string nickname)
    {
        nickname = nickname.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = "Player" + Random.Range(100, 999);
        }
        PhotonNetwork.NickName = nickname;
        ui.SetBusy(true);

        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.InLobby)
            {
                ui.SetBusy(false);
                ui.ShowLobbyPanel();
            }
            else if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer)
            {
                PhotonNetwork.JoinLobby();
            }
            return;
        }

        ui.SetStatus("Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    /// <summary>Creates a visible, open room with the given name and player limit.</summary>
    public void CreateRoom(string roomName, int maxPlayers)
    {
        roomName = roomName.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = PhotonNetwork.NickName + "'s room";
        }
        maxPlayers = Mathf.Clamp(maxPlayers, MinPlayersToStart, NetworkKeys.MaxPlayersPerRoom);

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            IsVisible = true,
            IsOpen = true
        };

        ui.SetBusy(true);
        ui.SetStatus("Creating room " + roomName + "...");
        PhotonNetwork.CreateRoom(roomName, options);
    }

    /// <summary>Joins a room picked from the list.</summary>
    public void JoinRoom(string roomName)
    {
        ui.SetBusy(true);
        ui.SetStatus("Joining " + roomName + "...");
        PhotonNetwork.JoinRoom(roomName);
    }

    /// <summary>Joins any open room, or creates one if none exists.</summary>
    public void QuickPlay()
    {
        ui.SetBusy(true);
        ui.SetStatus("Looking for a room...");
        RoomOptions options = new RoomOptions { MaxPlayers = (byte)NetworkKeys.MaxPlayersPerRoom };
        PhotonNetwork.JoinRandomOrCreateRoom(roomOptions: options);
    }

    /// <summary>Leaves the current room. Photon then returns us to the master server and the lobby.</summary>
    public void LeaveRoom()
    {
        ui.SetBusy(true);
        ui.SetStatus("Leaving room...");
        PhotonNetwork.LeaveRoom();
    }

    /// <summary>
    /// Master client only: closes the room so nobody can join mid-match, hides it from the list
    /// and loads the Game scene for everyone.
    /// </summary>
    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            ui.SetStatus("Only the host can start the game");
            return;
        }
        if (PhotonNetwork.CurrentRoom.PlayerCount < MinPlayersToStart)
        {
            ui.SetStatus("Need at least " + MinPlayersToStart + " players to start");
            return;
        }

        // Stay busy: the scene is about to change, and a second click would load it twice.
        ui.SetBusy(true);
        ui.SetStatus("Starting...");
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LoadLevel(NetworkKeys.GameScene);
    }

    /// <summary>Closes the application (stops Play mode inside the Editor).</summary>
    public void Quit()
    {
        PhotonNetwork.Disconnect();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------- Photon callbacks ----------------

    public override void OnConnectedToMaster()
    {
        ui.SetStatus("Connected, region: " + PhotonNetwork.CloudRegion + ". Joining lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        cachedRooms.Clear();
        ui.RefreshRoomList(cachedRooms.Values);
        ui.SetBusy(false);
        ui.ShowLobbyPanel();
        ui.SetStatus("In lobby as " + PhotonNetwork.NickName);
    }

    /// <summary>Photon callback with room list changes. Updates the cache, then redraws the list.</summary>
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            bool gone = info.RemovedFromList || !info.IsOpen || !info.IsVisible || info.PlayerCount == 0;
            if (gone)
            {
                cachedRooms.Remove(info.Name);
            }
            else
            {
                cachedRooms[info.Name] = info;
            }
        }
        ui.RefreshRoomList(cachedRooms.Values);
    }

    public override void OnJoinedRoom()
    {
        // Every player starts a match with zero kills and deaths.
        Hashtable startingScore = new Hashtable { { NetworkKeys.Kills, 0 }, { NetworkKeys.Deaths, 0 }, { NetworkKeys.Ready, false } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(startingScore);

        ui.SetBusy(false);
        ui.ShowRoomPanel();
        ui.RefreshPlayerList();
        ui.SetStatus("In room " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ui.SetBusy(false);
        ui.SetStatus("Could not create room: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        ui.SetBusy(false);
        ui.SetStatus("Could not join room: " + message);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        ui.SetBusy(false);
        ui.SetStatus("No room found: " + message);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ui.RefreshPlayerList();
        ui.SetStatus(newPlayer.NickName + " joined");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ui.RefreshPlayerList();
        ui.SetStatus(otherPlayer.NickName + " left");
    }

    /// <summary>Photon callback: the host left, so the Start button may now belong to us.</summary>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        ui.RefreshPlayerList();
        ui.SetStatus(newMasterClient.NickName + " is now the host");
    }

    public override void OnLeftRoom()
    {
        ui.ShowConnectPanel();
        ui.SetStatus("Returning to the lobby...");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        cachedRooms.Clear();
        ui.SetBusy(false);
        ui.ShowConnectPanel();
        ui.SetStatus("Disconnected: " + cause + ". Press Connect to try again.");
    }
}
