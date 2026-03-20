using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    private CharacterController controller;

    [Header("Base Movement")]
    public float speed = 5f;
    [HideInInspector] public float speedMultiplier = 1f;
    public float gravity = -9.81f * 2f;
    public float jumpHeight = 3f;

    [Header("Jump Assist")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.1f;
    public LayerMask groundMask;

    [Header("Crouch")]
    public float standingHeight = 2.0f;
    public float crouchHeight = 1.2f;
    public float crouchSpeedMultiplier = 0.6f;
    public float crouchTransitionSpeed = 12f;

    [Header("Camera Crouch")]
    public Transform cameraRoot;
    public float crouchCameraExtraDrop = 0f;

    [Header("Glide")]
    public float airControlMultiplier = 0.65f;
    public float glideGravityMultiplier = 0.25f;
    public float maxGlideFallSpeed = -6f;

    // Health
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool IsDead => currentHealth.Value <= 0;

    private Vector3 velocity;
    private bool isGrounded;

    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction shiftAction;

    private enum MoveState { Normal, Crouching, Gliding }
    private MoveState state = MoveState.Normal;

    private float currentSpeedMultiplier = 1f;
    private float standingCameraLocalY;

    public override void OnNetworkSpawn()
    {
        // Only enable input and camera for the local owner
        if (!IsOwner)
        {
            // Disable camera for non-owners so they don't see through another player's eyes
            if (cameraRoot != null)
            {
                Camera cam = cameraRoot.GetComponentInChildren<Camera>();
                if (cam != null) cam.gameObject.SetActive(false);
            }
            enabled = false; // Disable this script entirely for non-owners
            return;
        }

        InitInput();
    }

    private void InitInput()
    {
        // controller already set in Awake
        moveAction = new InputAction("Move", InputActionType.Value);
        var composite = moveAction.AddCompositeBinding("2DVector");
        composite.With("Up", "<Keyboard>/w");
        composite.With("Down", "<Keyboard>/s");
        composite.With("Left", "<Keyboard>/a");
        composite.With("Right", "<Keyboard>/d");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");

        shiftAction = new InputAction("Crouch/Glide", InputActionType.Button);
        shiftAction.AddBinding("<Keyboard>/leftShift");
        shiftAction.AddBinding("<Keyboard>/rightShift");

        moveAction.Enable();
        jumpAction.Enable();
        shiftAction.Enable();
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        standingHeight = controller.height;
        if (cameraRoot != null)
            standingCameraLocalY = cameraRoot.localPosition.y;
    }

    void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        shiftAction?.Disable();
    }

    void Update()
    {
        // Only the owner runs movement
        if (!IsOwner || IsDead) return;

        isGrounded = controller.isGrounded;

        Vector2 input = moveAction.ReadValue<Vector2>();
        float x = input.x;
        float z = input.y;

        Vector3 moveDirWorld = (transform.right * x + transform.forward * z);

        bool shiftHeld = shiftAction.IsPressed();
        bool shiftReleased = shiftAction.WasReleasedThisFrame();

        if (jumpAction.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        if (isGrounded)
            state = shiftHeld ? MoveState.Crouching : MoveState.Normal;
        else
        {
            if (shiftHeld && velocity.y <= 0f)
                state = MoveState.Gliding;
            else if (state == MoveState.Gliding)
                state = MoveState.Normal;
        }

        HandleCrouchSizing(shiftReleased);
        HandleCameraCrouch();

        currentSpeedMultiplier = 1f;
        if (state == MoveState.Crouching)
            currentSpeedMultiplier *= crouchSpeedMultiplier;

        float airControl = isGrounded ? 1f : airControlMultiplier;
        Vector3 finalMove = moveDirWorld * (speed * speedMultiplier * currentSpeedMultiplier * airControl);
        controller.Move(finalMove * Time.deltaTime);

        float effectiveGravity = gravity;
        if (state == MoveState.Gliding && !isGrounded)
        {
            effectiveGravity = gravity * glideGravityMultiplier;
            if (velocity.y < maxGlideFallSpeed)
                velocity.y = maxGlideFallSpeed;
        }

        velocity.y += effectiveGravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        isGrounded = controller.isGrounded;

        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            state = MoveState.Normal;
            isGrounded = false;
        }

        if (isGrounded && velocity.y < 0f)
            velocity.y = 0f;
    }

    // Called by server only via Arrow hit
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(int damage)
    {
        if (IsDead) return;
        currentHealth.Value = Mathf.Max(0, currentHealth.Value - damage);
        if (IsDead)
        {
            DieClientRpc();
        }
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        // Disable visuals and collider — don't SetActive(false) on a NetworkObject
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        controller.enabled = false;
        moveAction?.Disable();
        jumpAction?.Disable();
        shiftAction?.Disable();
    }

    // Only call from server
    public void ResetPlayer(Vector3 spawnPosition)
    {
        if (!IsServer) return;
        currentHealth.Value = 100;
        TeleportClientRpc(spawnPosition);
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 spawnPosition)
    {
        controller.enabled = false;
        transform.position = spawnPosition;
        controller.enabled = true;

        // Re-enable visuals
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;

        // Re-enable input for owner
        if (IsOwner)
        {
            moveAction?.Enable();
            jumpAction?.Enable();
            shiftAction?.Enable();
        }
    }

    private void HandleCrouchSizing(bool shiftReleased)
    {
        float targetHeight = (state == MoveState.Crouching) ? crouchHeight : standingHeight;

        if (state == MoveState.Normal && shiftReleased)
        {
            if (!CanStandUp())
            {
                state = MoveState.Crouching;
                targetHeight = crouchHeight;
            }
        }

        float newHeight = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        float centerY = newHeight / 2f;
        controller.height = newHeight;
        controller.center = new Vector3(controller.center.x, centerY, controller.center.z);
    }

    private void HandleCameraCrouch()
    {
        if (cameraRoot == null) return;
        float heightDelta = standingHeight - controller.height;
        float targetY = standingCameraLocalY - heightDelta - crouchCameraExtraDrop;
        Vector3 local = cameraRoot.localPosition;
        local.y = Mathf.Lerp(local.y, targetY, Time.deltaTime * crouchTransitionSpeed);
        cameraRoot.localPosition = local;
    }

    private bool CanStandUp()
    {
        float radius = controller.radius;
        Vector3 bottom = transform.position + controller.center - Vector3.up * (controller.height / 2f) + Vector3.up * radius;
        Vector3 top = bottom + Vector3.up * (standingHeight - 2f * radius);
        return !Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
    }
}