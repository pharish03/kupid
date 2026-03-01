using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
    [Header("Behavior")]
    public int maxBounces = 1;                 // bounce once
    public float destroyAfterLand = 2.5f;      // seconds after landing
    public float landSpeedThreshold = 1.0f;    // only used AFTER bounce is used

    private Rigidbody rb;
    private int bouncesUsed = 0;
    private bool hasLanded = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        // If you hit a target, delete immediately (your original behavior)
        if (collision.gameObject.CompareTag("Target"))
        {
            Debug.Log("hit " + collision.gameObject.name + "!");
            Destroy(gameObject);
            return;
        }

        // If we still have our bounce available, consume it and let physics bounce naturally.
        if (bouncesUsed < maxBounces)
        {
            bouncesUsed++;
            return;
        }

        // After bounce is used, the next collision counts as landing.
        // Optionally require it to be moving slow enough to "settle".
        if (rb.linearVelocity.magnitude <= landSpeedThreshold || landSpeedThreshold <= 0f)
        {
            Land();
        }
        else
        {
            // If it's still fast, you can either Land anyway or let it keep going.
            // Most people prefer landing immediately after the last bounce:
            Land();
        }
    }

    private void Land()
    {
        if (hasLanded) return;
        hasLanded = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        StartCoroutine(DestroyAfterSeconds(destroyAfterLand));
    }

    private IEnumerator DestroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }
}