using UnityEngine;

/// <summary>
/// Spawns local-only particle effects from Resources. Effects are not networked: every client
/// already knows when a hit or death happened, so each one plays the effect itself.
/// </summary>
public static class Effects
{
    /// <summary>Plays a one-shot effect prefab at a position. The prefab destroys itself when done.</summary>
    public static void Play(string prefabName, Vector3 position)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab != null)
        {
            Object.Instantiate(prefab, position, Quaternion.identity);
        }
    }
}
