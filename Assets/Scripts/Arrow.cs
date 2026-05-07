using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 25;

    [Header("Bounce Settings")]
    [Tooltip("Max number of bounces before sticking (Sova = 1)")]
    public int maxBounces = 1;
    [Tooltip("Speed multiplier after each bounce.")]
    [Range(0.5f, 1f)]
    public float bounceSpeedRetention = 0.85f;

    [Header("Flight")]
    public bool alignToVelocity = true;

    [Header("Lifetime")]
    public float maxLifetime = 8f;
    public float stickDestroyDelay = 4f;

    // Set by Weapon on spawn
    [HideInInspector] public GameObject shooter;

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

    void Update()
    {
        if (alignToVelocity && !hasHit && rb.linearVelocity.sqrMagnitude > 1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }

        aliveTimer += Time.deltaTime;
        if (aliveTimer >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // --- Ignore shooter ---
        if (collision.gameObject == shooter) return;

        Debug.Log("Hit something, its layer is:" + collision.gameObject.layer);
        // --- Enemy hit --- (check FIRST, before any bounce logic)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Debug.Log("Hit an Enemy");
            hasHit = true;
            EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
            if (enemy != null)
                enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // --- Player hit ---
        PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();
        if (player != null)
        {
            hasHit = true;
            player.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // --- Environment hit ---
        if (bounceCount < maxBounces)
        {
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
            Destroy(gameObject, stickDestroyDelay);
        }
    }
}