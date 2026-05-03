using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform arrowSpawn;
    public GameObject arrowPrefab;
    public PlayerMovement playerMovement;

    [Header("Aiming")]
    public float aimMaxDistance = 200f;
    public LayerMask aimMask = ~0;

    [Header("Charge")]
    public float maxChargeTime = 1.0f;
    [Tooltip("Minimum charge (0-1) required to actually fire. Below this, releasing cancels the shot.")]
    [Range(0f, 1f)] public float minChargeToFire = 0.15f;
    public float minVelocity = 15f;
    public float maxVelocity = 60f;

    [Header("Cooldown")]
    [Tooltip("Time after firing before the player can draw again")]
    public float fireCooldown = 0.75f;

    [Header("Effects During Charge")]
    [Range(0.05f, 1f)] public float moveSlowMultiplier = 0.25f;
    public float normalFOV = 90f;
    public float zoomFOV = 35f;
    public float zoomLerpSpeed = 12f;

    [Header("Damage")]
    public int minDamage = 10;
    public int maxDamage = 50;

    [Header("Arrow Spawn Offset")]
    [Tooltip("How far forward from arrowSpawn to actually spawn the arrow")]
    public float spawnForwardOffset = 1.5f;

    [Header("Animation")]
    public Animator animator;

    private InputAction fireAction;
    private bool isCharging;
    private float charge01;
    private float cooldownTimer;

    void Awake()
    {
        fireAction = new InputAction("Fire", InputActionType.Button);
        fireAction.AddBinding("<Mouse>/leftButton");
        fireAction.AddBinding("<Gamepad>/rightTrigger");
    }

    void Start()
    {
        fireAction.Enable();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void OnDisable() => fireAction?.Disable();

    void Update()
    {
        if (playerCamera == null) return;

        // Tick cooldown
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // --- PRESS: Start drawing the bow ---
        if (fireAction.WasPressedThisFrame() && cooldownTimer <= 0f && !isCharging)
        {
            isCharging = true;
            charge01 = 0f;

            if (playerMovement != null)
                playerMovement.speedMultiplier = moveSlowMultiplier;

            if (animator != null)
            {
                animator.ResetTrigger("Fire");
                animator.SetBool("IsDrawing", true);
            }
        }

        // --- HOLD: Charge the bow ---
        if (isCharging && fireAction.IsPressed())
        {
            charge01 += Time.deltaTime / Mathf.Max(0.01f, maxChargeTime);
            charge01 = Mathf.Clamp01(charge01);

            if (animator != null)
                animator.SetFloat("ChargeAmount", charge01);
        }

        // --- RELEASE: Fire or cancel ---
        if (isCharging && fireAction.WasReleasedThisFrame())
        {
            if (charge01 >= minChargeToFire)
            {
                // Enough charge — fire the arrow
                FireArrow();
                cooldownTimer = fireCooldown;

                if (animator != null)
                {
                    animator.SetBool("IsDrawing", false);
                    animator.SetTrigger("Fire");
                    animator.SetFloat("ChargeAmount", 0f);
                }
            }
            else
            {
                // Not enough charge — cancel, no arrow fired
                if (animator != null)
                {
                    animator.SetBool("IsDrawing", false);
                    animator.SetFloat("ChargeAmount", 0f);
                }
            }

            isCharging = false;
            charge01 = 0f;

            if (playerMovement != null)
                playerMovement.speedMultiplier = 1f;
        }

        // --- Zoom FOV tied to charge ---
        float targetFov = Mathf.Lerp(normalFOV, zoomFOV, charge01);
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView, targetFov, Time.deltaTime * zoomLerpSpeed);
    }

    private void FireArrow()
    {
        Vector3 aimPoint = GetAimPoint();
        Vector3 spawnPos = arrowSpawn.position;
        Vector3 dir = (aimPoint - spawnPos).normalized;

        // Offset spawn forward to prevent arrow from spawning inside player/ground
        spawnPos += dir * spawnForwardOffset;

        GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.LookRotation(dir));

        // Configure arrow — damage scales with charge
        Arrow arrowScript = arrow.GetComponent<Arrow>();
        if (arrowScript != null)
        {
            arrowScript.damage = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, charge01));
            arrowScript.shooter = playerMovement.gameObject;
        }

        // Ignore collision between arrow and shooter
        Collider arrowCollider = arrow.GetComponent<Collider>();
        if (arrowCollider != null)
        {
            Collider[] shooterColliders = playerMovement.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in shooterColliders)
            {
                Physics.IgnoreCollision(arrowCollider, col);
            }

            Collider weaponCollider = GetComponent<Collider>();
            if (weaponCollider != null)
                Physics.IgnoreCollision(arrowCollider, weaponCollider);
        }

        // Arrow speed scales with charge
        float speed = Mathf.Lerp(minVelocity, maxVelocity, charge01);
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = dir * speed;
        }
    }

    private Vector3 GetAimPoint()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, aimMaxDistance, aimMask,
                QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.origin + ray.direction * aimMaxDistance;
    }
}