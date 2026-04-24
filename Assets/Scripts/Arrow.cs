using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : NetworkBehaviour
{
    [Header("Damage")]
    public int damage = 25;

    [Header("Bounce Settings")]
    [Tooltip("Max number of bounces before sticking")]
    public int maxBounces = 1;
    [Tooltip("Speed multiplier after each bounce.")]
    [Range(0.5f, 1f)]
    public float bounceSpeedRetention = 0.85f;

    [Header("Flight")]
    [Tooltip("If true, arrow aligns to velocity every frame for a realistic arc.")]
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

        // Critical for fast projectiles — prevents tunneling through walls
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
        // Align the arrow model to face its velocity direction
        if (alignToVelocity && !hasHit && rb.linearVelocity.sqrMagnitude > 1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }

        // Safety despawn so stray arrows don't live forever
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
        // Only the server decides what happens on hit
        if (!IsServer || hasHit) return;

        // --- Player hit ---
        PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();
        if (player != null)
        {
            // Ignore the shooter
            if (player.OwnerClientId == shooterOwnerId) return;

            // Friendly fire check
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
            // Manual bounce: reflect velocity off the surface normal
            Vector3 incomingVel = rb.linearVelocity;
            Vector3 surfaceNormal = collision.contacts[0].normal;

            Vector3 reflectedVel = Vector3.Reflect(incomingVel, surfaceNormal);
            reflectedVel *= bounceSpeedRetention;

            rb.linearVelocity = reflectedVel;

            bounceCount++;
        }
        else
        {
            // Out of bounces — stick to the surface
            hasHit = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            // NetworkRigidbody syncs this to all clients automatically,
            // so no ClientRpc needed

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