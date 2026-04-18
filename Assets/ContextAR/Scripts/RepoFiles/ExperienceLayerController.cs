// ExperienceLayerController.cs
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ExperienceLayerController : MonoBehaviour
{
    private const string SERVER = "http://192.168.1.42:8000";

    // Call this when the visitor asks a question
    public void OnVisitorQuestion(string question, string crowdLevel, string noiseLevel, bool bothHolding)
    {
        StartCoroutine(AskServer(question, crowdLevel, noiseLevel, bothHolding));
    }

    IEnumerator AskServer(string question, string crowd, string noise, bool bothHolding)
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
        using var req = new UnityWebRequest($"{SERVER}/ask", "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<AskResponse>(req.downloadHandler.text);
            HandleResponse(resp);
        }
    }

    void HandleResponse(AskResponse resp)
    {
        switch (resp.mode)
        {
            case "FULL_VOICE":
                ShowFullOverlay(resp.answer);
                if (!string.IsNullOrEmpty(resp.audio_url))
                    StartCoroutine(PlayAudio($"{SERVER}{resp.audio_url}"));
                break;

            case "BRIEF_TEXT":
                ShowBriefText(resp.answer);   // short text only, no audio
                break;

            case "XR_MENU":
                ShowXRMenu();                 // ignore answer and audio
                break;
        }
    }

    IEnumerator PlayAudio(string url)
    {
        using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var clip = DownloadHandlerAudioClip.GetContent(req);
            var source = GetComponent<AudioSource>();
            source.clip = clip;
            source.Play();
        }
    }

    void ShowFullOverlay(string text) { /* update your UI panel */ }
    void ShowBriefText(string text)   { /* update your UI panel */ }
    void ShowXRMenu()                 { /* activate your XR menu GameObject */ }
}