// InputLayerController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class InputLayerController : MonoBehaviour
{
    private const string SERVER = "http://10.x.x.x:8000";


    void Start()
    {
        StartCoroutine(PollState());
        StartCoroutine(TestConnection());
    }

    IEnumerator TestConnection()
    {
        Debug.Log("Testing connection to: " + SERVER);
        string url = $"{SERVER}/state";
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("<color=green>CONNECTED ✔</color>");
            Debug.Log("Server Response: " + req.downloadHandler.text);
        }
        else
        {
            Debug.LogError("<color=red>FAILED TO CONNECT ✘</color>");
            Debug.LogError("Error: " + req.error);
        }
    }


    IEnumerator PollState()
    {
        while (true)
        {
            using var req = UnityWebRequest.Get($"{SERVER}/state");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var state = JsonUtility.FromJson<StateResponse>(req.downloadHandler.text);
                UpdateInputMode(state);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    void UpdateInputMode(StateResponse state)
    {
        if (state.hands.both_holding)
        {
            // Switch to eye / head gaze interaction
        }
        else if (state.noise.level == "noisy")
        {
            // Disable voice, use gaze only
        }
        else
        {
            // Full voice input
        }
    }
}