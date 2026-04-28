using UnityEngine;

public class ForwardUIRayPointer : MonoBehaviour
{
    [Header("Ray Settings")]
    public float maxDistance = 10f;
    public LayerMask uiLayer;

    [Header("Pointer Sphere (assign in Inspector)")]
    public Transform pointerSphere;

    void Start()
    {
        if (pointerSphere == null)
        {
            Debug.LogError("Pointer Sphere not assigned.");
            enabled = false;
            return;
        }

        pointerSphere.gameObject.SetActive(false);
    }

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.green);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, uiLayer))
        {
            pointerSphere.gameObject.SetActive(true);

            pointerSphere.position = hit.point;

            pointerSphere.rotation =
                Quaternion.LookRotation(hit.normal);
        }
        else
        {
            pointerSphere.gameObject.SetActive(false);
        }

    }
}