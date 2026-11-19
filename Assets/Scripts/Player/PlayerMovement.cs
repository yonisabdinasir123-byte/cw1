using Photon.Pun;
using UnityEngine;

/// <summary>
/// Reads WASD or arrow keys on the owning client and moves the player in eight directions.
/// Diagonals are normalised so moving diagonally is not faster. The last non-zero direction
/// is remembered as the facing direction, which PlayerShooting uses to aim projectiles.
/// Position and rotation reach other clients through the PhotonTransformView on the prefab.
/// </summary>
public class PlayerMovement : MonoBehaviourPun
{
    [SerializeField, Tooltip("Movement speed in units per second.")]
    private float moveSpeed = 8f;

    [SerializeField, Tooltip("How fast the player turns to face the movement direction, in degrees per second.")]
    private float turnSpeed = 900f;

    [Header("Dash")]
    [SerializeField, Tooltip("Key that triggers a dash in the facing direction.")]
    private KeyCode dashKey = KeyCode.LeftShift;

    [SerializeField, Tooltip("Speed during the dash, in units per second.")]
    private float dashSpeed = 30f;

    [SerializeField, Tooltip("How long the dash lasts, in seconds.")]
    private float dashDuration = 0.15f;

    [SerializeField, Tooltip("Seconds before the dash can be used again.")]
    private float dashCooldown = 3f;

    [SerializeField, Tooltip("Trail shown while dashing, on every client.")]
    private TrailRenderer dashTrail;

    /// <summary>Seconds until the dash is ready again. Zero means ready.</summary>
    public float DashCooldownRemaining { get; private set; }

    /// <summary>Full cooldown length, so the HUD can draw a ratio.</summary>
    public float DashCooldown => dashCooldown;

    /// <summary>Direction the player is facing. Starts as forward so a player can shoot before moving.</summary>
    public Vector3 FacingDirection { get; private set; } = Vector3.forward;

    /// <summary>Set to false to ignore input, for example when the round has ended.</summary>
    public bool InputEnabled { get; set; } = true;

    private Rigidbody body;
    private PlayerPowerUps powerUps;
    private Vector3 moveInput;
    private Vector3 dashDirection;
    private float dashTimeLeft;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        powerUps = GetComponent<PlayerPowerUps>();
        if (dashTrail != null) dashTrail.emitting = false;
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            // Remote copies are driven by PhotonTransformView, so physics must not move them.
            body.isKinematic = true;
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        DashCooldownRemaining = Mathf.Max(0f, DashCooldownRemaining - Time.deltaTime);

        if (!InputEnabled)
        {
            moveInput = Vector3.zero;
            return;
        }

        if (Input.GetKeyDown(dashKey) && DashCooldownRemaining <= 0f)
        {
            StartDash();
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(horizontal, 0f, vertical);

        // Normalise so diagonal movement has the same speed as straight movement.
        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        if (moveInput.sqrMagnitude > 0.01f)
        {
            FacingDirection = moveInput.normalized;
        }

        // Rotate towards the facing direction so the body shows where the next shot goes.
        Quaternion targetRotation = Quaternion.LookRotation(FacingDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        Vector3 velocity;
        if (dashTimeLeft > 0f)
        {
            dashTimeLeft -= Time.fixedDeltaTime;
            velocity = dashDirection * dashSpeed;
            if (dashTimeLeft <= 0f && dashTrail != null) dashTrail.emitting = false;
        }
        else
        {
            // Setting velocity (instead of teleporting) lets the arena walls block the player.
            float multiplier = powerUps != null ? powerUps.SpeedMultiplier : 1f;
            velocity = moveInput * (moveSpeed * multiplier);
        }
        velocity.y = body.linearVelocity.y;
        body.linearVelocity = velocity;
    }

    /// <summary>Owner only: begins a dash and asks the other clients to play the trail.</summary>
    private void StartDash()
    {
        dashDirection = FacingDirection;
        dashTimeLeft = dashDuration;
        DashCooldownRemaining = dashCooldown;
        PlayDashEffect();
        photonView.RPC(nameof(RpcDashEffect), RpcTarget.Others);
    }

    /// <summary>RPC on other clients: the movement itself arrives through PhotonTransformView; this adds the trail and sound.</summary>
    [PunRPC]
    public void RpcDashEffect()
    {
        PlayDashEffect();
    }

    private void PlayDashEffect()
    {
        AudioManager.Play(SoundId.Dash, 0.7f);
        if (dashTrail != null)
        {
            dashTrail.Clear();
            dashTrail.emitting = true;
            StopAllCoroutines();
            StartCoroutine(StopTrailAfter(dashDuration + 0.05f));
        }
    }

    private System.Collections.IEnumerator StopTrailAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (dashTrail != null) dashTrail.emitting = false;
    }
}
