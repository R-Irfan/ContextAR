using UnityEngine;

public class OVRDotProjector : MonoBehaviour
{
    [Header("OVR Gaze Source (IMPORTANT: assign CenterEyeAnchor or GazeManager)")]
    public Transform rayOrigin;

    [Header("References")]
    public Transform dotVisual;
    public LineRenderer lineRenderer;

    [Header("Settings")]
    public float maxDistance = 20f;
    public float surfaceOffset = 0.002f;
    public float dotScale = 0.01f;

    [Header("Layers")]
    public LayerMask worldLayers = ~0;

    [Header("Options")]
    public bool showRay = true;
    public bool showDot = true;

    void Start()
    {
        if (dotVisual != null)
            dotVisual.localScale = Vector3.one * dotScale;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = showRay;
        }
    }

    void Update()
    {
        if (rayOrigin == null)
            return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        Vector3 endPoint = ray.origin + ray.direction * maxDistance;
        Vector3 hitNormal = -ray.direction;
        bool hasHit = false;

        // 1. World physics hit (ONLY for visualization)
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, worldLayers))
        {
            endPoint = hit.point;
            hitNormal = hit.normal;
            hasHit = true;
        }

        // 2. Place dot (if enabled)
        if (dotVisual != null && showDot)
        {
            dotVisual.gameObject.SetActive(true);

            dotVisual.position = endPoint + hitNormal * surfaceOffset;
            dotVisual.rotation = Quaternion.LookRotation(hitNormal);
            dotVisual.localScale = Vector3.one * dotScale;
        }

        // 3. Draw ray (always matches OVR gaze direction)
        if (lineRenderer != null && showRay)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, ray.origin);
            lineRenderer.SetPosition(1, endPoint);
        }

        // 4. Hide dot if nothing hit
        if (!hasHit && dotVisual != null && showDot)
        {
            dotVisual.gameObject.SetActive(false);
        }
    }
}