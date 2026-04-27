using UnityEngine;
using Meta.WitAi.TTS;
using Meta.WitAi.TTS.Utilities;
public class TTSManager : MonoBehaviour
{
     // Assign in inspector
    public TTSSpeaker ttsSpeaker;       // Attach to same GameObject or assign

    void Awake()
    {
        

        if (ttsSpeaker == null)
            ttsSpeaker = GetComponent<TTSSpeaker>();
    }


    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("TTS: Empty text");
            return;
        }

        if (ttsSpeaker == null)
        {
            Debug.LogError("TTS Speaker not assigned!");
            return;
        }

        // Stop any ongoing speech (optional for barge-in handling)
        //ttsSpeaker.Stop();

        // Speak new text
        ttsSpeaker.Speak(text);
    }
}
