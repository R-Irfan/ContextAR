using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ExperienceLayerController : MonoBehaviour
{
    [Header("Legacy /ask Pipeline")]
    [SerializeField] private bool enableLegacyAskPipeline = false;
    [SerializeField] private string legacyServerBaseUrl = "http://192.168.1.42:8000";

    private bool _hasWarnedDisabledMode;

    // Legacy path kept for reference. /qa flow should be used via SendToBackend.
    public void OnVisitorQuestion(string question, string crowdLevel, string noiseLevel, bool bothHolding)
    {
        if (!enableLegacyAskPipeline)
        {
            if (!_hasWarnedDisabledMode)
            {
                _hasWarnedDisabledMode = true;
                Debug.LogWarning("ExperienceLayerController: Legacy /ask pipeline is disabled. Use SendToBackend (/ask) instead.");
            }

            return;
        }

        StartCoroutine(AskServer(question, crowdLevel, noiseLevel, bothHolding));
    }

    private IEnumerator AskServer(string question, string crowd, string noise, bool bothHolding)
    {
        var body = new AskRequest
        {
            question = question,
            state = new AskState
            {
                crowd = crowd,
                noise = noise,
                detected = true,
                both_holding = bothHolding
            }
        };

        string json = JsonUtility.ToJson(body);
        var req = new UnityWebRequest($"{legacyServerBaseUrl}/ask", UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<AskResponse>(req.downloadHandler.text);
            HandleResponse(resp);
        }
        else
        {
            Debug.LogError("ExperienceLayerController: /ask request failed -> " + req.error);
        }

        req.Dispose();
    }

    private void HandleResponse(AskResponse resp)
    {
        if (resp == null)
        {
            return;
        }

        switch (resp.mode)
        {
            case "FULL_VOICE":
                ShowFullOverlay(resp.answer);
                if (!string.IsNullOrEmpty(resp.audio_url))
                {
                    StartCoroutine(PlayAudio($"{legacyServerBaseUrl}{resp.audio_url}"));
                }
                break;

            case "BRIEF_TEXT":
                ShowBriefText(resp.answer);
                break;

            case "XR_MENU":
                ShowXRMenu();
                break;
        }
    }

    private IEnumerator PlayAudio(string url)
    {
        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var clip = DownloadHandlerAudioClip.GetContent(req);
                var source = GetComponent<AudioSource>();
                if (source == null)
                {
                    source = gameObject.AddComponent<AudioSource>();
                }

                source.clip = clip;
                source.Play();
            }
            else
            {
                Debug.LogError("ExperienceLayerController: Audio playback download failed -> " + req.error);
            }
        }
    }

    private void ShowFullOverlay(string text) { /* update your UI panel */ }
    private void ShowBriefText(string text) { /* update your UI panel */ }
    private void ShowXRMenu() { /* activate your XR menu GameObject */ }
}
