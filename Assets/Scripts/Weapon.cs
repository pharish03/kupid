using System.Collections;
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
    public float maxChargeTime = 1.2f;
    public float minVelocity = 15f;
    public float maxVelocity = 60f;

    [Header("Effects During Charge")]
    [Range(0.05f, 1f)] public float moveSlowMultiplier = 0.25f;
    public float normalFOV = 90f;
    public float zoomFOV = 60f;
    public float zoomLerpSpeed = 12f;

    [Header("Arrow Lifetime")]
    public float arrowPrefabLifeTime = 3f;

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
            enabled = false; // Only owner fires
            return;
        }

        fireAction.Enable();

        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera != null) normalFOV = playerCamera.fieldOfView;
    }

    void OnDisable() => fireAction?.Disable();

    void Update()
    {
        if (!IsOwner) return;
        if (playerCamera == null) return;

        if (fireAction.WasPressedThisFrame())
        {
            isCharging = true;
            charge01 = 0f;
            if (playerMovement != null)
                playerMovement.speedMultiplier = moveSlowMultiplier;
        }

        if (isCharging && fireAction.IsPressed())
        {
            charge01 += Time.deltaTime / Mathf.Max(0.01f, maxChargeTime);
            charge01 = Mathf.Clamp01(charge01);
        }

        if (isCharging && fireAction.WasReleasedThisFrame())
        {
            bool isPink = gameObject.CompareTag("PinkTeam");
            RequestFireArrowServerRpc(charge01, arrowSpawn.position, GetAimPoint(), isPink);
            isCharging = false;
            charge01 = 0f;
            if (playerMovement != null)
                playerMovement.speedMultiplier = 1f;
        }

        float targetFov = isCharging ? zoomFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * zoomLerpSpeed);
    }

    private Vector3 GetAimPoint()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, aimMaxDistance, aimMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.origin + ray.direction * aimMaxDistance;
    }

    // Server spawns the arrow so it's authoritative
    [ServerRpc]
    private void RequestFireArrowServerRpc(float chargeAmount, Vector3 spawnPos, Vector3 aimPoint, bool isPink)
    {
        Vector3 dir = (aimPoint - spawnPos).normalized;
        GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.LookRotation(dir, Vector3.up));

        Arrow arrowScript = arrow.GetComponent<Arrow>();
        if (arrowScript != null)
        {
            arrowScript.damage = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, chargeAmount));
            arrowScript.shooterOwnerId = OwnerClientId;
            arrowScript.shooterIsPink = isPink;
        }

        NetworkObject netObj = arrow.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn();

        float velocity = Mathf.Lerp(minVelocity, maxVelocity, chargeAmount);
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(dir * velocity, ForceMode.Impulse);
        }

        StartCoroutine(DestroyArrowAfterTime(arrow, arrowPrefabLifeTime));
    }

    private IEnumerator DestroyArrowAfterTime(GameObject arrow, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (arrow != null)
        {
            NetworkObject netObj = arrow.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn();
            else
                Destroy(arrow);
        }
    }
}