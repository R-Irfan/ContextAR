using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DetectedObjectUIAnchor))]
public class DetectedObjectUIAnchorEditor : Editor
{
    private SerializedProperty _detectionAgentProp;
    private SerializedProperty _detectionVisualizerProp;
    private SerializedProperty _classLabelsFileProp;
    private SerializedProperty _targetLabelsProp;
    private SerializedProperty _confidenceThresholdProp;
    private SerializedProperty _useAllWhenEmptyProp;
    private SerializedProperty _targetCanvasProp;
    private SerializedProperty _scaleCanvasToBoxProp;
    private SerializedProperty _canvasScaleMultiplierProp;
    private SerializedProperty _hideCanvasWhenNoMatchProp;
    private SerializedProperty _hideTrackedObjectsWhenNoMatchProp;
    private SerializedProperty _uiElementsProp;
    private SerializedProperty _enableSmoothingProp;
    private SerializedProperty _positionSmoothTimeProp;
    private SerializedProperty _rotationSmoothSpeedProp;
    private SerializedProperty _boxScaleSmoothTimeProp;
    private SerializedProperty _lostTargetHoldTimeProp;

    private string _labelFilter = string.Empty;

    private void OnEnable()
    {
        _detectionAgentProp = serializedObject.FindProperty("detectionAgent");
        _detectionVisualizerProp = serializedObject.FindProperty("detectionVisualizer");
        _classLabelsFileProp = serializedObject.FindProperty("classLabelsFile");
        _targetLabelsProp = serializedObject.FindProperty("targetLabels");
        _confidenceThresholdProp = serializedObject.FindProperty("confidenceThreshold");
        _useAllWhenEmptyProp = serializedObject.FindProperty("useAllLabelsWhenSelectionEmpty");
        _targetCanvasProp = serializedObject.FindProperty("targetCanvas");
        _scaleCanvasToBoxProp = serializedObject.FindProperty("scaleCanvasToBox");
        _canvasScaleMultiplierProp = serializedObject.FindProperty("canvasScaleMultiplier");
        _hideCanvasWhenNoMatchProp = serializedObject.FindProperty("hideCanvasWhenNoMatch");
        _hideTrackedObjectsWhenNoMatchProp = serializedObject.FindProperty("hideTrackedObjectsWhenNoMatch");
        _uiElementsProp = serializedObject.FindProperty("uiElements");
        _enableSmoothingProp = serializedObject.FindProperty("enableSmoothing");
        _positionSmoothTimeProp = serializedObject.FindProperty("positionSmoothTime");
        _rotationSmoothSpeedProp = serializedObject.FindProperty("rotationSmoothSpeed");
        _boxScaleSmoothTimeProp = serializedObject.FindProperty("boxScaleSmoothTime");
        _lostTargetHoldTimeProp = serializedObject.FindProperty("lostTargetHoldTime");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Detection", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_detectionAgentProp);
        EditorGUILayout.PropertyField(_detectionVisualizerProp);
        EditorGUILayout.PropertyField(_classLabelsFileProp);
        EditorGUILayout.PropertyField(_confidenceThresholdProp);
        EditorGUILayout.PropertyField(_useAllWhenEmptyProp);

        EditorGUILayout.Space(8f);
        DrawLabelSelector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("UI Placement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_targetCanvasProp);
        EditorGUILayout.PropertyField(_scaleCanvasToBoxProp);
        if (_scaleCanvasToBoxProp.boolValue)
        {
            EditorGUILayout.PropertyField(_canvasScaleMultiplierProp);
        }
        EditorGUILayout.PropertyField(_hideCanvasWhenNoMatchProp);
        EditorGUILayout.PropertyField(_hideTrackedObjectsWhenNoMatchProp);
        EditorGUILayout.PropertyField(_uiElementsProp, includeChildren: true);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Smoothing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_enableSmoothingProp);
        if (_enableSmoothingProp.boolValue)
        {
            EditorGUILayout.PropertyField(_positionSmoothTimeProp);
            EditorGUILayout.PropertyField(_rotationSmoothSpeedProp);
            EditorGUILayout.PropertyField(_boxScaleSmoothTimeProp);
        }
        EditorGUILayout.PropertyField(_lostTargetHoldTimeProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawLabelSelector()
    {
        var anchor = (DetectedObjectUIAnchor)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Target Labels", EditorStyles.boldLabel);
            if (GUILayout.Button("Reload", GUILayout.Width(70f)))
            {
                Undo.RecordObject(anchor, "Reload Class Labels");
                anchor.RefreshAvailableLabelsFromFile();
                EditorUtility.SetDirty(anchor);
            }
        }

        var availableLabels = anchor.AvailableLabels;
        if (availableLabels == null || availableLabels.Count == 0)
        {
            EditorGUILayout.HelpBox("Assign class_labels.txt and click Reload to select labels from the file.", MessageType.Info);
            EditorGUILayout.PropertyField(_targetLabelsProp, includeChildren: true);
            return;
        }

        _labelFilter = EditorGUILayout.TextField("Filter", _labelFilter);

        var selected = ReadSelectedLabels(_targetLabelsProp);
        var changed = false;
        var filter = _labelFilter == null ? string.Empty : _labelFilter.Trim().ToLowerInvariant();

        foreach (var availableLabel in availableLabels)
        {
            if (!string.IsNullOrEmpty(filter) && !availableLabel.Contains(filter))
            {
                continue;
            }

            var isSelected = selected.Contains(availableLabel);
            var shouldSelect = EditorGUILayout.ToggleLeft(availableLabel, isSelected);
            if (shouldSelect == isSelected)
            {
                continue;
            }

            changed = true;
            if (shouldSelect)
            {
                selected.Add(availableLabel);
            }
            else
            {
                selected.Remove(availableLabel);
            }
        }

        if (!changed)
        {
            return;
        }

        WriteSelectedLabels(_targetLabelsProp, selected, availableLabels);
    }

    private static HashSet<string> ReadSelectedLabels(SerializedProperty targetLabelsProperty)
    {
        var selected = new HashSet<string>();
        for (var i = 0; i < targetLabelsProperty.arraySize; i++)
        {
            var value = targetLabelsProperty.GetArrayElementAtIndex(i).stringValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            selected.Add(value.Trim().ToLowerInvariant());
        }

        return selected;
    }

    private static void WriteSelectedLabels(SerializedProperty targetLabelsProperty, HashSet<string> selected, IReadOnlyList<string> availableLabels)
    {
        targetLabelsProperty.ClearArray();

        var index = 0;
        foreach (var availableLabel in availableLabels)
        {
            if (!selected.Contains(availableLabel))
            {
                continue;
            }

            targetLabelsProperty.InsertArrayElementAtIndex(index);
            targetLabelsProperty.GetArrayElementAtIndex(index).stringValue = availableLabel;
            index++;
        }
    }
}
