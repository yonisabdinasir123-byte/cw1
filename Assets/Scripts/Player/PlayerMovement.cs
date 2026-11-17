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

    /// <summary>Direction the player is facing. Starts as forward so a player can shoot before moving.</summary>
    public Vector3 FacingDirection { get; private set; } = Vector3.forward;

    /// <summary>Set to false to ignore input, for example when the round has ended.</summary>
    public bool InputEnabled { get; set; } = true;

    private Rigidbody body;
    private Vector3 moveInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
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

        if (!InputEnabled)
        {
            moveInput = Vector3.zero;
            return;
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

        // Setting velocity (instead of teleporting) lets the arena walls block the player.
        Vector3 velocity = moveInput * moveSpeed;
        velocity.y = body.linearVelocity.y;
        body.linearVelocity = velocity;
    }
}
