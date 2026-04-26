using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class InputLayerController : MonoBehaviour
{
    [Header("Backend State Endpoint")]
    [SerializeField] private string serverBaseUrl = "http://10.155.96.145:8000";
    [SerializeField, Min(0.1f)] private float pollIntervalSeconds = 0.5f;
    [SerializeField] private bool runConnectionTestOnStart = true;

    public bool HasLatestState => _hasLatestState;
    //public StateResponse LatestState => _latestState;

    //private StateResponse _latestState;
    private bool _hasLatestState;

    private void Start()
    {
        StartCoroutine(TestConnection());
        
    }

    private IEnumerator TestConnection()
    {
        Debug.Log("InputLayerController: Testing connection to " + serverBaseUrl);
        var req = UnityWebRequest.Get($"{serverBaseUrl}/state");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("<color=green>InputLayerController: CONNECTED</color>");
            Debug.Log("InputLayerController: Server response -> " + req.downloadHandler.text);
        }
        else
        {
            Debug.LogError("<color=red>InputLayerController: FAILED TO CONNECT</color>");
            Debug.LogError("InputLayerController: Error -> " + req.error);
        }

        req.Dispose();
    }



    private static void UpdateInputMode(AskState state)//StateResponse state
    {
        if (state == null)
        {
            return;
        }

        //if (state.hands != null && state.hands.both_holding)
        //{
            // Switch to eye / head gaze interaction
        //}
        //else if (state.noise != null && state.noise.level == "noisy")
        //{
        //    // Disable voice, use gaze only
        //}
        else
        {
            // Full voice input
        }
    }
}
