using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : NetworkBehaviour
{
    [Header("Damage")]
    public int damage = 25;

    [Header("Bounce Settings")]
    [Tooltip("Max number of bounces before sticking (Sova = 1)")]
    public int maxBounces = 1;
    [Tooltip("Speed multiplier after each bounce. 0.85 keeps it fast like Sova.")]
    [Range(0.5f, 1f)]
    public float bounceSpeedRetention = 0.85f;

    [Header("Flight")]
    public bool alignToVelocity = true;

    [Header("Lifetime")]
    public float maxLifetime = 8f;
    public float stickDestroyDelay = 4f;

    // Set by the shooter script on spawn
    [HideInInspector] public ulong shooterOwnerId;
    [HideInInspector] public bool shooterIsPink = false;

    private Rigidbody rb;
    private int bounceCount = 0;
    private bool hasHit = false;
    private float aliveTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        aliveTimer = 0f;
    }

    void Update()
    {
        if (alignToVelocity && !hasHit && rb.linearVelocity.sqrMagnitude > 1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }

        if (IsServer)
        {
            aliveTimer += Time.deltaTime;
            if (aliveTimer >= maxLifetime)
            {
                DespawnArrow();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || hasHit) return;

        // --- Player hit ---
        PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();
        if (player != null)
        {
            if (player.OwnerClientId == shooterOwnerId) return;

            bool targetIsPink = collision.gameObject.CompareTag("PinkTeam");
            if (targetIsPink == shooterIsPink) return;

            hasHit = true;
            player.TakeDamageServerRpc(damage);
            DespawnArrow();
            return;
        }

        // --- Environment hit ---
        if (bounceCount < maxBounces)
        {
            // Manual bounce using reflection
            Vector3 incomingVel = rb.linearVelocity;
            Vector3 surfaceNormal = collision.contacts[0].normal;
            Vector3 reflectedVel = Vector3.Reflect(incomingVel, surfaceNormal);
            reflectedVel *= bounceSpeedRetention;

            rb.linearVelocity = reflectedVel;
            bounceCount++;
        }
        else
        {
            // Out of bounces — stick
            hasHit = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            Invoke(nameof(DespawnArrow), stickDestroyDelay);
        }
    }

    private void DespawnArrow()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}