using UnityEngine;
using Unity.Netcode;

// Attach to Arrow prefab alongside NetworkObject component
[RequireComponent(typeof(Rigidbody))]
public class Arrow : NetworkBehaviour
{
    public int damage = 25;
    public ulong shooterOwnerId;

    // Set this on spawn to prevent friendly fire
    public bool shooterIsPink = false;

    [Header("Flight")]
    public float rotationSpeed = 15f; // How fast arrow rotates to match velocity direction

    private bool hasHit = false;
    private bool hasBounced = false;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Rotate arrow to face its velocity direction (gives realistic arc look)
        if (!hasHit && rb != null && rb.linearVelocity.sqrMagnitude > 0.5f)
        {
            Quaternion targetRot = Quaternion.LookRotation(rb.linearVelocity);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || hasHit) return;

        PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();

        if (player != null)
        {
            // Don't hit shooter
            if (player.OwnerClientId == shooterOwnerId) return;

            // Friendly fire prevention — check team tags
            bool targetIsPink = collision.gameObject.CompareTag("PinkTeam");
            if (targetIsPink == shooterIsPink) return;

            hasHit = true;
            player.TakeDamageServerRpc(damage);

            if (NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn();
        }
        else
        {
            // Hit environment — bounce once, then stick on second hit
            if (!hasBounced)
            {
                hasBounced = true;
                // Velocity reflection is handled by the Rigidbody's Physics Material
                // We just reduce speed after the bounce
                BounceClientRpc();
            }
            else
            {
                // Already bounced once — now stick
                hasHit = true;
                StickToSurfaceClientRpc();
            }
        }
    }

    [ClientRpc]
    private void BounceClientRpc()
    {
        if (rb != null)
        {
            // Reduce speed by half after bounce
            rb.linearVelocity *= 0.5f;
        }
    }

    [ClientRpc]
    private void StickToSurfaceClientRpc()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}