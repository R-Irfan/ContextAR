using UnityEngine;
using TMPro;

public class StateManagerDebugUI : MonoBehaviour
{
    [Header("Optional UI Text Reference")]
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Screen Position")]
    [SerializeField] private Vector2 topLeftOffset = new Vector2(15f, 15f);

    [Header("Style")]
    [SerializeField] private int fontSize = 28;
    [SerializeField] private Color textColor = Color.white;

    private string _lastRendered = "";

    private void Awake()
    {
        if (debugText != null)
        {
            debugText.fontSize = fontSize;
            debugText.color = textColor;
        }
    }

    private void Update()
    {
        if (StateManager.Instance == null)
        {
            SetText("StateManager not found");
            return;
        }

        var state = StateManager.Instance.CurrentState;

        string content =
            $"Crowd: {state.crowd}\n" +
            $"Noise: {state.noise}\n" +
            $"Gaze: {state.gaze_duration:F2}s";

        if (content != _lastRendered)
        {
            _lastRendered = content;
            SetText(content);
        }
    }

    private void SetText(string value)
    {
        if (debugText != null)
        {
            debugText.text = value;
        }
    }

    private void OnGUI()
    {
        if (debugText != null)
            return;

        GUI.color = textColor;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
        style.alignment = TextAnchor.UpperLeft;

        GUI.Label(
            new Rect(topLeftOffset.x, topLeftOffset.y, 500f, 200f),
            _lastRendered,
            style
        );
    }
}