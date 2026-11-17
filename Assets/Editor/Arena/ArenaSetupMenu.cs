using System.Collections.Generic;
using Photon.Pun;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click project setup and a verification report. "Run All Steps" builds prefabs, both scenes
/// and the Build Settings list, then opens the main menu ready to press Play.
/// </summary>
public static class ArenaSetupMenu
{
    [MenuItem("Tools/Arena/1. Run All Steps", priority = 0)]
    public static void RunAll()
    {
        ArenaEditorUtil.EnsureTagsAndLayers();
        ArenaPrefabBuilder.BuildAll();
        ArenaSceneBuilder.BuildGameScene();
        ArenaMenuSceneBuilder.BuildMenuScene();
        UpdateBuildSettings();
        EditorSceneManager.OpenScene(ArenaEditorUtil.ScenesFolder + "/" + NetworkKeys.MainMenuScene + ".unity");
        Verify();
    }

    /// <summary>Puts MainMenu first and Game second in Build Settings and removes everything else.</summary>
    [MenuItem("Tools/Arena/5. Update Build Settings")]
    public static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ArenaEditorUtil.ScenesFolder + "/" + NetworkKeys.MainMenuScene + ".unity", true),
            new EditorBuildSettingsScene(ArenaEditorUtil.ScenesFolder + "/" + NetworkKeys.GameScene + ".unity", true)
        };
        Debug.Log("Arena: Build Settings now list MainMenu (0) and Game (1)");
    }

    /// <summary>Logs a pass or fail line for each thing the game needs. Read the Console after running it.</summary>
    [MenuItem("Tools/Arena/6. Verify Setup")]
    public static void Verify()
    {
        List<string> problems = new List<string>();

        if (Resources.Load<GameObject>(NetworkKeys.PlayerPrefab) == null) problems.Add("Player prefab missing from Resources");
        if (Resources.Load<GameObject>(NetworkKeys.ProjectilePrefab) == null) problems.Add("Projectile prefab missing from Resources");

        foreach (string layer in new[] { GameTags.PlayerLayer, GameTags.ProjectileLayer, GameTags.ArenaLayer })
        {
            if (LayerMask.NameToLayer(layer) < 0) problems.Add("Layer missing: " + layer);
        }

        bool menuInBuild = false;
        bool gameInBuild = false;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path.EndsWith(NetworkKeys.MainMenuScene + ".unity")) menuInBuild = true;
            if (scene.path.EndsWith(NetworkKeys.GameScene + ".unity")) gameInBuild = true;
        }
        if (!menuInBuild) problems.Add("MainMenu scene not in Build Settings");
        if (!gameInBuild) problems.Add("Game scene not in Build Settings");

        ServerSettings settings = PhotonNetwork.PhotonServerSettings;
        if (settings == null || string.IsNullOrEmpty(settings.AppSettings.AppIdRealtime)) problems.Add("Photon App ID is empty (Window > Photon Unity Networking > PUN Wizard)");

        if (problems.Count == 0)
        {
            Debug.Log("Arena verify: all checks passed. Press Play in MainMenu.");
        }
        else
        {
            foreach (string problem in problems)
            {
                Debug.LogError("Arena verify: " + problem);
            }
        }
    }
}
