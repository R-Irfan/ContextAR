using Oculus.Voice;
using UnityEngine;

public class VoiceAIInteract : MonoBehaviour
{
    public TTSManager ttsManager;
    public VoiceManager voiceManager;
    public AppVoiceExperience wit;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        wit.VoiceEvents.OnFullTranscription.AddListener(HandleSTTTranscript);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartConversation() 
    {
        ttsManager.Speak("What would you like to know?");
        //
    }

    public void HandleQuestion()
    {
        voiceManager.AskQuestion();
    }

    public void HandleSTTTranscript(string reply)
    {
        ttsManager.Speak(reply);
    }

    private void OnDestroy()
    {
        if (wit != null)
        {
            wit.VoiceEvents.OnFullTranscription.RemoveListener(HandleSTTTranscript);
        }
    }
}
