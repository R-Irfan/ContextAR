using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TTSPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SendToBackend sendToBackend;

    [Header("URL Fallback")]
    [SerializeField] private string fallbackServerBaseUrl = "http://192.168.1.42:8000";
    [SerializeField] private bool autoFindSendToBackend = true;

    private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (autoFindSendToBackend && sendToBackend == null)
        {
            sendToBackend = FindAnyObjectByType<SendToBackend>();
        }
    }

    private void OnEnable()
    {
        SubscribeToBackend();
    }

    private void OnDisable()
    {
        UnsubscribeFromBackend();
    }

    public void PlayFromURL(string audioUrl)
    {
        StartCoroutine(PlayAudioRoutine(audioUrl));
    }

    private void SubscribeToBackend()
    {
        if (sendToBackend != null)
        {
            sendToBackend.OnAudioURL -= HandleAudioUrl;
            sendToBackend.OnAudioURL += HandleAudioUrl;
        }
    }

    private void UnsubscribeFromBackend()
    {
        if (sendToBackend != null)
        {
            sendToBackend.OnAudioURL -= HandleAudioUrl;
        }
    }

    private void HandleAudioUrl(string audioUrl)
    {
        PlayFromURL(audioUrl);
    }

    private IEnumerator PlayAudioRoutine(string audioUrl)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            Debug.LogWarning("TTSPlayer: Empty audio URL.");
            yield break;
        }

        string fullUrl = ResolveAudioUrl(audioUrl);
        if (string.IsNullOrWhiteSpace(fullUrl))
        {
            Debug.LogError("TTSPlayer: Could not resolve final audio URL.");
            yield break;
        }

        if (_cache.TryGetValue(fullUrl, out var cachedClip))
        {
            audioSource.clip = cachedClip;
            audioSource.Play();
            yield break;
        }

        using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(fullUrl, AudioType.MPEG))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("TTSPlayer: Error downloading audio -> " + req.error);
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null)
            {
                Debug.LogError("TTSPlayer: Failed to decode audio clip.");
                yield break;
            }

            _cache[fullUrl] = clip;
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private string ResolveAudioUrl(string audioUrl)
    {
        if (Uri.TryCreate(audioUrl, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        string baseUrl = sendToBackend != null ? sendToBackend.ServerBaseUrl : fallbackServerBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return audioUrl;
        }

        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            return new Uri(baseUri, audioUrl).ToString();
        }

        if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }

        return baseUrl + (audioUrl.StartsWith("/") ? audioUrl.Substring(1) : audioUrl);
    }
}
