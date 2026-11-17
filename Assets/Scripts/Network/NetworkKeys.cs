/// <summary>
/// The single place where every shared network string lives: custom property keys,
/// prefab names used by PhotonNetwork.Instantiate and scene names used by PhotonNetwork.LoadLevel.
/// Every client must agree on these exactly, so they are constants instead of typed-out strings.
/// </summary>
public static class NetworkKeys
{
    // ---- Player custom properties (Photon sends the key with every update, so short keys save bandwidth) ----

    /// <summary>Number of kills this player has in the current round. Written only by the master client.</summary>
    public const string Kills = "k";

    /// <summary>Number of times this player has died in the current round. Written only by the master client.</summary>
    public const string Deaths = "d";

    // ---- Prefabs. PhotonNetwork.Instantiate loads these by name from a Resources folder. ----

    public const string PlayerPrefab = "Player";
    public const string ProjectilePrefab = "Projectile";

    // ---- Scenes. Must be listed in Build Settings. ----

    public const string MainMenuScene = "MainMenu";
    public const string GameScene = "Game";

    /// <summary>Room size limit used when creating rooms.</summary>
    public const int MaxPlayersPerRoom = 4;
}
