using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Phase 1 connection flow for the main menu: connects to the Photon cloud, lets the player
/// choose a nickname and joins or creates a room with one click. The full lobby (room list,
/// create and join by name) replaces the quick-play button in Phase 2, but the connection
/// callbacks in this class stay the same.
/// </summary>
public class NetworkLauncher : MonoBehaviourPunCallbacks
{
    [SerializeField, Tooltip("Text field where the player types a nickname.")]
    private InputField nicknameInput;

    [SerializeField, Tooltip("Button that joins or creates a room.")]
    private Button playButton;

    [SerializeField, Tooltip("Label that shows the current connection state.")]
    private Text statusText;

    private void Start()
    {
        // The master client loads the Game scene and every other client follows automatically.
        PhotonNetwork.AutomaticallySyncScene = true;

        playButton.onClick.AddListener(OnPlayClicked);
        playButton.interactable = false;

        if (!PhotonNetwork.IsConnected)
        {
            SetStatus("Connecting to Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer)
        {
            // Returning from a game: we are already connected, so just enable the button.
            OnConnectedToMaster();
        }
        else
        {
            // Returning from a game and still switching servers; OnConnectedToMaster will fire.
            SetStatus("Returning to the master server...");
        }
    }

    /// <summary>
    /// Called by the Play button. Stores the nickname and asks Photon for any open room,
    /// creating one if none exists.
    /// </summary>
    public void OnPlayClicked()
    {
        string nickname = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = "Player" + Random.Range(100, 999);
        }

        PhotonNetwork.NickName = nickname;
        playButton.interactable = false;
        SetStatus("Looking for a room...");

        RoomOptions options = new RoomOptions { MaxPlayers = NetworkKeys.MaxPlayersPerRoom };
        PhotonNetwork.JoinRandomOrCreateRoom(roomOptions: options);
    }

    /// <summary>Photon callback: we reached the master server and can create or join rooms.</summary>
    public override void OnConnectedToMaster()
    {
        SetStatus("Connected to Photon, region: " + PhotonNetwork.CloudRegion);
        playButton.interactable = true;
    }

    /// <summary>Photon callback: we are inside a room. The master client loads the Game scene.</summary>
    public override void OnJoinedRoom()
    {
        // Every player starts the round with zero kills and deaths.
        Hashtable startingScore = new Hashtable
        {
            { NetworkKeys.Kills, 0 },
            { NetworkKeys.Deaths, 0 }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(startingScore);

        SetStatus("Joined room " + PhotonNetwork.CurrentRoom.Name);

        if (PhotonNetwork.IsMasterClient)
        {
            // Only one client may load the level; AutomaticallySyncScene makes the others follow.
            PhotonNetwork.LoadLevel(NetworkKeys.GameScene);
        }
    }

    /// <summary>Photon callback: no random room was found and creating one also failed.</summary>
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        SetStatus("Could not join a room: " + message);
        playButton.interactable = true;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetStatus("Could not create a room: " + message);
        playButton.interactable = true;
    }

    /// <summary>Photon callback: the connection dropped or was refused.</summary>
    public override void OnDisconnected(DisconnectCause cause)
    {
        SetStatus("Disconnected: " + cause);
        playButton.interactable = false;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
