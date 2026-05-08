using System.Collections;
using UnityEngine;

public class Grappling : MonoBehaviour
{
    [Header("References")]
    private PlayerMovement playerMovement;
    private CharacterController characterController;
    public Transform cam;
    public Transform gunTip;
    public LayerMask whatIsGrappleable;
    public LineRenderer lr;

    [Header("Grappling")]
    public float maxGrappleDistance = 25f;
    public float grappleDelayTime = 0.1f;
    private Vector3 grapplePoint;

    [Header("Grapple Movement")]
    [Tooltip("How fast the player travels along the grapple arc.")]
    public float grappleSpeed = 12f;
    [Tooltip("How much upward arc is added at launch. 0 = straight line, 1+ = more arc.")]
    public float arcHeight = 2f;
    [Tooltip("How close to the grapple point before we stop (in units).")]
    public float arrivalDistance = 1.2f;

    [Header("Cooldown")]
    public float grappleCd = 1f;
    private float grapplingCdTimer;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.E;

    private bool grappling;
    private Coroutine grappleMoveCoroutine;

    // Later im gonna add an animation to the grapple hook 
    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(grappleKey)) StartGrapple();
        if (grapplingCdTimer > 0) grapplingCdTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (grappling)
            lr.SetPosition(0, gunTip.position);
    }

    private void StartGrapple()
    {
        if (grapplingCdTimer > 0) return;

        // Cancel any existing grapple move
        if (grappleMoveCoroutine != null)
        {
            StopCoroutine(grappleMoveCoroutine);
            grappleMoveCoroutine = null;
        }

        grappling = true;

        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;
            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        {
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;
            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        lr.enabled = true;
        lr.SetPosition(1, grapplePoint);
    }

    private void StopGrapple()
    {
        grappling = false;
        grapplingCdTimer = grappleCd;
        lr.enabled = false;

        if (grappleMoveCoroutine != null)
        {
            StopCoroutine(grappleMoveCoroutine);
            grappleMoveCoroutine = null;
        }
    }

    private void ExecuteGrapple()
    {
        grappleMoveCoroutine = StartCoroutine(GrappleMove());
    }

    private IEnumerator GrappleMove()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = grapplePoint;

        float totalDist = Vector3.Distance(startPos, endPos);
        float travelTime = totalDist / grappleSpeed;
        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);


            Vector3 flatPos = Vector3.Lerp(startPos, endPos, t);
            float arcOffset = arcHeight * Mathf.Sin(t * Mathf.PI);
            Vector3 targetPos = flatPos + Vector3.up * arcOffset;

            Vector3 moveStep = (targetPos - transform.position);
            characterController.Move(moveStep);

            if (Vector3.Distance(transform.position, endPos) < arrivalDistance)
                break;

            yield return null;
        }

        StopGrapple();
    }
}