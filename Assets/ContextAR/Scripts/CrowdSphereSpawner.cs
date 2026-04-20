using UnityEngine;

public class CrowdSphereSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AmbientNoiseMonitor ambientNoiseMonitor;
    [SerializeField] private Transform cameraTransform;

    [Header("Spawn")]
    [SerializeField] private float spawnDistanceFromCamera = 0.5f;
    [SerializeField] private float sphereScale = 0.1f;
    [SerializeField] private bool spawnIfCrowdAlreadyConfirmedOnEnable = true;

    [Header("Debug")]
    [SerializeField] private bool logSpawns = true;

    private bool spawnedForCurrentCrowdState;
    private bool missingMonitorWarningShown;

    private void Awake()
    {
        if (ambientNoiseMonitor == null)
        {
            ambientNoiseMonitor = FindAnyObjectByType<AmbientNoiseMonitor>();
        }
    }

    private void OnEnable()
    {
        if (ambientNoiseMonitor != null)
        {
            ambientNoiseMonitor.OnCrowdStateChanged.AddListener(OnCrowdStateChanged);

            if (spawnIfCrowdAlreadyConfirmedOnEnable && ambientNoiseMonitor.IsCrowdConfirmed)
            {
                SpawnSphereInFrontOfCamera();
                spawnedForCurrentCrowdState = true;
            }
        }
        else if (!missingMonitorWarningShown)
        {
            missingMonitorWarningShown = true;
            Debug.LogWarning("[CrowdSphereSpawner] AmbientNoiseMonitor not found. Crowd spawning is disabled.");
        }
    }

    private void OnDisable()
    {
        if (ambientNoiseMonitor != null)
        {
            ambientNoiseMonitor.OnCrowdStateChanged.RemoveListener(OnCrowdStateChanged);
        }
    }

    private void OnCrowdStateChanged(bool isCrowdConfirmed, int personCount, float currentDb)
    {
        if (!isCrowdConfirmed)
        {
            spawnedForCurrentCrowdState = false;
            return;
        }

        if (spawnedForCurrentCrowdState)
        {
            return;
        }

        SpawnSphereInFrontOfCamera();
        spawnedForCurrentCrowdState = true;
    }

    private void SpawnSphereInFrontOfCamera()
    {
        var targetCameraTransform = ResolveCameraTransform();
        if (targetCameraTransform == null)
        {
            Debug.LogWarning("[CrowdSphereSpawner] Cannot spawn sphere: no camera transform available.");
            return;
        }

        var spawnPosition = targetCameraTransform.position + targetCameraTransform.forward * spawnDistanceFromCamera;
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "CrowdDetectedSphere";
        sphere.transform.position = spawnPosition;
        sphere.transform.localScale = Vector3.one * sphereScale;

        if (logSpawns)
        {
            Debug.Log($"[CrowdSphereSpawner] Spawned sphere at {spawnPosition}.");
        }
    }

    private Transform ResolveCameraTransform()
    {
        if (cameraTransform != null)
        {
            return cameraTransform;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        return null;
    }
}
