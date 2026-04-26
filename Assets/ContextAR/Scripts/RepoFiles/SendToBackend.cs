using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SendToBackend : MonoBehaviour
{
    [Header("Backend Settings")]
    [SerializeField] private string serverUrl = "http://10.155.96.145:8000/ask";

    [Header("References")]
    [SerializeField] private CameraCapture cameraCapture;
    [SerializeField] private InputLayerController inputLayerController;

    public Action<string> OnTextResponse;
    public Action<string> OnAudioURL;

    public string ServerUrl => serverUrl;
    public string ServerBaseUrl => ExtractServerBaseUrl(serverUrl);

    private void Awake()
    {
        if (cameraCapture == null)
        {
            cameraCapture = FindAnyObjectByType<CameraCapture>();
        }

        if (inputLayerController == null)
        {
            inputLayerController = FindAnyObjectByType<InputLayerController>();
        }
    }

    public void AskQuestion(string question)
    {
        if (cameraCapture == null)
        {
            Debug.LogError("SendToBackend: CameraCapture reference is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            Debug.LogError("SendToBackend: Question is empty.");
            return;
        }

        StartCoroutine(SendQARequest(question));
    }

    public string ResolveBackendUrl(string audioUrl)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(audioUrl, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        string baseUrl = ServerBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return audioUrl;
        }

        if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }

        string relative = audioUrl.StartsWith("/") ? audioUrl.Substring(1) : audioUrl;
        return baseUrl + relative;
    }

    private IEnumerator SendQARequest(string question)
    {
        string base64Image = null;
        yield return cameraCapture.CaptureFrame(img => { base64Image = img; });

        if (string.IsNullOrEmpty(base64Image))
        {
            Debug.LogError("SendToBackend: No image captured.");
            yield break;
        }

        var payload = new BackendRequest
        {
            question = question,
            image_base64 = base64Image,
            state = GetRequestState()
        };
        string json = JsonUtility.ToJson(payload);

        var req = new UnityWebRequest(serverUrl, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("SendToBackend: Sending request to " + serverUrl);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("SendToBackend: Error -> " + req.error);
            req.Dispose();
            yield break;
        }

        string responseText = req.downloadHandler.text;
        Debug.Log("SendToBackend: Response -> " + responseText);

        if (string.IsNullOrWhiteSpace(responseText))
        {
            Debug.LogError("SendToBackend: Empty response body.");
            req.Dispose();
            yield break;
        }

        BackendResponse response;
        try
        {
            response = JsonUtility.FromJson<BackendResponse>(responseText);
        }
        catch (Exception ex)
        {
            Debug.LogError("SendToBackend: Failed to parse JSON response. " + ex.Message);
            req.Dispose();
            yield break;
        }

        if (response == null)
        {
            Debug.LogError("SendToBackend: Parsed response is null.");
            req.Dispose();
            yield break;
        }

        OnTextResponse?.Invoke(response.answer);
        OnAudioURL?.Invoke(response.audio_url);
        req.Dispose();
    }

    private AskState GetRequestState()
    {
        if (inputLayerController != null)
        {
            //return inputLayerController.GetLatestAskState();
        }

        return new AskState
        {
            crowd = "unknown",
            noise = "unknown",
            //detected = false,
            //both_holding = false
        };
    }

    private static string ExtractServerBaseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.GetLeftPart(UriPartial.Authority);
        }

        string trimmed = url.TrimEnd('/');
        int slash = trimmed.LastIndexOf('/');
        return slash > 0 ? trimmed.Substring(0, slash) : trimmed;
    }

    [Serializable]
    private class BackendRequest
    {
        public string question;
        public string image_base64;
        public AskState state;
    }

    [Serializable]
    private class BackendResponse
    {
        public string mode;
        public string answer;
        public string audio_url;
    }
}
