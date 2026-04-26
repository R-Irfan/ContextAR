using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class AnchoredUiModeManager : MonoBehaviour
{
    public enum UiMode
    {
        None = 0,
        Minimal = 1,
        Short = 2,
        Full = 3
    }

    [Serializable]
    public sealed class ModeUi
    {
        [Tooltip("Root object for this mode. Usually the mode canvas root.")]
        public GameObject root;

        [Tooltip("Optional CanvasGroup for fade transitions.")]
        public CanvasGroup canvasGroup;

        [Tooltip("Optional text targets (e.g., Text, TMP_Text component via reflection).")]
        public List<Component> textTargets = new List<Component>();

        [Min(0f), Tooltip("Seconds before this mode auto-hides. Set to 0 to disable auto-hide.")]
        public float autoHideSeconds = 3f;
    }

    [Header("Mode UI")]
    [SerializeField] private ModeUi minimalUi = new ModeUi { autoHideSeconds = 3f };
    [SerializeField] private ModeUi shortUi = new ModeUi { autoHideSeconds = 8f };
    [SerializeField] private ModeUi fullUi = new ModeUi { autoHideSeconds = 15f };

    [Header("Transitions")]
    [SerializeField] private bool hideAllOnEnable = true;
    [SerializeField] private bool enableFadeTransitions = true;
    [SerializeField] private bool enableAutoHide = true;
    [SerializeField, Min(0f)] private float fadeDurationSeconds = 0.2f;

    [Header("Context Mapping")]
    [SerializeField] private bool autoSelectModeFromState = true;
    [SerializeField, Min(0f)] private float shortGazeThresholdSeconds = 5f;
    [SerializeField, Min(0f)] private float longGazeThresholdSeconds = 15f;

    [Header("Debug")]
    [SerializeField] private bool logTransitions = false;

    public UiMode CurrentMode => _currentMode;

    private const string CrowdLow = "low";
    private const string CrowdModerate = "moderate";
    private const string CrowdCrowded = "crowded";
    private const string NoiseQuiet = "quiet";
    private const string NoiseModerate = "moderate";
    private const string NoiseNoisy = "noisy";

    private UiMode _currentMode = UiMode.None;
    private Coroutine _transitionRoutine;
    private Coroutine _autoHideRoutine;
    private bool _missingTextPropertyWarningShown;
    private string _minimalMessage = string.Empty;
    private string _shortMessage = string.Empty;
    private string _fullMessage = string.Empty;

    private void Update()
    {
        if (!autoSelectModeFromState)
        {
            return;
        }

        RefreshModeFromState();
    }

    private void OnEnable()
    {
        if (!hideAllOnEnable)
        {
            return;
        }

        HideAllImmediate();
    }

    public void ShowMinimal(string message)
    {
        _minimalMessage = message ?? string.Empty;
        BeginModeTransition(UiMode.Minimal, _minimalMessage);
    }

    public void ShowShort(string message)
    {
        _shortMessage = message ?? string.Empty;
        BeginModeTransition(UiMode.Short, _shortMessage);
    }

    public void ShowFull(string message)
    {
        _fullMessage = message ?? string.Empty;
        BeginModeTransition(UiMode.Full, _fullMessage);
    }

    public void HideAll()
    {
        BeginModeTransition(UiMode.None, null);
    }

    // Sets text for minimal UI without forcing mode changes.
    public void SetMinimalText(string message)
    {
        _minimalMessage = message ?? string.Empty;
        ApplyMessage(minimalUi, _minimalMessage);
    }

    // Sets text for short UI without forcing mode changes.
    public void SetShortText(string message)
    {
        _shortMessage = message ?? string.Empty;
        ApplyMessage(shortUi, _shortMessage);
    }

    // Sets text for full UI without forcing mode changes.
    public void SetFullText(string message)
    {
        _fullMessage = message ?? string.Empty;
        ApplyMessage(fullUi, _fullMessage);
    }

    // Public mapping function requested for crowd/noise/gaze -> UI level.
    public UiMode MapToUiMode(string crowdLevel, string noiseLevel, float gazeDurationSeconds)
    {
        var crowd = NormalizeLevel(crowdLevel);
        var noise = NormalizeLevel(noiseLevel);

        if (gazeDurationSeconds < shortGazeThresholdSeconds)
        {
            return UiMode.Minimal;
        }

        if (gazeDurationSeconds <= longGazeThresholdSeconds)
        {
            if (noise == NoiseNoisy)
            {
                return UiMode.Minimal;
            }

            if (crowd == CrowdCrowded && noise == NoiseModerate)
            {
                return UiMode.Minimal;
            }

            return UiMode.Short;
        }

        if (crowd == CrowdCrowded && noise == NoiseNoisy)
        {
            return UiMode.Minimal;
        }

        if ((crowd == CrowdLow && (noise == NoiseQuiet || noise == NoiseModerate))
            || (crowd == CrowdModerate && noise == NoiseQuiet))
        {
            return UiMode.Full;
        }

        return UiMode.Short;
    }

    // Uses StateManager.CurrentState and current text buckets to select and show one mode.
    public void RefreshModeFromState()
    {
        if (StateManager.Instance == null || StateManager.Instance.CurrentState == null)
        {
            return;
        }

        var state = StateManager.Instance.CurrentState;
        var mappedMode = MapToUiMode(state.crowd, state.noise, state.gaze_duration);
        var message = GetMessageForMode(mappedMode);

        if (_currentMode != mappedMode)
        {
            BeginModeTransition(mappedMode, message);
            return;
        }

        // Keep visible mode text in sync even when mode does not change.
        switch (mappedMode)
        {
            case UiMode.Minimal:
                ApplyMessage(minimalUi, _minimalMessage);
                break;
            case UiMode.Short:
                ApplyMessage(shortUi, _shortMessage);
                break;
            case UiMode.Full:
                ApplyMessage(fullUi, _fullMessage);
                break;
        }
    }

    private void HideAllImmediate()
    {
        CancelRunningRoutines();

        HideMode(minimalUi);
        HideMode(shortUi);
        HideMode(fullUi);
        _currentMode = UiMode.None;
    }

    private void BeginModeTransition(UiMode targetMode, string message)
    {
        CancelRunningRoutines();
        _transitionRoutine = StartCoroutine(TransitionToMode(targetMode, message));
    }

    private void CancelRunningRoutines()
    {
        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }

        if (_autoHideRoutine != null)
        {
            StopCoroutine(_autoHideRoutine);
            _autoHideRoutine = null;
        }
    }

    private IEnumerator TransitionToMode(UiMode targetMode, string message)
    {
        var previousMode = _currentMode;
        var previousUi = GetModeUi(previousMode);
        var targetUi = GetModeUi(targetMode);

        if (previousUi != null && previousMode != targetMode)
        {
            yield return FadeOutAndHide(previousUi);
        }

        if (targetMode == UiMode.None || targetUi == null)
        {
            HideNonTargetModes(UiMode.None);
            _currentMode = UiMode.None;
            _transitionRoutine = null;
            yield break;
        }

        HideNonTargetModes(targetMode);
        ApplyMessage(targetUi, message ?? GetMessageForMode(targetMode));

        if (targetUi.root != null && !targetUi.root.activeSelf)
        {
            targetUi.root.SetActive(true);
        }

        if (targetUi.canvasGroup != null)
        {
            if (enableFadeTransitions && fadeDurationSeconds > 0f)
            {
                targetUi.canvasGroup.alpha = 0f;
                yield return FadeCanvasGroup(targetUi.canvasGroup, 0f, 1f, fadeDurationSeconds);
            }
            else
            {
                targetUi.canvasGroup.alpha = 1f;
            }
        }

        _currentMode = targetMode;
        _transitionRoutine = null;

        if (enableAutoHide && targetUi.autoHideSeconds > 0f)
        {
            _autoHideRoutine = StartCoroutine(AutoHideAfter(targetMode, targetUi.autoHideSeconds));
        }

        if (logTransitions)
        {
            Debug.Log($"[AnchoredUiModeManager] Active mode: {_currentMode}");
        }
    }

    private IEnumerator AutoHideAfter(UiMode mode, float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (_currentMode != mode)
        {
            _autoHideRoutine = null;
            yield break;
        }

        if (logTransitions)
        {
            Debug.Log($"[AnchoredUiModeManager] Auto-hide mode: {mode}");
        }

        _autoHideRoutine = null;
        BeginModeTransition(UiMode.None, null);
    }

    private void HideNonTargetModes(UiMode targetMode)
    {
        if (targetMode != UiMode.Minimal)
        {
            HideMode(minimalUi);
        }

        if (targetMode != UiMode.Short)
        {
            HideMode(shortUi);
        }

        if (targetMode != UiMode.Full)
        {
            HideMode(fullUi);
        }
    }

    private IEnumerator FadeOutAndHide(ModeUi modeUi)
    {
        if (modeUi == null || modeUi.root == null || !modeUi.root.activeSelf)
        {
            yield break;
        }

        if (modeUi.canvasGroup != null && enableFadeTransitions && fadeDurationSeconds > 0f)
        {
            yield return FadeCanvasGroup(modeUi.canvasGroup, modeUi.canvasGroup.alpha, 0f, fadeDurationSeconds);
        }

        HideMode(modeUi);
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private static void HideMode(ModeUi modeUi)
    {
        if (modeUi == null || modeUi.root == null)
        {
            return;
        }

        if (modeUi.canvasGroup != null)
        {
            modeUi.canvasGroup.alpha = 0f;
        }

        if (modeUi.root.activeSelf)
        {
            modeUi.root.SetActive(false);
        }
    }

    private void ApplyMessage(ModeUi modeUi, string message)
    {
        if (modeUi == null || modeUi.textTargets == null || modeUi.textTargets.Count == 0)
        {
            return;
        }

        var text = message ?? string.Empty;

        for (int i = 0; i < modeUi.textTargets.Count; i++)
        {
            var target = modeUi.textTargets[i];
            if (target == null)
            {
                continue;
            }

            if (target is Text legacyText)
            {
                legacyText.text = text;
                continue;
            }

            // Supports TMP_Text or any component that exposes a writable string "text" property.
            var textProperty = target.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            if (textProperty != null && textProperty.CanWrite && textProperty.PropertyType == typeof(string))
            {
                textProperty.SetValue(target, text);
                continue;
            }

            if (!_missingTextPropertyWarningShown)
            {
                _missingTextPropertyWarningShown = true;
                Debug.LogWarning("[AnchoredUiModeManager] A configured text target has no writable string text property.");
            }
        }
    }

    private string GetMessageForMode(UiMode mode)
    {
        switch (mode)
        {
            case UiMode.Minimal:
                return _minimalMessage;
            case UiMode.Short:
                return _shortMessage;
            case UiMode.Full:
                return _fullMessage;
            default:
                return string.Empty;
        }
    }

    private static string NormalizeLevel(string level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return string.Empty;
        }

        return level.Trim().ToLowerInvariant();
    }

    private ModeUi GetModeUi(UiMode mode)
    {
        switch (mode)
        {
            case UiMode.Minimal:
                return minimalUi;
            case UiMode.Short:
                return shortUi;
            case UiMode.Full:
                return fullUi;
            default:
                return null;
        }
    }
}
