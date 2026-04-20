using System;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;
using Meta.XR.BuildingBlocks.AIBlocks;
using UnityEngine;

public class DetectionReader : MonoBehaviour
{
    private const string PassthroughCameraAccessTypeName = "Meta.XR.BuildingBlocks.AIBlocks.PassthroughCameraAccess, Meta.XR.BuildingBlocks.AIBlocks";

    [Header("Detection Sources")]
    [SerializeField] private ObjectDetectionAgent metaAgent;
    [SerializeField] private WebcamObjectDetectionAgent webcamAgent;
    [SerializeField] private bool allowWebcamFallback = true;

    [Header("Debug")]
    [SerializeField] private bool logDetectionBatches = true;
    [SerializeField] private bool logEachDetection = true;

    private enum DetectionSource
    {
        None,
        Meta,
        Webcam
    }

    private DetectionSource activeSource = DetectionSource.None;
    private readonly List<BoxData> latestDetections = new();

    private void OnEnable()
    {
        ConfigureDetectionSource();
    }

    private void OnDisable()
    {
        UnsubscribeActiveSource();
    }

    private void ConfigureDetectionSource()
    {
        UnsubscribeActiveSource();

        var useWebcamFallback = allowWebcamFallback && ShouldFallbackToWebcam();

        if (useWebcamFallback && webcamAgent != null)
        {
            activeSource = DetectionSource.Webcam;
            webcamAgent.OnDetectionResponseReceived.AddListener(OnDetections);
            webcamAgent.StartWebcam();
            Debug.Log("[DetectionReader] Webcam fallback enabled. Using webcam detections.");
            return;
        }

        if (metaAgent != null)
        {
            activeSource = DetectionSource.Meta;
            metaAgent.OnDetectionResponseReceived.AddListener(OnDetections);
            Debug.Log("[DetectionReader] Using Meta passthrough detections.");
            return;
        }

        if (allowWebcamFallback && webcamAgent != null)
        {
            activeSource = DetectionSource.Webcam;
            webcamAgent.OnDetectionResponseReceived.AddListener(OnDetections);
            webcamAgent.StartWebcam();
            Debug.Log("[DetectionReader] Meta agent missing. Using webcam detections.");
            return;
        }

        activeSource = DetectionSource.None;
        Debug.LogWarning("[DetectionReader] No detection source configured.");
    }

    private void UnsubscribeActiveSource()
    {
        if (activeSource == DetectionSource.Meta && metaAgent != null)
        {
            metaAgent.OnDetectionResponseReceived.RemoveListener(OnDetections);
        }
        else if (activeSource == DetectionSource.Webcam && webcamAgent != null)
        {
            webcamAgent.OnDetectionResponseReceived.RemoveListener(OnDetections);
        }

        activeSource = DetectionSource.None;
    }

    private bool ShouldFallbackToWebcam()
    {
        if (metaAgent == null)
        {
            return true;
        }

        return !IsPassthroughCameraFound();
    }

    private static bool IsPassthroughCameraFound()
    {
        var cameraAccessType = Type.GetType(PassthroughCameraAccessTypeName);
        if (cameraAccessType == null)
        {
            return false;
        }

        var cameraAccessObjects = Resources.FindObjectsOfTypeAll(cameraAccessType);
        if (cameraAccessObjects == null || cameraAccessObjects.Length == 0)
        {
            return false;
        }

        var isPlayingProperty = cameraAccessType.GetProperty("IsPlaying", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (isPlayingProperty == null || isPlayingProperty.PropertyType != typeof(bool))
        {
            return true;
        }

        foreach (var cameraAccess in cameraAccessObjects)
        {
            var value = isPlayingProperty.GetValue(cameraAccess);
            if (value is bool isPlaying && isPlaying)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDetections(List<BoxData> boxes)
    {
        latestDetections.Clear();
        if (boxes != null)
        {
            latestDetections.AddRange(boxes);
        }

        if (logDetectionBatches)
        {
            Debug.Log($"[DetectionReader] Source={activeSource} Frame={Time.frameCount} Detections={latestDetections.Count}");
        }

        if (logEachDetection)
        {
            for (int i = 0; i < latestDetections.Count; i++)
            {
                var detection = latestDetections[i];
                var parsed = TryParseLabelAndConfidence(detection.label, out var labelName, out var confidence);
                var confidenceText = parsed ? confidence.ToString("0.00", CultureInfo.InvariantCulture) : "N/A";
                var objectName = parsed ? labelName : detection.label;

                Debug.Log(
                    $"[DetectionReader] #{i} Source={activeSource} Object={objectName} Confidence={confidenceText} RawLabel=\"{detection.label}\" " +
                    $"Position={detection.position} Scale={detection.scale} Rotation={detection.rotation.eulerAngles}");
            }
        }
    }

    public bool DetectSpecificObject(string objectName, float threshold)
    {
        var detectedCount = CountSpecificObject(objectName, threshold);
        if (detectedCount <= 0)
        {
            return false;
        }

        if (TryGetBestConfidenceForObject(objectName, threshold, out var bestConfidence))
        {
            OnSpecificObjectDetected(objectName.Trim().ToLowerInvariant(), bestConfidence); // your action
        }

        return true;
    }

    public int CountSpecificObject(string objectName, float threshold)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return 0;
        }

        var targetName = objectName.Trim().ToLowerInvariant();
        var detectedCount = 0;

        foreach (var b in latestDetections)
        {
            if (!TryParseLabelAndConfidence(b.label, out var labelName, out var conf))
                continue;

            if (labelName == targetName && conf > threshold)
            {
                detectedCount++;
            }
        }

        return detectedCount;
    }

    private bool TryGetBestConfidenceForObject(string objectName, float threshold, out float bestConfidence)
    {
        bestConfidence = 0f;

        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        var targetName = objectName.Trim().ToLowerInvariant();

        foreach (var b in latestDetections)
        {
            if (!TryParseLabelAndConfidence(b.label, out var labelName, out var conf))
                continue;

            if (labelName != targetName || conf <= threshold)
                continue;

            if (conf > bestConfidence)
            {
                bestConfidence = conf;
            }
        }

        return bestConfidence > 0f;
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

    private void OnSpecificObjectDetected(string objectName, float confidence)
    {
        Debug.Log($"{objectName} detected with confidence {confidence:0.00}");
        // Do your action here (spawn, play sound, call method, etc.)
    }

}
