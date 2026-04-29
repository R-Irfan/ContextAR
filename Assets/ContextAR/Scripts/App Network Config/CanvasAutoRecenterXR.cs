using UnityEngine;

public class CanvasAutoRecenterXR : MonoBehaviour
{
    public Transform centerEyeAnchor;

    public float distance = 1.5f;
    public float angleThreshold = 50f;
    public float moveSpeed = 4f;
    public float rotationSpeed = 6f;

    private bool isRecentering;

    void Update()
    {
        if (centerEyeAnchor == null) return;

        Vector3 toCanvas = (transform.position - centerEyeAnchor.position).normalized;
        float angle = Vector3.Angle(centerEyeAnchor.forward, toCanvas);

        if (angle > angleThreshold)
        {
            isRecentering = true;
        }

        if (isRecentering)
        {
            Vector3 targetPos = centerEyeAnchor.position + centerEyeAnchor.forward * distance;

            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                Time.deltaTime * moveSpeed
            );

            Quaternion lookRot = Quaternion.LookRotation(
                transform.position - centerEyeAnchor.position
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRot,
                Time.deltaTime * rotationSpeed
            );

            float newAngle = Vector3.Angle(
                centerEyeAnchor.forward,
                (transform.position - centerEyeAnchor.position).normalized
            );

            if (newAngle < 5f)
                isRecentering = false;
        }
    }
}