using System.Collections.Generic;
using System.Globalization;
using Meta.XR.BuildingBlocks.AIBlocks;
using UnityEngine;
using UnityEngine.Events;

public class PaintingDetectionReader : MonoBehaviour
{
    [SerializeField] private ObjectDetectionAgent agent;
    [SerializeField, Range(0f, 1f)] private float confidenceThreshold = 0.8f;
    [SerializeField] private float spawnDistanceFromCamera = 0.5f;
    [SerializeField] private float sphereScale = 0.1f;

    [System.Serializable]
    public class PaintingDetectedEvent : UnityEvent<float> { }

    [SerializeField] private PaintingDetectedEvent onPaintingDetected = new();
    public PaintingDetectedEvent OnPaintingDetected => onPaintingDetected;

    private int personCountLastFrame;

    private void OnEnable()
    {
        if (agent != null)
        {
            agent.OnDetectionResponseReceived.AddListener(OnDetections);
        }
    }

    private void OnDisable()
    {
        if (agent != null)
        {
            agent.OnDetectionResponseReceived.RemoveListener(OnDetections);
        }
    }

    private void OnDetections(List<BoxData> boxes)
    {
        var personCount = 0;
        var bestConfidence = 0f;

        if (boxes != null)
        {
            foreach (var detection in boxes)
            {
                if (!TryParseLabelAndConfidence(detection.label, out var labelName, out var confidence))
                    continue;

                if (labelName != "person" || confidence < confidenceThreshold)
                    continue;

                personCount++;
                if (confidence > bestConfidence)
                {
                    bestConfidence = confidence;
                }
            }
        }

        // Spawn only for newly detected people so we do not create spheres every frame.
        if (personCount > personCountLastFrame)
        {
            var newPeopleCount = personCount - personCountLastFrame;
            SpawnSpheresInFrontOfMainCamera(newPeopleCount);
            onPaintingDetected.Invoke(bestConfidence);
            Debug.Log($"Person detected with confidence {bestConfidence:0.00}. Spawned {newPeopleCount} sphere(s).");
        }

        personCountLastFrame = personCount;
    }

    private void SpawnSpheresInFrontOfMainCamera(int count)
    {
        if (count <= 0)
            return;

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[PaintingDetectionReader] Cannot spawn sphere: no Main Camera found.");
            return;
        }

        var spawnPosition = mainCamera.transform.position + mainCamera.transform.forward * spawnDistanceFromCamera;
        for (var i = 0; i < count; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = spawnPosition;
            sphere.transform.localScale = Vector3.one * sphereScale;
        }
    }

    private static bool TryParseLabelAndConfidence(string rawLabel, out string labelName, out float confidence)
    {
        labelName = string.Empty;
        confidence = 0f;

        if (string.IsNullOrWhiteSpace(rawLabel))
            return false;

        int cut = rawLabel.LastIndexOf(' ');
        if (cut <= 0 || cut >= rawLabel.Length - 1)
            return false;

        labelName = rawLabel[..cut].Trim().ToLowerInvariant();
        return float.TryParse(rawLabel[(cut + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out confidence);
    }
}
