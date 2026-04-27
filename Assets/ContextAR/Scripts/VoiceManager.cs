using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine;
using TMPro; // For displaying text on the screen
using Oculus.Interaction.Input; // For controller interaction
using Meta.WitAi; // Meta Voice SDK
using Meta.WitAi.Configuration;
using Meta.WitAi.Interfaces;

using Oculus.Voice;

public class VoiceManager : MonoBehaviour
{

    [Header("Controller Configuration")]
    [SerializeField] private OVRInput.Controller controllerHand = OVRInput.Controller.RTouch; // Set to RTouch or LTouch
    [SerializeField] private OVRInput.Button triggerButton = OVRInput.Button.One, askButton = OVRInput.Button.Two;

   

    public string question;

    //public TTSManager ttsManager;
    public AppVoiceExperience wit;
    private bool isListening;

    private void Start()
    {
        // Initialize the Wit instance
        //wit = GetComponent<AppVoiceExperience>();
        wit.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        wit.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        wit.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
        wit.VoiceEvents.OnError.AddListener(OnError);

        isListening = false;
    }

    private void Update()
    {
        // Check for button press on the controller
        if (OVRInput.GetDown(triggerButton, controllerHand))
        {
            Debug.Log("Started Listening");
            ToggleListening();
            
        }
        
    }


    public void AskQuestion()
    {
        ToggleListening();
    }


    private void ToggleListening()
    {

        if (isListening)
        {
            //StopListening();
            return;
        }
        else
        {
            Debug.Log("Toggle");
            StartListening();
        }
    }

    private void StartListening()
    {
        if (wit != null && !isListening)
        {
            wit.Activate();
            isListening = true;
            

        }
    }

    private void StopListening()
    {
        if (wit != null && isListening)
        {
            wit.Deactivate();
            isListening = false;
        }
    }



    private void OnPartialTranscription(string transcription)
    {
        
        Debug.Log("Partial: " + transcription);
    }

    private void OnFullTranscription(string transcription)
    {
        question = transcription;
        Debug.Log("QUestion to Ask: " + question);
        //ttsManager.Speak("You said, " + question);
    }

    private void OnStoppedListening()
    {
        StopListening();

        Debug.Log("Stopped listneing");
    }

    private void OnError(string error, string message)
    {
        Debug.Log($"Error: {error} - {message}");
       
    }

    private void OnDestroy()
    {
        // Clean up event listeners
        if (wit != null)
        {
            wit.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            wit.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
            wit.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            wit.VoiceEvents.OnError.RemoveListener(OnError);
        }
    }
}