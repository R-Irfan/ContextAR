using System;
using System.Collections.Generic;
using System.Globalization;
using Meta.XR.BuildingBlocks.AIBlocks;
using UnityEngine;
using UnityEngine.Events;

public class ObjectDetectionDwellTracker : MonoBehaviour
{
    public enum DwellRange
    {
        NotDetected = 0,
        LessThanShortThreshold = 1,
        BetweenThresholds = 2,
        GreaterThanOrEqualLongThreshold = 3
    }

    [Serializable]
    public class DwellRangeChangedEvent : UnityEvent<DwellRange, float> { }

    [Header("Detection Source")]
    [SerializeField] private ObjectDetectionAgent metaAgent;
    [SerializeField] private WebcamObjectDetectionAgent webcamAgent;

    [Header("Target")]
    [SerializeField] private string targetLabel = "tv monitor";
    [SerializeField, Range(0f, 1f)] private float minConfidence = 0.7f;

    [Header("Dwell Thresholds (seconds)")]
    [SerializeField, Min(0f)] private float shortThresholdSeconds = 5f;
    [SerializeField, Min(0f)] private float longThresholdSeconds = 15f;
    [SerializeField, Min(0f)] private float missingGraceSeconds = 0.35f;

    [Header("Events")]
    [SerializeField] private DwellRangeChangedEvent onDwellRangeChanged = new();

    [Header("Debug")]
    [SerializeField] private bool logRangeChanges = true;

    private string _normalizedTargetLabel = string.Empty;
    private float _lastSeenTime = float.NegativeInfinity;
    private float _dwellStartTime = float.NegativeInfinity;
    private float _currentDwellSeconds;
    private bool _isTargetDetected;
    private DwellRange _currentRange = DwellRange.NotDetected;

    public DwellRange CurrentRange => _currentRange;
    public float CurrentDwellSeconds => _currentDwellSeconds;
    public bool IsTargetDetected => _isTargetDetected;
    public DwellRangeChangedEvent OnDwellRangeChanged => onDwellRangeChanged;

    private void Awake()
    {
        NormalizeTargetLabel();
    }

    private void OnEnable()
    {
        NormalizeTargetLabel();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        if (longThresholdSeconds < shortThresholdSeconds)
        {
            longThresholdSeconds = shortThresholdSeconds;
        }

        NormalizeTargetLabel();
    }

    private void Update()
    {
        RefreshState(Time.time);
    }

    private void Subscribe()
    {
        if (metaAgent != null)
        {
            metaAgent.OnDetectionResponseReceived.AddListener(OnDetections);
        }

        if (webcamAgent != null)
        {
            webcamAgent.OnDetectionResponseReceived.AddListener(OnDetections);
        }

        if (metaAgent == null && webcamAgent == null)
        {
            Debug.LogWarning("[ObjectDetectionDwellTracker] No detection source assigned.");
        }
    }

    private void Unsubscribe()
    {
        if (metaAgent != null)
        {
            metaAgent.OnDetectionResponseReceived.RemoveListener(OnDetections);
        }

        if (webcamAgent != null)
        {
            webcamAgent.OnDetectionResponseReceived.RemoveListener(OnDetections);
        }
    }

    private void OnDetections(List<BoxData> boxes)
    {
        if (boxes == null || boxes.Count == 0 || string.IsNullOrEmpty(_normalizedTargetLabel))
        {
            return;
        }

        var now = Time.time;
        var foundTarget = false;

        for (var i = 0; i < boxes.Count; i++)
        {
            var rawLabel = boxes[i].label;
            if (!TryParseLabelAndConfidence(rawLabel, out var labelName, out var confidence))
            {
                labelName = NormalizeLabel(rawLabel);
                confidence = 1f;
            }

            if (labelName != _normalizedTargetLabel || confidence < minConfidence)
            {
                continue;
            }

            foundTarget = true;
            break;
        }

        if (!foundTarget)
        {
            return;
        }

        if (now - _lastSeenTime > missingGraceSeconds)
        {
            _dwellStartTime = now;
        }

        _lastSeenTime = now;
        if (float.IsNegativeInfinity(_dwellStartTime))
        {
            _dwellStartTime = now;
        }
        //RefreshState(now);
    }

    private void RefreshState(float now)
    {
        Debug.Log($"Delta: {now - _lastSeenTime}");
        var seenRecently = now - _lastSeenTime <= missingGraceSeconds;

        if (!seenRecently)
        {
            _isTargetDetected = false;
            _currentDwellSeconds = 0f;
            _dwellStartTime = float.NegativeInfinity;
            SetRange(DwellRange.NotDetected);
            return;
        }

        _isTargetDetected = true;
        if (float.IsNegativeInfinity(_dwellStartTime))
        {
            _dwellStartTime = now;
        }

        _currentDwellSeconds = Mathf.Max(0f, now - _dwellStartTime);
        SetRange(EvaluateRange(_currentDwellSeconds));
    }

    private DwellRange EvaluateRange(float dwellSeconds)
    {
        if (dwellSeconds < shortThresholdSeconds)
        {
            return DwellRange.LessThanShortThreshold;
        }

        if (dwellSeconds < longThresholdSeconds)
        {
            return DwellRange.BetweenThresholds;
        }

        return DwellRange.GreaterThanOrEqualLongThreshold;
    }

    private void SetRange(DwellRange nextRange)
    {
        if (_currentRange == nextRange)
        {
            return;
        }

        _currentRange = nextRange;
        onDwellRangeChanged.Invoke(_currentRange, _currentDwellSeconds);
        //StateManager.Instance.SetGaze(_currentDwellSeconds);

        if (!logRangeChanges)
        {
            return;
        }

        var targetText = string.IsNullOrEmpty(_normalizedTargetLabel) ? "<unset>" : _normalizedTargetLabel;
        Debug.Log(
            $"[ObjectDetectionDwellTracker] Target={targetText} Range={_currentRange} Dwell={_currentDwellSeconds:0.00}s");
    }

    private void NormalizeTargetLabel()
    {
        _normalizedTargetLabel = NormalizeLabel(targetLabel);
    }

    private static string NormalizeLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        return label.Trim().ToLowerInvariant();
    }

    private static bool TryParseLabelAndConfidence(string rawLabel, out string labelName, out float confidence)
    {
        labelName = string.Empty;
        confidence = 0f;

        if (string.IsNullOrWhiteSpace(rawLabel))
        {
            return false;
        }

        var cut = rawLabel.LastIndexOf(' ');
        if (cut <= 0 || cut >= rawLabel.Length - 1)
        {
            return false;
        }

        labelName = NormalizeLabel(rawLabel[..cut]);
        return float.TryParse(rawLabel[(cut + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out confidence);
    }
}
