/// <summary>
/// Names of the tags and layers the game relies on. They are created by the Editor tool
/// (Tools > Arena) and referenced here so no script contains a typed-out tag or layer string.
/// </summary>
public static class GameTags
{
    public const string PlayerTag = "Player";
    public const string ArenaTag = "Arena";
    public const string SpawnPointTag = "SpawnPoint";
    public const string ProjectileTag = "Projectile";

    public const string PlayerLayer = "Player";
    public const string ProjectileLayer = "Projectile";
    public const string ArenaLayer = "Arena";
}
