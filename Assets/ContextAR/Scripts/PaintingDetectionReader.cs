using System.Collections.Generic;
using System.Globalization;
using Meta.XR.BuildingBlocks.AIBlocks;
using UnityEngine;
using UnityEngine.Events;

public class PaintingDetectionReader : MonoBehaviour
{
    [SerializeField] private ObjectDetectionAgent agent;
    [SerializeField, Range(0f, 1f)] private float confidenceThreshold = 0.8f;
    [SerializeField] private bool triggerOnlyOnEnter = true;

    [System.Serializable]
    public class PaintingDetectedEvent : UnityEvent<float> { }

    [SerializeField] private PaintingDetectedEvent onPaintingDetected = new();
    public PaintingDetectedEvent OnPaintingDetected => onPaintingDetected;

    private bool paintingDetectedLastFrame;

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
        var foundPainting = false;
        var bestConfidence = 0f;

        if (boxes != null)
        {
            foreach (var detection in boxes)
            {
                if (!TryParseLabelAndConfidence(detection.label, out var labelName, out var confidence))
                    continue;

                if (labelName != "painting" || confidence < confidenceThreshold)
                    continue;

                foundPainting = true;
                if (confidence > bestConfidence)
                {
                    bestConfidence = confidence;
                }
            }
        }

        if (foundPainting && (!triggerOnlyOnEnter || !paintingDetectedLastFrame))
        {
            onPaintingDetected.Invoke(bestConfidence);
            Debug.Log($"Painting detected with confidence {bestConfidence:0.00}");
        }

        paintingDetectedLastFrame = foundPainting;
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
