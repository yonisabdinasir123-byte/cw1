using System.Collections.Generic;
using Photon.Pun;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds Assets/Scenes/Game.unity from scratch: floor, four walls, spawn points, follow camera,
/// the GameManager object (with its PhotonView and KillTracker) and the HUD canvas scaled for
/// 1920x1080. Running it again replaces the scene.
/// </summary>
public static class ArenaSceneBuilder
{
    private const float ArenaHalfSize = 20f;
    private const float WallHeight = 2.5f;
    private const float WallThickness = 1f;

    [MenuItem("Tools/Arena/3. Build Game Scene")]
    public static void BuildGameScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        ArenaEditorUtil.EnsureTagsAndLayers();
        ArenaEditorUtil.EnsureFolder(ArenaEditorUtil.ScenesFolder);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        BuildArenaGeometry();
        List<Object> spawnPoints = BuildSpawnPoints();
        List<Object> powerUpPoints = BuildPowerUpPoints();
        BuildCamera();
        BuildManagers(spawnPoints, powerUpPoints);
        BuildHud();

        string path = ArenaEditorUtil.ScenesFolder + "/" + NetworkKeys.GameScene + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("Arena: Game scene saved to " + path);
    }

    private static void BuildArenaGeometry()
    {
        Material floorMaterial = ArenaEditorUtil.CreateMaterial("ArenaFloor", new Color(0.16f, 0.17f, 0.2f));
        Material wallMaterial = ArenaEditorUtil.CreateMaterial("ArenaWall", new Color(0.55f, 0.58f, 0.65f));

        GameObject arena = new GameObject("Arena");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(arena.transform, false);
        floor.transform.localScale = new Vector3(ArenaHalfSize / 5f, 1f, ArenaHalfSize / 5f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
        MarkArena(floor);

        float length = ArenaHalfSize * 2f + WallThickness * 2f;
        CreateWall(arena.transform, "WallNorth", new Vector3(0f, WallHeight / 2f, ArenaHalfSize + WallThickness / 2f), new Vector3(length, WallHeight, WallThickness), wallMaterial);
        CreateWall(arena.transform, "WallSouth", new Vector3(0f, WallHeight / 2f, -ArenaHalfSize - WallThickness / 2f), new Vector3(length, WallHeight, WallThickness), wallMaterial);
        CreateWall(arena.transform, "WallEast", new Vector3(ArenaHalfSize + WallThickness / 2f, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, length), wallMaterial);
        CreateWall(arena.transform, "WallWest", new Vector3(-ArenaHalfSize - WallThickness / 2f, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, length), wallMaterial);

        // A few cover blocks so fights are not just open-field.
        CreateWall(arena.transform, "CoverA", new Vector3(8f, 1f, 8f), new Vector3(3f, 2f, 3f), wallMaterial);
        CreateWall(arena.transform, "CoverB", new Vector3(-8f, 1f, -8f), new Vector3(3f, 2f, 3f), wallMaterial);
        CreateWall(arena.transform, "CoverC", new Vector3(-8f, 1f, 8f), new Vector3(3f, 2f, 3f), wallMaterial);
        CreateWall(arena.transform, "CoverD", new Vector3(8f, 1f, -8f), new Vector3(3f, 2f, 3f), wallMaterial);
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = material;
        MarkArena(wall);
    }

    private static void MarkArena(GameObject go)
    {
        go.tag = GameTags.ArenaTag;
        go.layer = LayerMask.NameToLayer(GameTags.ArenaLayer);
        go.isStatic = true;
    }

    private static List<Object> BuildSpawnPoints()
    {
        GameObject parent = new GameObject("SpawnPoints");
        float inset = ArenaHalfSize - 4f;
        Vector3[] positions =
        {
            new Vector3(-inset, 0f, -inset),
            new Vector3(inset, 0f, inset),
            new Vector3(-inset, 0f, inset),
            new Vector3(inset, 0f, -inset),
            new Vector3(0f, 0f, -inset),
            new Vector3(0f, 0f, inset)
        };

        List<Object> points = new List<Object>();
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject point = new GameObject("SpawnPoint" + (i + 1));
            point.tag = GameTags.SpawnPointTag;
            point.transform.SetParent(parent.transform, false);
            point.transform.localPosition = positions[i];
            // Face the centre of the arena.
            Vector3 toCentre = -positions[i];
            toCentre.y = 0f;
            point.transform.rotation = Quaternion.LookRotation(toCentre.normalized, Vector3.up);
            points.Add(point.transform);
        }
        return points;
    }

    private static List<Object> BuildPowerUpPoints()
    {
        GameObject parent = new GameObject("PowerUpPoints");
        Vector3[] positions =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 12f),
            new Vector3(0f, 0f, -12f),
            new Vector3(12f, 0f, 0f),
            new Vector3(-12f, 0f, 0f)
        };
        List<Object> points = new List<Object>();
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject point = new GameObject("PowerUpPoint" + (i + 1));
            point.transform.SetParent(parent.transform, false);
            point.transform.localPosition = positions[i];
            points.Add(point.transform);
        }
        return points;
    }

    private static void BuildCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
        }
        camera.transform.position = new Vector3(0f, 16f, -9f);
        camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
        camera.fieldOfView = 60f;
        if (camera.GetComponent<CameraFollow>() == null)
        {
            camera.gameObject.AddComponent<CameraFollow>();
        }
    }

    private static void BuildManagers(List<Object> spawnPoints, List<Object> powerUpPoints)
    {
        GameObject managers = new GameObject("GameManager");
        PhotonView view = managers.AddComponent<PhotonView>();
        view.ObservedComponents = new List<Component>();
        view.Synchronization = ViewSynchronization.Off;

        // Scene PhotonViews need a fixed ID that is identical on every client. PUN's editor hook
        // normally assigns it after the hierarchy changes; set it now so the saved scene has it.
        SerializedObject serializedView = new SerializedObject(view);
        SerializedProperty sceneViewId = serializedView.FindProperty("sceneViewId");
        if (sceneViewId != null)
        {
            sceneViewId.intValue = 1;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        GameManager gameManager = managers.AddComponent<GameManager>();
        managers.AddComponent<KillTracker>();
        managers.AddComponent<RoundManager>();
        managers.AddComponent<PlayAgainController>();
        PowerUpSpawner spawner = managers.AddComponent<PowerUpSpawner>();
        ArenaEditorUtil.SetReferenceArray(gameManager, "spawnPoints", spawnPoints);
        ArenaEditorUtil.SetReferenceArray(spawner, "spawnPoints", powerUpPoints);
    }

    private static void BuildHud()
    {
        Canvas canvas = ArenaEditorUtil.CreateCanvas("GameCanvas");
        Transform root = canvas.transform;

        // Top-left: room info and Leave button.
        Text roomInfo = ArenaEditorUtil.CreateText(root, "RoomInfo", "Room", 26, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -50f), new Vector2(700f, -12f));
        Button leaveButton = ArenaEditorUtil.CreateButton(root, "LeaveButton", "Leave",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -110f), new Vector2(180f, -60f));

        // Top-right: scoreboard panel.
        Image scorePanel = ArenaEditorUtil.CreateImage(root, "ScorePanel", new Color(0f, 0f, 0f, 0.55f),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-420f, -240f), new Vector2(-20f, -20f));
        Text scoreText = ArenaEditorUtil.CreateText(scorePanel.transform, "ScoreText", "SCOREBOARD", 28, TextAnchor.UpperLeft,
            Vector2.zero, Vector2.one, new Vector2(16f, 12f), new Vector2(-16f, -12f));

        // Bottom-left: health bar.
        Image healthBackground = ArenaEditorUtil.CreateImage(root, "HealthBar", new Color(0.1f, 0.1f, 0.1f, 0.8f),
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(520f, 70f));
        Image healthFill = ArenaEditorUtil.CreateImage(healthBackground.transform, "Fill", new Color(0.2f, 0.85f, 0.3f),
            Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
        Text healthText = ArenaEditorUtil.CreateText(healthBackground.transform, "HealthText", "100 / 100", 28, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Centre: respawn countdown.
        Text respawnText = ArenaEditorUtil.CreateText(root, "RespawnText", string.Empty, 64, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-500f, -60f), new Vector2(500f, 60f));

        // Bottom-centre: controls hint.
        ArenaEditorUtil.CreateText(root, "ControlsHint", "WASD to move   Space or Left Click to fire   Shift to dash", 24, TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-500f, 16f), new Vector2(500f, 56f));

        // Full-screen damage flash, drawn first so everything else sits above it. Never blocks clicks.
        Image damageFlash = ArenaEditorUtil.CreateImage(root, "DamageFlash", new Color(1f, 0f, 0f, 0f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        damageFlash.raycastTarget = false;
        damageFlash.transform.SetAsFirstSibling();

        // Bottom-left above health: dash cooldown bar and power-up label.
        Image dashBackground = ArenaEditorUtil.CreateImage(root, "DashBar", new Color(0.1f, 0.1f, 0.1f, 0.8f),
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 80f), new Vector2(520f, 104f));
        Image dashFill = ArenaEditorUtil.CreateImage(dashBackground.transform, "Fill", new Color(0.4f, 0.8f, 1f),
            Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        ArenaEditorUtil.CreateText(dashBackground.transform, "DashLabel", "DASH (Shift)", 18, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text powerUpText = ArenaEditorUtil.CreateText(root, "PowerUpText", string.Empty, 30, TextAnchor.MiddleLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 112f), new Vector2(520f, 152f));

        // Right side under the scoreboard: kill feed.
        Text killFeed = ArenaEditorUtil.CreateText(root, "KillFeed", string.Empty, 26, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-620f, -460f), new Vector2(-20f, -260f));
        killFeed.color = new Color(1f, 0.9f, 0.7f);
        KillFeedUI feed = canvas.gameObject.AddComponent<KillFeedUI>();
        ArenaEditorUtil.SetReference(feed, "feedText", killFeed);

        HudUI hud = canvas.gameObject.AddComponent<HudUI>();
        ArenaEditorUtil.SetReference(hud, "healthFill", healthFill.rectTransform);
        ArenaEditorUtil.SetReference(hud, "healthText", healthText);
        ArenaEditorUtil.SetReference(hud, "respawnText", respawnText);
        ArenaEditorUtil.SetReference(hud, "roomInfoText", roomInfo);
        ArenaEditorUtil.SetReference(hud, "leaveButton", leaveButton);
        ArenaEditorUtil.SetReference(hud, "dashFill", dashFill.rectTransform);
        ArenaEditorUtil.SetReference(hud, "powerUpText", powerUpText);
        ArenaEditorUtil.SetReference(hud, "damageFlash", damageFlash);

        ScoreboardUI scoreboard = canvas.gameObject.AddComponent<ScoreboardUI>();
        ArenaEditorUtil.SetReference(scoreboard, "scoreText", scoreText);

        // Top-centre: round timer.
        Text timerText = ArenaEditorUtil.CreateText(root, "TimerText", "1:30", 56, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-200f, -90f), new Vector2(200f, -16f));
        RoundTimerUI timer = canvas.gameObject.AddComponent<RoundTimerUI>();
        ArenaEditorUtil.SetReference(timer, "timerText", timerText);

        BuildEndScreen(canvas);
    }

    private static void BuildEndScreen(Canvas canvas)
    {
        // Added last so it draws above the HUD.
        Image overlay = ArenaEditorUtil.CreateImage(canvas.transform, "EndScreen", new Color(0f, 0f, 0f, 0.75f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Transform p = overlay.transform;

        Text winner = ArenaEditorUtil.CreateText(p, "WinnerText", "Winner", 96, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-900f, 180f), new Vector2(900f, 320f));
        winner.color = new Color(1f, 0.9f, 0.4f);
        Text result = ArenaEditorUtil.CreateText(p, "ResultText", "You Won!", 64, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600f, 90f), new Vector2(600f, 180f));
        Image scoreBackground = ArenaEditorUtil.CreateImage(p, "FinalScorePanel", new Color(0f, 0f, 0f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300f, -170f), new Vector2(300f, 70f));
        Text finalScore = ArenaEditorUtil.CreateText(scoreBackground.transform, "FinalScoreText", "FINAL SCORES", 30, TextAnchor.UpperLeft,
            Vector2.zero, Vector2.one, new Vector2(20f, 12f), new Vector2(-20f, -12f));
        Text playAgainHint = ArenaEditorUtil.CreateText(p, "PlayAgainHint", string.Empty, 26, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-700f, -230f), new Vector2(700f, -180f));
        playAgainHint.color = new Color(0.9f, 0.85f, 0.5f);
        Button playAgain = ArenaEditorUtil.CreateButton(p, "PlayAgainButton", "Play Again",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -330f), new Vector2(-20f, -250f));
        playAgain.image.color = new Color(0.2f, 0.65f, 0.35f);
        Button leave = ArenaEditorUtil.CreateButton(p, "LeaveButton", "Leave",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f, -330f), new Vector2(330f, -250f));
        leave.image.color = new Color(0.75f, 0.25f, 0.25f);

        EndScreenUI endScreen = canvas.gameObject.AddComponent<EndScreenUI>();
        ArenaEditorUtil.SetReference(endScreen, "panel", overlay.gameObject);
        ArenaEditorUtil.SetReference(endScreen, "winnerText", winner);
        ArenaEditorUtil.SetReference(endScreen, "resultText", result);
        ArenaEditorUtil.SetReference(endScreen, "finalScoreText", finalScore);
        ArenaEditorUtil.SetReference(endScreen, "leaveButton", leave);
        ArenaEditorUtil.SetReference(endScreen, "playAgainButton", playAgain);
        ArenaEditorUtil.SetReference(endScreen, "playAgainHintText", playAgainHint);

        overlay.gameObject.SetActive(false);
    }
}
