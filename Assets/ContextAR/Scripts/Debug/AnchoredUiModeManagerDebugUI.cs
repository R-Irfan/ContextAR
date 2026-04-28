using UnityEngine;
using TMPro;

public class AnchoredUiModeManagerDebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AnchoredUiModeManager manager;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Update")]
    [SerializeField] private float updateInterval = 0.1f;

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < updateInterval)
            return;

        _timer = 0f;

        if (debugText == null || manager == null)
            return;

        var state = StateManager.Instance != null ? StateManager.Instance.CurrentState : null;

        var crowd = state != null ? state.crowd : "null";
        var noise = state != null ? state.noise : "null";
        var gaze = state != null ? state.gaze_duration.ToString("0.00") : "null";

        var mappedMode = state != null
            ? manager.MapToUiMode(state.crowd, state.noise, state.gaze_duration)
            : AnchoredUiModeManager.UiMode.None;

        debugText.text =
            $"=== UI MODE DEBUG ===\n" +
            $"Current Mode: {manager.CurrentMode}\n" +
            $"Mapped Mode: {mappedMode}\n\n" +

            $"STATE INPUT\n" +
            $"Crowd: {crowd}\n" +
            $"Noise: {noise}\n" +
            $"Gaze: {gaze}\n\n" +

            $"ACTIVE ROOTS\n" +
            $"Minimal: {IsActive(manager, "Minimal")}\n" +
            $"Short: {IsActive(manager, "Short")}\n" +
            $"Full: {IsActive(manager, "Full")}\n\n" +

            $"FRAME: {Time.frameCount}";
    }

    private string IsActive(AnchoredUiModeManager mgr, string mode)
    {
        var field = typeof(AnchoredUiModeManager)
            .GetField(mode.ToLower() + "Ui", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field == null)
            return "unknown";

        var ui = field.GetValue(mgr);
        if (ui == null)
            return "null";

        var rootField = ui.GetType().GetField("root");
        if (rootField == null)
            return "no-root";

        var root = rootField.GetValue(ui) as GameObject;
        if (root == null)
            return "null";

        return root.activeInHierarchy ? "ACTIVE" : "inactive";
    }
}