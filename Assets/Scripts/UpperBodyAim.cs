using UnityEngine;

public class UpperBodyAim : MonoBehaviour
{
    public Animator animator;
    public Transform cameraTransform;

    [Tooltip("How much the spine follows camera pitch")]
    [Range(0f, 1f)] public float verticalAimWeight = 0.8f;

    [Tooltip("Horizontal offset to correct animation facing direction")]
    public float horizontalOffset = 90f;

    private Transform spineBone;
    private Transform chestBone;

    void Start()
    {
        if (animator != null)
        {
            spineBone = animator.GetBoneTransform(HumanBodyBones.Spine);
            chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
        }
    }

    void LateUpdate()
    {
        if (spineBone == null || cameraTransform == null) return;

        float pitch = cameraTransform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        // Split the rotation across spine and chest for natural look
        float spineShare = pitch * verticalAimWeight * 0.5f;
        float chestShare = pitch * verticalAimWeight * 0.5f;

        // Horizontal offset
        Quaternion yawOffset = Quaternion.AngleAxis(
            horizontalOffset,
            spineBone.InverseTransformDirection(transform.up));

        // Apply to spine
        Quaternion spinePitch = Quaternion.AngleAxis(
            spineShare,
            spineBone.InverseTransformDirection(transform.right));
        spineBone.localRotation = spineBone.localRotation * yawOffset * spinePitch;

        // Apply to chest
        if (chestBone != null)
        {
            Quaternion chestPitch = Quaternion.AngleAxis(
                chestShare,
                chestBone.InverseTransformDirection(transform.right));
            chestBone.localRotation = chestBone.localRotation * chestPitch;
        }
    }
}