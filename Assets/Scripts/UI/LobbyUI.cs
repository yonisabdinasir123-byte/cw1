using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The menu's three panels (Connect, Lobby, Room) and every widget on them. It only draws and
/// forwards button clicks to LobbyManager; it never calls Photon itself, so networking logic
/// stays in one place.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField, Tooltip("Manager that receives button actions.")]
    private LobbyManager manager;

    [SerializeField, Tooltip("Status line visible on every panel.")]
    private Text statusText;

    [Header("Connect panel")]
    [SerializeField] private GameObject connectPanel;
    [SerializeField] private InputField nicknameInput;
    [SerializeField] private Button connectButton;

    [Header("Lobby panel")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private InputField roomNameInput;
    [SerializeField] private Text maxPlayersText;
    [SerializeField] private Button maxPlayersMinusButton;
    [SerializeField] private Button maxPlayersPlusButton;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button quickPlayButton;
    [SerializeField, Tooltip("Parent that room entries are added to.")]
    private Transform roomListContent;
    [SerializeField, Tooltip("Inactive button copied for each room.")]
    private Button roomEntryTemplate;
    [SerializeField] private Text roomListEmptyText;
    [SerializeField] private Button quitButton;

    [Header("Room panel")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private Text roomTitleText;
    [SerializeField] private Text playerListText;
    [SerializeField] private Button startButton;
    [SerializeField] private Text startHintText;
    [SerializeField] private Button leaveRoomButton;

    private int maxPlayers = NetworkKeys.MaxPlayersPerRoom;
    private readonly List<GameObject> roomEntries = new List<GameObject>();

    private void Start()
    {
        connectButton.onClick.AddListener(() => manager.Connect(nicknameInput.text));
        createRoomButton.onClick.AddListener(() => manager.CreateRoom(roomNameInput.text, maxPlayers));
        quickPlayButton.onClick.AddListener(manager.QuickPlay);
        quitButton.onClick.AddListener(manager.Quit);
        startButton.onClick.AddListener(manager.StartGame);
        leaveRoomButton.onClick.AddListener(manager.LeaveRoom);
        maxPlayersMinusButton.onClick.AddListener(() => ChangeMaxPlayers(-1));
        maxPlayersPlusButton.onClick.AddListener(() => ChangeMaxPlayers(1));

        // Remember the last nickname between play sessions on this machine.
        nicknameInput.text = PlayerPrefs.GetString("nickname", string.Empty);
        nicknameInput.onEndEdit.AddListener(value => PlayerPrefs.SetString("nickname", value));

        roomEntryTemplate.gameObject.SetActive(false);
        UpdateMaxPlayersLabel();
    }

    public void ShowConnectPanel()
    {
        connectPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);
    }

    public void ShowLobbyPanel()
    {
        connectPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
    }

    public void ShowRoomPanel()
    {
        connectPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
    }

    public void SetStatus(string message)
    {
        statusText.text = message;
    }

    /// <summary>Rebuilds the room list from the manager's cache: one button per joinable room.</summary>
    public void RefreshRoomList(IEnumerable<RoomInfo> rooms)
    {
        foreach (GameObject entry in roomEntries)
        {
            Destroy(entry);
        }
        roomEntries.Clear();

        int count = 0;
        foreach (RoomInfo room in rooms)
        {
            count++;
            Button entry = Instantiate(roomEntryTemplate, roomListContent);
            entry.gameObject.SetActive(true);
            entry.GetComponentInChildren<Text>().text = room.Name + "   (" + room.PlayerCount + " / " + room.MaxPlayers + ")";

            string roomName = room.Name; // captured copy for the click handler
            entry.onClick.AddListener(() => manager.JoinRoom(roomName));
            roomEntries.Add(entry.gameObject);
        }

        roomListEmptyText.gameObject.SetActive(count == 0);
    }

    /// <summary>Redraws the players in the room and decides whether Start is clickable.</summary>
    public void RefreshPlayerList()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        Room room = PhotonNetwork.CurrentRoom;
        roomTitleText.text = room.Name + "   (" + room.PlayerCount + " / " + room.MaxPlayers + ")";

        StringBuilder builder = new StringBuilder();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            builder.Append(player.NickName);
            if (player.IsMasterClient) builder.Append("  (host)");
            if (player.IsLocal) builder.Append("  (you)");
            builder.AppendLine();
        }
        playerListText.text = builder.ToString();

        bool enoughPlayers = room.PlayerCount >= LobbyManager.MinPlayersToStart;
        bool isHost = PhotonNetwork.IsMasterClient;
        startButton.interactable = isHost && enoughPlayers;

        if (!isHost)
        {
            startHintText.text = "Waiting for the host to start...";
        }
        else if (!enoughPlayers)
        {
            startHintText.text = "Waiting for another player (" + room.PlayerCount + " / " + LobbyManager.MinPlayersToStart + ")";
        }
        else
        {
            startHintText.text = "Ready. Press Start Game.";
        }
    }

    private void ChangeMaxPlayers(int delta)
    {
        maxPlayers = Mathf.Clamp(maxPlayers + delta, LobbyManager.MinPlayersToStart, NetworkKeys.MaxPlayersPerRoom);
        UpdateMaxPlayersLabel();
    }

    private void UpdateMaxPlayersLabel()
    {
        maxPlayersText.text = "Max players: " + maxPlayers;
    }
}
