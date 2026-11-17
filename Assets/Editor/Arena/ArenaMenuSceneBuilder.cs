using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds Assets/Scenes/MainMenu.unity for Phase 1: a title, nickname field, Play button and
/// status line wired to NetworkLauncher. Phase 2 replaces this with the full lobby.
/// </summary>
public static class ArenaMenuSceneBuilder
{
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
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        }

        Canvas canvas = ArenaEditorUtil.CreateCanvas("MenuCanvas");
        Transform root = canvas.transform;

        ArenaEditorUtil.CreateText(root, "Title", "ARENA SHOOTER", 96, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-600f, -260f), new Vector2(600f, -100f));

        ArenaEditorUtil.CreateText(root, "NicknameLabel", "Nickname", 30, TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250f, 70f), new Vector2(250f, 110f));

        InputField nickname = ArenaEditorUtil.CreateInputField(root, "NicknameInput", "Type a name...",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250f, 0f), new Vector2(250f, 64f));

        Button play = ArenaEditorUtil.CreateButton(root, "PlayButton", "Quick Play",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250f, -100f), new Vector2(250f, -20f));

        Text status = ArenaEditorUtil.CreateText(root, "StatusText", "Connecting...", 28, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-800f, 40f), new Vector2(800f, 90f));
        status.color = new Color(0.8f, 0.85f, 0.9f);

        GameObject launcherObject = new GameObject("NetworkLauncher");
        NetworkLauncher launcher = launcherObject.AddComponent<NetworkLauncher>();
        ArenaEditorUtil.SetReference(launcher, "nicknameInput", nickname);
        ArenaEditorUtil.SetReference(launcher, "playButton", play);
        ArenaEditorUtil.SetReference(launcher, "statusText", status);

        string path = ArenaEditorUtil.ScenesFolder + "/" + NetworkKeys.MainMenuScene + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("Arena: MainMenu scene saved to " + path);
    }
}
