using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
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
    [Tooltip("Assign your Camera transform OR a parent 'CameraRoot' transform.")]
    public Transform cameraRoot;
    [Tooltip("Optional extra offset applied when crouching (usually 0).")]
    public float crouchCameraExtraDrop = 0f;

    [Header("Glide")]
    public float airControlMultiplier = 0.65f;
    public float glideGravityMultiplier = 0.25f;
    public float maxGlideFallSpeed = -6f;

    private Vector3 velocity;
    private bool isGrounded;
    private bool isMoving;

    // Jump timers
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;

    // New Input System actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction shiftAction;

    private enum MoveState { Normal, Crouching, Gliding }
    private MoveState state = MoveState.Normal;

    private float currentSpeedMultiplier = 1f;

    // Camera crouch internals
    private float standingCameraLocalY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

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
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        shiftAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        shiftAction.Disable();
    }

    void Start()
    {
        standingHeight = controller.height;

        if (cameraRoot != null)
            standingCameraLocalY = cameraRoot.localPosition.y;
        else
            Debug.LogWarning("PlayerMovement: cameraRoot is not assigned. Camera won't adjust for crouch.");
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        // Read movement input
        Vector2 input = moveAction.ReadValue<Vector2>();
        float x = input.x;
        float z = input.y;

        Vector3 moveDirWorld = (transform.right * x + transform.forward * z);
        isMoving = controller.velocity.sqrMagnitude > 0.01f;

        bool shiftHeld = shiftAction.IsPressed();
        bool shiftReleased = shiftAction.WasReleasedThisFrame();

        // Jump buffer (press a bit early)
        if (jumpAction.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        // --- State transitions ---
        if (isGrounded)
        {
            // Crouch while on ground (moving or not)
            state = shiftHeld ? MoveState.Crouching : MoveState.Normal;
        }
        else
        {
            // Only allow glide while falling
            if (shiftHeld && velocity.y <= 0f)
                state = MoveState.Gliding;
            else if (state == MoveState.Gliding)
                state = MoveState.Normal;
        }

        HandleCrouchSizing(shiftReleased);
        HandleCameraCrouch(); 

        //  Horizontal move 
        currentSpeedMultiplier = 1f;
        if (state == MoveState.Crouching)
            currentSpeedMultiplier *= crouchSpeedMultiplier;

        float airControl = isGrounded ? 1f : airControlMultiplier;
        Vector3 finalMove = moveDirWorld * (speed * speedMultiplier * currentSpeedMultiplier * airControl);

        controller.Move(finalMove * Time.deltaTime);

        //  Vertical / gravity 
        float effectiveGravity = gravity;

        if (state == MoveState.Gliding && !isGrounded)
        {
            effectiveGravity = gravity * glideGravityMultiplier;

            if (velocity.y < maxGlideFallSpeed)
                velocity.y = maxGlideFallSpeed;
        }

        velocity.y += effectiveGravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Ground truth AFTER Move() 
        isGrounded = controller.isGrounded;

        // Update coyote time AFTER movement (most accurate grounded)
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        // Execute jump using buffer + coyote time
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            // Leaving ground cancels crouch/glide state into normal jump
            state = MoveState.Normal;
            isGrounded = false;
        }

        // When grounded, kill downward velocity cleanly.
        if (isGrounded && velocity.y < 0f)
            velocity.y = 0f;
    }

    private void HandleCrouchSizing(bool shiftReleased)
    {
        // Only crouch affects height
        float targetHeight = (state == MoveState.Crouching) ? crouchHeight : standingHeight;

        // If trying to stand up but blocked by ceiling, stay crouched
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

        // Move camera down by the same amount the capsule shrinks
        float heightDelta = standingHeight - controller.height; 
        float targetY = standingCameraLocalY - heightDelta - crouchCameraExtraDrop;

        Vector3 local = cameraRoot.localPosition;
        local.y = Mathf.Lerp(local.y, targetY, Time.deltaTime * crouchTransitionSpeed);
        cameraRoot.localPosition = local;
    }

    private bool CanStandUp()
    {
        float radius = controller.radius;
        float standHeight = standingHeight;

        Vector3 bottom = transform.position + controller.center - Vector3.up * (controller.height / 2f) + Vector3.up * radius;
        Vector3 top = bottom + Vector3.up * (standHeight - 2f * radius);

        // Checks everything solid; adjust if you want a specific mask for ceilings/walls.
        return !Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
    }
}