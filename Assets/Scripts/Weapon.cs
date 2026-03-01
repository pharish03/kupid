using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
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
    public float maxChargeTime = 1.2f;
    public float minVelocity = 15f;
    public float maxVelocity = 60f;

    [Header("Effects During Charge")]
    [Range(0.05f,1f)] public float moveSlowMultiplier = 0.25f;
    public float normalFOV = 90f;
    public float zoomFOV = 60f;
    public float zoomLerpSpeed= 12f;

    [Header("Arrow Lifetime")]
    public float arrowPrefabLifeTime = 3f;

    private InputAction fireAction;
    private bool isCharging;
    private float charge01;

    void Awake()
    {
        fireAction = new InputAction("Fire", InputActionType.Button);
        fireAction.AddBinding("<Mouse>/leftButton");
        fireAction.AddBinding("<Gamepad>/rightTrigger");
    }

    void OnEnable() => fireAction.Enable();
    void OnDisable() => fireAction.Disable();

    void Start()
    {
        if(playerCamera == null) playerCamera = Camera.main;
        if(playerCamera != null) normalFOV = playerCamera.fieldOfView;
    }

    void Update()
    {
        if (playerCamera == null) return;
        if (fireAction.WasPressedThisFrame())
        {
            isCharging = true;
            charge01 = 0f;

            if(playerMovement != null)
            {
                playerMovement.speedMultiplier = moveSlowMultiplier;
            }
        }

        //while held, build charge
        if(isCharging && fireAction.IsPressed())
        {
            charge01 += Time.deltaTime / Mathf.Max(0.01f, maxChargeTime);
            charge01 = Mathf.Clamp01(charge01);
        }

        if(isCharging && fireAction.WasReleasedThisFrame())
        {
            FireChargedArrow(charge01);
            isCharging = false;
            charge01 = 0f;

            if (playerMovement != null)
            {
                playerMovement.speedMultiplier = 1f;
            }

        }

        // zoom while charging
        float targetFov = isCharging ? zoomFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * zoomLerpSpeed);
    }

    private void FireChargedArrow(float chargeAmount01)
    {
        if(arrowPrefab == null || arrowSpawn == null) return;
        
        // aim point from crosshair
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, aimMaxDistance, aimMask, QueryTriggerInteraction.Ignore))
            aimPoint = hit.point;
        else
            aimPoint = ray.origin + ray.direction * aimMaxDistance;

        // Direction from bow spawn -> aim point
        Vector3 dir = (aimPoint - arrowSpawn.position).normalized;

        GameObject arrow = Instantiate(arrowPrefab, arrowSpawn.position, Quaternion.LookRotation(dir, Vector3.up));

        float velocity = Mathf.Lerp(minVelocity, maxVelocity, chargeAmount01);

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
        if (arrow != null) Destroy(arrow);
    }
}
