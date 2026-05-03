using System.Collections;
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
    public Transform cameraRoot;
    public float crouchCameraExtraDrop = 0f;

    [Header("Glide")]
    public float airControlMultiplier = 0.65f;
    public float glideGravityMultiplier = 0.25f;
    public float maxGlideFallSpeed = -6f;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Respawn")]
    public Transform spawnPoint;
    public float respawnDelay = 5f;

    [Header("Animation")]
    public Animator animator;

    public bool IsDead => currentHealth <= 0;
    public bool IsRespawning { get; private set; }
    public float RespawnTimer { get; private set; }

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

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        standingHeight = controller.height;
        if (cameraRoot != null)
            standingCameraLocalY = cameraRoot.localPosition.y;

        currentHealth = maxHealth;

        // If no spawn point assigned, create one at starting position
        if (spawnPoint == null)
        {
            GameObject spawnObj = new GameObject("SpawnPoint");
            spawnObj.transform.position = transform.position;
            spawnPoint = spawnObj.transform;
        }

        InitInput();
    }

    private void InitInput()
    {
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

    void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        shiftAction?.Disable();
    }

    void Update()
    {
        if (IsDead) return;

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

        // Animation
        if (animator != null)
        {
            animator.SetFloat("MoveX", input.x, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveY", input.y, 0.1f, Time.deltaTime);
        }
    }

    // ==================== HEALTH & DAMAGE ====================

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"Player took {damage} damage. Health: {currentHealth}");

        if (IsDead)
        {
            Die();
            StartCoroutine(RespawnCountdown());
        }
    }

    private void Die()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        controller.enabled = false;

        moveAction?.Disable();
        jumpAction?.Disable();
        shiftAction?.Disable();

        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }

    // ==================== RESPAWN ====================

    private IEnumerator RespawnCountdown()
    {
        IsRespawning = true;
        RespawnTimer = respawnDelay;

        while (RespawnTimer > 0f)
        {
            RespawnTimer -= Time.deltaTime;
            yield return null;
        }

        RespawnTimer = 0f;
        Respawn();
    }

    private void Respawn()
    {
        currentHealth = maxHealth;
        velocity = Vector3.zero;

        controller.enabled = false;
        transform.position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        controller.enabled = true;

        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;

        moveAction?.Enable();
        jumpAction?.Enable();
        shiftAction?.Enable();

        IsRespawning = false;
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    // ==================== CROUCH ====================

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