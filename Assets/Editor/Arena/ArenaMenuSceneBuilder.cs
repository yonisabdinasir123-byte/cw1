using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds Assets/Scenes/MainMenu.unity with the full lobby: a Connect panel (nickname), a Lobby
/// panel (create room, room list, quick play, quit) and a Room panel (player list, start, leave),
/// all wired to LobbyManager and LobbyUI. Running it again replaces the scene.
/// </summary>
public static class ArenaMenuSceneBuilder
{
    private static readonly Color PanelColor = new Color(0.12f, 0.14f, 0.2f, 0.95f);
    private static readonly Color DangerColor = new Color(0.75f, 0.25f, 0.25f);

    [MenuItem("Tools/Arena/4. Build Main Menu Scene")]
    public static void BuildMenuScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        ArenaEditorUtil.EnsureFolder(ArenaEditorUtil.ScenesFolder);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
        }

        Canvas canvas = ArenaEditorUtil.CreateCanvas("MenuCanvas");
        Transform root = canvas.transform;

        GameObject lobbyObject = new GameObject("Lobby");
        LobbyManager manager = lobbyObject.AddComponent<LobbyManager>();
        LobbyUI ui = lobbyObject.AddComponent<LobbyUI>();
        ArenaEditorUtil.SetReference(manager, "ui", ui);
        ArenaEditorUtil.SetReference(ui, "manager", manager);

        // Shared: title and status line.
        ArenaEditorUtil.CreateText(root, "Title", "ARENA SHOOTER", 90, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-600f, -150f), new Vector2(600f, -30f));
        Text status = ArenaEditorUtil.CreateText(root, "StatusText", string.Empty, 28, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-900f, 30f), new Vector2(900f, 80f));
        status.color = new Color(0.8f, 0.85f, 0.95f);
        ArenaEditorUtil.SetReference(ui, "statusText", status);

        GameObject connectPanel = BuildConnectPanel(root, ui);
        GameObject lobbyPanel = BuildLobbyPanel(root, ui);
        GameObject roomPanel = BuildRoomPanel(root, ui);

        // Only the Connect panel is visible until LobbyUI switches panels at runtime.
        connectPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);

        string path = ArenaEditorUtil.ScenesFolder + "/" + NetworkKeys.MainMenuScene + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("Arena: MainMenu scene saved to " + path);
    }

    private static GameObject BuildConnectPanel(Transform root, LobbyUI ui)
    {
        Image panel = ArenaEditorUtil.CreateImage(root, "ConnectPanel", PanelColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -160f), new Vector2(320f, 160f));
        Transform p = panel.transform;

        ArenaEditorUtil.CreateText(p, "Label", "Nickname", 32, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -90f), new Vector2(-40f, -40f));
        InputField nickname = ArenaEditorUtil.CreateInputField(p, "NicknameInput", "Type a name...",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -160f), new Vector2(-40f, -96f));
        Button connect = ArenaEditorUtil.CreateButton(p, "ConnectButton", "Connect",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(40f, 40f), new Vector2(-40f, 120f));

        ArenaEditorUtil.SetReference(ui, "connectPanel", panel.gameObject);
        ArenaEditorUtil.SetReference(ui, "nicknameInput", nickname);
        ArenaEditorUtil.SetReference(ui, "connectButton", connect);
        return panel.gameObject;
    }

    private static GameObject BuildLobbyPanel(Transform root, LobbyUI ui)
    {
        Image panel = ArenaEditorUtil.CreateImage(root, "LobbyPanel", PanelColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-700f, -330f), new Vector2(700f, 330f));
        Transform p = panel.transform;

        // Left column: create a room.
        ArenaEditorUtil.CreateText(p, "CreateHeader", "Create a room", 36, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -80f), new Vector2(620f, -30f));
        InputField roomName = ArenaEditorUtil.CreateInputField(p, "RoomNameInput", "Room name (optional)",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -150f), new Vector2(620f, -90f));
        Button minus = ArenaEditorUtil.CreateButton(p, "MaxMinus", "-",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -230f), new Vector2(110f, -165f));
        Text maxPlayers = ArenaEditorUtil.CreateText(p, "MaxPlayersText", "Max players: 4", 30, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -230f), new Vector2(540f, -165f));
        Button plus = ArenaEditorUtil.CreateButton(p, "MaxPlus", "+",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(550f, -230f), new Vector2(620f, -165f));
        Button create = ArenaEditorUtil.CreateButton(p, "CreateRoomButton", "Create Room",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -320f), new Vector2(620f, -250f));
        Button quickPlay = ArenaEditorUtil.CreateButton(p, "QuickPlayButton", "Quick Play (any room)",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -410f), new Vector2(620f, -340f));
        Button quit = ArenaEditorUtil.CreateButton(p, "QuitButton", "Quit",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 30f), new Vector2(620f, 100f));
        quit.image.color = DangerColor;

        // Right column: room list.
        ArenaEditorUtil.CreateText(p, "ListHeader", "Rooms (click to join)", 36, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(680f, -80f), new Vector2(1360f, -30f));
        RectTransform content = ArenaEditorUtil.CreateScrollList(p, "RoomList",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(680f, 30f), new Vector2(1360f, -90f));
        Text emptyText = ArenaEditorUtil.CreateText(content.parent.parent, "EmptyText", "No rooms yet. Create one or wait for the list to update.", 26, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
        emptyText.color = new Color(0.7f, 0.7f, 0.75f);

        Button template = ArenaEditorUtil.CreateButton(content, "RoomEntryTemplate", "Room name (1 / 4)",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        template.image.color = new Color(0.25f, 0.3f, 0.4f);
        LayoutElement layout = template.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 60f;
        template.GetComponentInChildren<Text>().fontSize = 28;
        template.gameObject.SetActive(false);

        ArenaEditorUtil.SetReference(ui, "lobbyPanel", panel.gameObject);
        ArenaEditorUtil.SetReference(ui, "roomNameInput", roomName);
        ArenaEditorUtil.SetReference(ui, "maxPlayersText", maxPlayers);
        ArenaEditorUtil.SetReference(ui, "maxPlayersMinusButton", minus);
        ArenaEditorUtil.SetReference(ui, "maxPlayersPlusButton", plus);
        ArenaEditorUtil.SetReference(ui, "createRoomButton", create);
        ArenaEditorUtil.SetReference(ui, "quickPlayButton", quickPlay);
        ArenaEditorUtil.SetReference(ui, "roomListContent", content);
        ArenaEditorUtil.SetReference(ui, "roomEntryTemplate", template);
        ArenaEditorUtil.SetReference(ui, "roomListEmptyText", emptyText);
        ArenaEditorUtil.SetReference(ui, "quitButton", quit);
        return panel.gameObject;
    }

    private static GameObject BuildRoomPanel(Transform root, LobbyUI ui)
    {
        Image panel = ArenaEditorUtil.CreateImage(root, "RoomPanel", PanelColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450f, -300f), new Vector2(450f, 300f));
        Transform p = panel.transform;

        Text title = ArenaEditorUtil.CreateText(p, "RoomTitle", "Room", 40, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -90f), new Vector2(-30f, -30f));
        ArenaEditorUtil.CreateText(p, "PlayersHeader", "Players", 30, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -150f), new Vector2(-40f, -100f));
        Image listBackground = ArenaEditorUtil.CreateImage(p, "PlayerListBackground", new Color(0f, 0f, 0f, 0.35f),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -400f), new Vector2(-40f, -155f));
        Text players = ArenaEditorUtil.CreateText(listBackground.transform, "PlayerListText", string.Empty, 30, TextAnchor.UpperLeft,
            Vector2.zero, Vector2.one, new Vector2(16f, 10f), new Vector2(-16f, -10f));
        Text hint = ArenaEditorUtil.CreateText(p, "StartHint", string.Empty, 26, TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(40f, 130f), new Vector2(-40f, 180f));
        hint.color = new Color(0.9f, 0.85f, 0.5f);
        Button start = ArenaEditorUtil.CreateButton(p, "StartButton", "Start Game",
            new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(40f, 40f), new Vector2(-15f, 120f));
        start.image.color = new Color(0.2f, 0.65f, 0.35f);
        Button leave = ArenaEditorUtil.CreateButton(p, "LeaveRoomButton", "Leave Room",
            new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(15f, 40f), new Vector2(-40f, 120f));
        leave.image.color = DangerColor;

        ArenaEditorUtil.SetReference(ui, "roomPanel", panel.gameObject);
        ArenaEditorUtil.SetReference(ui, "roomTitleText", title);
        ArenaEditorUtil.SetReference(ui, "playerListText", players);
        ArenaEditorUtil.SetReference(ui, "startButton", start);
        ArenaEditorUtil.SetReference(ui, "startHintText", hint);
        ArenaEditorUtil.SetReference(ui, "leaveRoomButton", leave);
        return panel.gameObject;
    }
}
