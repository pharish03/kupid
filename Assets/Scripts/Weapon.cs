using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class Weapon : NetworkBehaviour
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
    public float maxChargeTime = 1f;
    public float minVelocity = 15f;
    public float maxVelocity = 60f;

    [Header("Effects During Charge")]
    [Range(0.05f, 1f)] public float moveSlowMultiplier = 0.25f;
    public float normalFOV = 90f;
    public float zoomFOV = 35f;
    public float zoomLerpSpeed = 20f;

    [Header("Damage")]
    public int minDamage = 10;
    public int maxDamage = 50;

    private InputAction fireAction;
    private bool isCharging;
    private float charge01;

    void Awake()
    {
        fireAction = new InputAction("Fire", InputActionType.Button);
        fireAction.AddBinding("<Mouse>/leftButton");
        fireAction.AddBinding("<Gamepad>/rightTrigger");
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        fireAction.Enable();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void OnDisable() => fireAction?.Disable();

    void Update()
    {
        if (!IsOwner || playerCamera == null) return;

        // Start charging
        if (fireAction.WasPressedThisFrame())
        {
            isCharging = true;
            charge01 = 0f;
            if (playerMovement != null)
                playerMovement.speedMultiplier = moveSlowMultiplier;
        }

        // Accumulate charge
        if (isCharging && fireAction.IsPressed())
        {
            charge01 += Time.deltaTime / Mathf.Max(0.01f, maxChargeTime);
            charge01 = Mathf.Clamp01(charge01);
        }

        // Release — fire the arrow
        if (isCharging && fireAction.WasReleasedThisFrame())
        {
            bool isPink = gameObject.CompareTag("PinkTeam");
            Vector3 aimPoint = GetAimPoint();
            Vector3 spawnPos = arrowSpawn.position;
            Vector3 dir = (aimPoint - spawnPos).normalized;

            RequestFireArrowServerRpc(charge01, spawnPos, dir, isPink);

            isCharging = false;
            charge01 = 0f;
            if (playerMovement != null)
                playerMovement.speedMultiplier = 1f;
        }

        // Zoom FOV tied to charge progress for gradual zoom
        float targetFov = Mathf.Lerp(normalFOV, zoomFOV, charge01);
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView, targetFov, Time.deltaTime * zoomLerpSpeed);
    }

    private Vector3 GetAimPoint()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, aimMaxDistance, aimMask,
                QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.origin + ray.direction * aimMaxDistance;
    }

    [ServerRpc]
    private void RequestFireArrowServerRpc(
        float chargeAmount, Vector3 spawnPos, Vector3 direction, bool isPink)
    {
        GameObject arrow = Instantiate(
            arrowPrefab, spawnPos, Quaternion.LookRotation(direction));

        // Configure arrow
        Arrow arrowScript = arrow.GetComponent<Arrow>();
        if (arrowScript != null)
        {
            arrowScript.damage = Mathf.RoundToInt(
                Mathf.Lerp(minDamage, maxDamage, chargeAmount));
            arrowScript.shooterOwnerId = OwnerClientId;
            arrowScript.shooterIsPink = isPink;
        }

        // Ignore collision between arrow and shooter
        Collider arrowCollider = arrow.GetComponent<Collider>();
        if (arrowCollider != null)
        {
            Collider[] shooterColliders = GetComponentsInParent<Collider>(true);
            foreach (Collider col in shooterColliders)
            {
                Physics.IgnoreCollision(arrowCollider, col);
            }
        }

        // Spawn on network
        NetworkObject netObj = arrow.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn();

        // Set velocity directly — predictable regardless of mass
        float speed = Mathf.Lerp(minVelocity, maxVelocity, chargeAmount);
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Arrow manages its own lifetime via maxLifetime — no coroutine needed
    }
}