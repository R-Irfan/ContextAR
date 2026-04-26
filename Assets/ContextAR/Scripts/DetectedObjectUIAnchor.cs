using System;
using System.Collections.Generic;
using System.Globalization;
using Meta.XR.BuildingBlocks.AIBlocks;
using UnityEngine;
using UnityEngine.Serialization;

public class DetectedObjectUIAnchor : MonoBehaviour
{
    [Serializable]
    public sealed class UiElementPlacement
    {
        [Tooltip("UI element under the target canvas.")]
        public RectTransform target;

        [Tooltip("Local offset where X/Z are from canvas center and Y is from the box top edge (in canvas local axes).")]
        public Vector3 localOffsetFromCanvasCenter = Vector3.zero;

        [Header("Axis Locks (Local)")]
        [Tooltip("Lock current local X (relative to canvas center) and ignore X offset updates.")]
        public bool lockLocalX;

        [Tooltip("Lock current local Y (relative to canvas center) and ignore Y offset/top-edge updates.")]
        public bool lockLocalY;

        [Tooltip("Lock current local Z (relative to canvas center) and ignore Z offset updates.")]
        public bool lockLocalZ;

        [SerializeField, HideInInspector] public bool hasLockedLocalX;
        [SerializeField, HideInInspector] public bool hasLockedLocalY;
        [SerializeField, HideInInspector] public bool hasLockedLocalZ;
        [SerializeField, HideInInspector] public float lockedLocalXFromCenter;
        [SerializeField, HideInInspector] public float lockedLocalYFromCenter;
        [SerializeField, HideInInspector] public float lockedLocalZFromCenter;
    }

    [Header("Detection")]
    [SerializeField] private ObjectDetectionAgent detectionAgent;
    [SerializeField] private ObjectDetectionVisualizer detectionVisualizer;
    [SerializeField] private TextAsset classLabelsFile;
    [SerializeField] private List<string> targetLabels = new List<string> { "person" };
    [SerializeField, Range(0f, 1f)] private float confidenceThreshold = 0.7f;
    [SerializeField] private bool useAllLabelsWhenSelectionEmpty;

    [Header("UI Placement")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private bool scaleCanvasToBox = true;
    [SerializeField] private Vector3 canvasScaleMultiplier = Vector3.one;
    [SerializeField] private bool hideCanvasWhenNoMatch = true;
    [SerializeField] private bool hideTrackedObjectsWhenNoMatch;
    [SerializeField] private List<UiElementPlacement> uiElements = new List<UiElementPlacement>();

    [Header("Smoothing")]
    [SerializeField] private bool enableSmoothing = true;
    [SerializeField, Min(0f)] private float positionSmoothTime = 0.08f;
    [SerializeField, Min(0f)] private float rotationSmoothSpeed = 12f;
    [FormerlySerializedAs("boxHeightSmoothTime")]
    [SerializeField, Min(0f)] private float boxScaleSmoothTime = 0.08f;
    [SerializeField, Min(0f)] private float lostTargetHoldTime = 0.2f;

    [SerializeField, HideInInspector] private List<string> availableLabels = new List<string>();

    private readonly HashSet<string> _selectedLabelLookup = new HashSet<string>(StringComparer.Ordinal);
    private bool _isSubscribed;
    private bool _missingReferenceWarningShown;
    private bool _canvasModeWarningShown;
    private bool _hasActiveTarget;
    private bool _isPoseInitialized;
    private Vector3 _targetCanvasPosition;
    private Quaternion _targetCanvasRotation = Quaternion.identity;
    private Vector3 _targetBoxWorldScale = Vector3.one;
    private Vector3 _smoothedCanvasPosition;
    private Quaternion _smoothedCanvasRotation = Quaternion.identity;
    private Vector3 _smoothedBoxWorldScale = Vector3.one;
    private Vector3 _positionVelocity;
    private Vector3 _boxScaleVelocity;
    private float _lastTargetSeenTime = float.NegativeInfinity;
    private float _baseCanvasScaleZ = 1f;
    private bool _hasCapturedBaseCanvasScale;

    public IReadOnlyList<string> AvailableLabels => availableLabels;

    private void Reset()
    {
        detectionAgent = GetComponent<ObjectDetectionAgent>();
        detectionVisualizer = GetComponent<ObjectDetectionVisualizer>();
    }

    private void Awake()
    {
        RefreshAvailableLabelsFromFile();
        NormalizeTargetsAndRebuildLookup();
    }

    private void OnEnable()
    {
        TrySubscribe();
        if (hideCanvasWhenNoMatch)
        {
            SetCanvasVisible(false);
        }

        if (hideTrackedObjectsWhenNoMatch)
        {
            SetTrackedObjectsVisible(false);
        }
    }

    private void OnDisable()
    {
        TryUnsubscribe();
        _hasActiveTarget = false;
        _isPoseInitialized = false;
        _positionVelocity = Vector3.zero;
        _boxScaleVelocity = Vector3.zero;
        _lastTargetSeenTime = float.NegativeInfinity;
        _hasCapturedBaseCanvasScale = false;
    }

    private void OnValidate()
    {
        RefreshAvailableLabelsFromFile();
        NormalizeTargetsAndRebuildLookup();
    }

    public void RefreshAvailableLabelsFromFile()
    {
        availableLabels.Clear();

        if (classLabelsFile == null || string.IsNullOrWhiteSpace(classLabelsFile.text))
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lines = classLabelsFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var normalized = NormalizeLabel(line);
            if (string.IsNullOrEmpty(normalized))
            {
                continue;
            }

            if (seen.Add(normalized))
            {
                availableLabels.Add(normalized);
            }
        }
    }

    private void TrySubscribe()
    {
        if (_isSubscribed)
        {
            return;
        }

        if (detectionAgent == null)
        {
            detectionAgent = GetComponent<ObjectDetectionAgent>();
        }

        if (detectionVisualizer == null)
        {
            detectionVisualizer = GetComponent<ObjectDetectionVisualizer>();
        }

        if (detectionAgent == null || detectionVisualizer == null || targetCanvas == null)
        {
            if (!_missingReferenceWarningShown)
            {
                _missingReferenceWarningShown = true;
                Debug.LogWarning("[DetectedObjectUIAnchor] Missing references. Assign Detection Agent, Detection Visualizer, and Target Canvas.");
            }
            return;
        }

        if (targetCanvas.renderMode != RenderMode.WorldSpace && !_canvasModeWarningShown)
        {
            _canvasModeWarningShown = true;
            Debug.LogWarning("[DetectedObjectUIAnchor] Target Canvas is not World Space. World anchoring will only work correctly with a World Space canvas.");
        }

        if (!_hasCapturedBaseCanvasScale)
        {
            _baseCanvasScaleZ = Mathf.Abs(targetCanvas.transform.localScale.z);
            if (_baseCanvasScaleZ <= Mathf.Epsilon)
            {
                _baseCanvasScaleZ = 1f;
            }
            _hasCapturedBaseCanvasScale = true;
        }

        detectionAgent.OnDetectionResponseReceived.AddListener(OnDetections);
        _isSubscribed = true;
    }

    private void TryUnsubscribe()
    {
        if (!_isSubscribed || detectionAgent == null)
        {
            return;
        }

        detectionAgent.OnDetectionResponseReceived.RemoveListener(OnDetections);
        _isSubscribed = false;
    }

    private void Update()
    {
        if (!_hasActiveTarget || targetCanvas == null || !enableSmoothing)
        {
            return;
        }

        if (!_isPoseInitialized)
        {
            InitializeSmoothedStateFromTarget();
            ApplyAnchors(_smoothedCanvasPosition, _smoothedCanvasRotation, _smoothedBoxWorldScale);
            return;
        }

        var dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }

        var safePositionSmoothTime = Mathf.Max(0.0001f, positionSmoothTime);
        var safeBoxScaleSmoothTime = Mathf.Max(0.0001f, boxScaleSmoothTime);

        _smoothedCanvasPosition = Vector3.SmoothDamp(
            _smoothedCanvasPosition,
            _targetCanvasPosition,
            ref _positionVelocity,
            safePositionSmoothTime,
            Mathf.Infinity,
            dt);

        var rotationLerp = 1f - Mathf.Exp(-rotationSmoothSpeed * dt);
        _smoothedCanvasRotation = Quaternion.Slerp(_smoothedCanvasRotation, _targetCanvasRotation, rotationLerp);

        _smoothedBoxWorldScale = Vector3.SmoothDamp(
            _smoothedBoxWorldScale,
            _targetBoxWorldScale,
            ref _boxScaleVelocity,
            safeBoxScaleSmoothTime,
            Mathf.Infinity,
            dt);

        ApplyAnchors(_smoothedCanvasPosition, _smoothedCanvasRotation, _smoothedBoxWorldScale);
    }

    private void OnDetections(List<BoxData> boxes)
    {
        if (targetCanvas == null || detectionVisualizer == null)
        {
            return;
        }

        if (boxes == null || boxes.Count == 0)
        {
            HandleNoTargetDetected();
            return;
        }

        var found = TryGetBestMatch(boxes, out var boxCenter, out var boxRotation, out var boxScale);

        if (!found)
        {
            HandleNoTargetDetected();
            return;
        }

        _targetCanvasPosition = boxCenter;
        _targetCanvasRotation = boxRotation;
        _targetBoxWorldScale = new Vector3(
            Mathf.Max(0.0001f, boxScale.x),
            Mathf.Max(0.0001f, boxScale.y),
            Mathf.Max(0.0001f, boxScale.z));
        _hasActiveTarget = true;
        _lastTargetSeenTime = Time.time;

        if (!enableSmoothing)
        {
            _isPoseInitialized = false;
            ApplyAnchors(_targetCanvasPosition, _targetCanvasRotation, _targetBoxWorldScale);
        }
        else if (!_isPoseInitialized)
        {
            InitializeSmoothedStateFromTarget();
            ApplyAnchors(_smoothedCanvasPosition, _smoothedCanvasRotation, _smoothedBoxWorldScale);
        }

        if (hideCanvasWhenNoMatch)
        {
            SetCanvasVisible(true);
        }

        if (hideTrackedObjectsWhenNoMatch)
        {
            SetTrackedObjectsVisible(true);
        }
    }

    private bool TryGetBestMatch(List<BoxData> boxes, out Vector3 targetPosition, out Quaternion targetRotation, out Vector3 targetScale)
    {
        targetPosition = default;
        targetRotation = default;
        targetScale = default;

        var bestConfidence = float.MinValue;
        var found = false;

        foreach (var box in boxes)
        {
            if (!TryParseLabelAndConfidence(box.label, out var labelName, out var confidence))
            {
                continue;
            }

            if (confidence < confidenceThreshold || !ShouldTrackLabel(labelName))
            {
                continue;
            }

            var xmin = box.position.x;
            var ymin = box.position.y;
            var xmax = box.scale.x;
            var ymax = box.scale.y;

            if (!detectionVisualizer.TryProject(xmin, ymin, xmax, ymax, out var worldCenter, out var worldRotation, out var worldScale))
            {
                continue;
            }

            if (confidence <= bestConfidence)
            {
                continue;
            }

            bestConfidence = confidence;
            targetPosition = worldCenter;
            targetRotation = worldRotation;
            targetScale = worldScale;
            found = true;
        }

        return found;
    }

    private void HandleNoTargetDetected()
    {
        if (HasRecentlySeenTarget())
        {
            return;
        }

        if (hideCanvasWhenNoMatch)
        {
            SetCanvasVisible(false);
        }

        if (hideTrackedObjectsWhenNoMatch)
        {
            SetTrackedObjectsVisible(false);
        }

        _hasActiveTarget = false;
    }

    private bool HasRecentlySeenTarget()
    {
        if (!_hasActiveTarget)
        {
            return false;
        }

        if (lostTargetHoldTime <= 0f)
        {
            return false;
        }

        return Time.time - _lastTargetSeenTime <= lostTargetHoldTime;
    }

    private void InitializeSmoothedStateFromTarget()
    {
        _smoothedCanvasPosition = _targetCanvasPosition;
        _smoothedCanvasRotation = _targetCanvasRotation;
        _smoothedBoxWorldScale = _targetBoxWorldScale;
        _positionVelocity = Vector3.zero;
        _boxScaleVelocity = Vector3.zero;
        _isPoseInitialized = true;
    }

    private void ApplyAnchors(Vector3 canvasPosition, Quaternion canvasRotation, Vector3 boxWorldScale)
    {
        targetCanvas.transform.SetPositionAndRotation(canvasPosition, canvasRotation);
        if (scaleCanvasToBox)
        {
            ApplyCanvasScale(boxWorldScale);
        }

        UpdateTrackedObjects(boxWorldScale.y);
    }

    private void ApplyCanvasScale(Vector3 boxWorldScale)
    {
        if (targetCanvas == null)
        {
            return;
        }

        var rectTransform = targetCanvas.transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        var rectWidth = Mathf.Max(0.0001f, rectTransform.rect.width);
        var rectHeight = Mathf.Max(0.0001f, rectTransform.rect.height);

        var scaledX = Mathf.Max(0.0001f, (boxWorldScale.x / rectWidth) * Mathf.Max(0f, canvasScaleMultiplier.x));
        var scaledY = Mathf.Max(0.0001f, (boxWorldScale.y / rectHeight) * Mathf.Max(0f, canvasScaleMultiplier.y));
        var scaledZ = Mathf.Max(0.0001f, _baseCanvasScaleZ * Mathf.Max(0f, canvasScaleMultiplier.z));

        targetCanvas.transform.localScale = new Vector3(scaledX, scaledY, scaledZ);
    }

    private void UpdateTrackedObjects(float boxHeightWorld)
    {
        if (uiElements == null || uiElements.Count == 0)
        {
            return;
        }

        var canvasTransform = targetCanvas != null ? targetCanvas.transform : null;
        if (canvasTransform == null)
        {
            return;
        }

        var topEdgeYOffsetLocal = ConvertWorldDistanceToLocalAlongAxis(canvasTransform, Vector3.up, boxHeightWorld * 0.5f);

        for (var i = 0; i < uiElements.Count; i++)
        {
            var placement = uiElements[i];
            if (placement == null || placement.target == null)
            {
                continue;
            }

            if (placement.target.parent != canvasTransform)
            {
                placement.target.SetParent(canvasTransform, worldPositionStays: false);
            }

            var currentLocal = placement.target.localPosition;
            var localPosition = placement.localOffsetFromCanvasCenter;

            // Unlocked Y is measured beyond the visualizer box (top edge + custom offset).
            localPosition.y += topEdgeYOffsetLocal;

            localPosition.x = ResolveLocalAxisX(placement, currentLocal.x, localPosition.x);
            localPosition.y = ResolveLocalAxisY(placement, currentLocal.y, localPosition.y, topEdgeYOffsetLocal);
            localPosition.z = ResolveLocalAxisZ(placement, currentLocal.z, localPosition.z);

            placement.target.localPosition = localPosition;
        }
    }

    private static float ResolveLocalAxisX(UiElementPlacement placement, float currentLocalX, float computedLocalX)
    {
        if (!placement.lockLocalX)
        {
            placement.hasLockedLocalX = false;
            return computedLocalX;
        }

        if (!placement.hasLockedLocalX)
        {
            placement.lockedLocalXFromCenter = currentLocalX;
            placement.hasLockedLocalX = true;
        }

        return placement.lockedLocalXFromCenter;
    }

    private static float ResolveLocalAxisY(UiElementPlacement placement, float currentLocalY, float computedLocalY, float topEdgeYOffsetLocal)
    {
        if (!placement.lockLocalY)
        {
            placement.hasLockedLocalY = false;
            return computedLocalY;
        }

        if (!placement.hasLockedLocalY)
        {
            // Store lock in center-relative local space.
            placement.lockedLocalYFromCenter = currentLocalY - topEdgeYOffsetLocal;
            placement.hasLockedLocalY = true;
        }

        // Locked Y is relative to center, so we do not re-apply top-edge offset.
        return placement.lockedLocalYFromCenter;
    }

    private static float ResolveLocalAxisZ(UiElementPlacement placement, float currentLocalZ, float computedLocalZ)
    {
        if (!placement.lockLocalZ)
        {
            placement.hasLockedLocalZ = false;
            return computedLocalZ;
        }

        if (!placement.hasLockedLocalZ)
        {
            placement.lockedLocalZFromCenter = currentLocalZ;
            placement.hasLockedLocalZ = true;
        }

        return placement.lockedLocalZFromCenter;
    }

    private static float ConvertWorldDistanceToLocalAlongAxis(Transform reference, Vector3 localAxis, float worldDistance)
    {
        if (reference == null || worldDistance == 0f)
        {
            return worldDistance;
        }

        var worldAxisVector = reference.TransformVector(localAxis.normalized);
        var worldUnitsPerLocalUnit = worldAxisVector.magnitude;
        if (worldUnitsPerLocalUnit <= Mathf.Epsilon)
        {
            return worldDistance;
        }

        return worldDistance / worldUnitsPerLocalUnit;
    }

    private void SetTrackedObjectsVisible(bool visible)
    {
        if (uiElements == null || uiElements.Count == 0)
        {
            return;
        }

        for (var i = 0; i < uiElements.Count; i++)
        {
            var placement = uiElements[i];
            if (placement == null || placement.target == null)
            {
                continue;
            }

            if (placement.target.gameObject.activeSelf == visible)
            {
                continue;
            }

            placement.target.gameObject.SetActive(visible);
        }
    }

    private bool ShouldTrackLabel(string labelName)
    {
        if (_selectedLabelLookup.Count == 0)
        {
            return useAllLabelsWhenSelectionEmpty;
        }

        return _selectedLabelLookup.Contains(labelName);
    }

    private void NormalizeTargetsAndRebuildLookup()
    {
        _selectedLabelLookup.Clear();

        if (targetLabels == null)
        {
            targetLabels = new List<string>();
            return;
        }

        for (var i = 0; i < targetLabels.Count; i++)
        {
            targetLabels[i] = NormalizeLabel(targetLabels[i]);
        }

        targetLabels.RemoveAll(string.IsNullOrEmpty);

        var deduped = new List<string>(targetLabels.Count);
        foreach (var label in targetLabels)
        {
            if (!_selectedLabelLookup.Add(label))
            {
                continue;
            }

            deduped.Add(label);
        }

        targetLabels = deduped;
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

    private void SetCanvasVisible(bool visible)
    {
        if (targetCanvas == null)
        {
            return;
        }

        if (targetCanvas.gameObject.activeSelf == visible)
        {
            return;
        }

        targetCanvas.gameObject.SetActive(visible);
    }
}
