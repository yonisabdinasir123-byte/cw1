using UnityEngine;

/// <summary>
/// Keeps the camera above and behind the local player. The camera keeps the fixed top-down
/// angle set in the scene and only its position moves, which keeps the arena readable.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField, Tooltip("Camera position relative to the player.")]
    private Vector3 offset = new Vector3(0f, 16f, -9f);

    [SerializeField, Tooltip("Smoothing time in seconds. Lower is snappier.")]
    private float smoothTime = 0.12f;

    private Transform target;
    private Vector3 velocity;

    private void OnEnable()
    {
        PlayerHealth.LocalPlayerSpawned += HandleLocalPlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerHealth.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
    }

    private void HandleLocalPlayerSpawned(PlayerHealth player)
    {
        target = player.transform;
        transform.position = target.position + offset;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }
        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
