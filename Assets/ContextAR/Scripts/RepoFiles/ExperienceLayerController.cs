// ExperienceLayerController.cs
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ExperienceLayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PassthroughScreenshotCapture capture;
    [SerializeField] private AnchoredUiModeManager uiManager;
    public TTSManager ttsManager;

    [Header("Backend")]
    [SerializeField] public string serverUrl = "http://172.29.51.145:8000";

    [Header("Debug")]
    [SerializeField] private bool logServerResponses = true;

    private bool _missingUiManagerWarningShown;

    private void Awake()
    {
        if (capture == null)
        {
            capture = FindAnyObjectByType<PassthroughScreenshotCapture>();
        }

        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<AnchoredUiModeManager>();
        }
    }

    // Call this when the visitor asks a question.
    public void OnVisitorQuestion(string question, float gazeDuration, string crowdLevel, string noiseLevel, bool sendImage = false)
    {
        StartCoroutine(AskServer(question, gazeDuration, crowdLevel, noiseLevel, sendImage));
    }

    private IEnumerator AskServer(string question, float gazeDuration, string crowd, string noise, bool sendImage)
    {
        string imageBase64 = null;
        if (capture != null && sendImage)
        {
            yield return capture.CaptureScreenshotBase64(base64 => imageBase64 = base64);
        }
        else if (sendImage)
        {
            Debug.LogWarning("[ExperienceLayerController] PassthroughScreenshotCapture reference is missing. Sending request without image.");
        }

        var body = new AskRequest
        {
            question = question,
            state = new AskState
            {
                crowd = crowd,
                noise = noise,
                gaze_duration = gazeDuration
            },
            image_base64 = imageBase64// Optional exhibit image for recognition
            
        };
        Debug.Log($"Serialized AskRequest JSON: {JsonUtility.ToJson(body)}");
        Debug.Log("crowd: " + body.state.crowd);
        Debug.Log("noise: " + body.state.noise);
        Debug.Log("gaze_duration: " + body.state.gaze_duration);
        string json = JsonUtility.ToJson(body);
        using var req = new UnityWebRequest($"{serverUrl}/ask", UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            if (logServerResponses)
            {
                Debug.Log($"[ExperienceLayerController] /ask raw response: {req.downloadHandler.text}");
            }

            var resp = JsonUtility.FromJson<AskResponse>(req.downloadHandler.text);
            HandleResponse(resp);
            yield break;
        }

        Debug.LogWarning($"[ExperienceLayerController] Request failed: {req.error}");
    }

    private void HandleResponse(AskResponse resp)
    {
        if (resp == null)
        {
            Debug.LogWarning("[ExperienceLayerController] Parsed response is null.");
            return;
        }

        if (logServerResponses)
        {
            Debug.Log($"[ExperienceLayerController] Parsed mode={resp.mode}, answer=\"{resp.answer}\", exhibit=\"{resp.exhibit}\"");
        }

        switch (resp.mode)
        {
            case "NO_RESPONSE":
                // Do nothing - visitor is passing by
                break;

            case "GLANCE_CARD":
                ShowGlanceCard(resp.answer);
                ttsManager.Speak(resp.answer);
                break;

            case "BRIEF_TEXT":
                ShowBriefText(resp.answer);
                ttsManager.Speak(resp.answer);
                break;

            case "FULL_VOICE":
                ShowFullOverlay(resp.answer);
                ttsManager.Speak(resp.answer);
                break;

            case "BRIEF_TEXT_PROMPT":
                ShowBriefText(resp.answer); // answer already includes quiet-spot nudge
                ttsManager.Speak(resp.answer);
                break;

            default:
                Debug.LogWarning($"[ExperienceLayerController] Unknown mode received: {resp.mode}");
                break;
        }

        if (!string.IsNullOrEmpty(resp.exhibit))
        {
            if (logServerResponses)
            {
                Debug.Log($"[ExperienceLayerController] Setting painting from exhibit: {resp.exhibit}");
            }

            if (StateManager.Instance != null)
            {
                StateManager.Instance.SetPainting(resp.exhibit);
            }
        }
    }

    private void ShowGlanceCard(string text)
    {
        if (uiManager == null)
        {
            WarnMissingUiManager();
            return;
        }

        uiManager.SetMinimalText(text);
        //uiManager.RefreshModeFromState();
    }

    private void ShowBriefText(string text)
    {
        if (uiManager == null)
        {
            WarnMissingUiManager();
            return;
        }

        uiManager.SetShortText(text);
        //uiManager.RefreshModeFromState();
    }

    private void ShowFullOverlay(string text)
    {
        if (uiManager == null)
        {
            WarnMissingUiManager();
            return;
        }

        uiManager.SetFullText(text);
        //uiManager.RefreshModeFromState();
    }

    private void WarnMissingUiManager()
    {
        if (_missingUiManagerWarningShown)
        {
            return;
        }

        _missingUiManagerWarningShown = true;
        Debug.LogWarning("[ExperienceLayerController] AnchoredUiModeManager reference is missing.");
    }
}
