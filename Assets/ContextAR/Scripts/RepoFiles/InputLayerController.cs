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
    public StateResponse LatestState => _latestState;

    private StateResponse _latestState;
    private bool _hasLatestState;

    private void Start()
    {
        StartCoroutine(PollState());
        if (runConnectionTestOnStart)
        {
            StartCoroutine(TestConnection());
        }
    }

    public AskState GetLatestAskState()
    {
        if (!_hasLatestState || _latestState == null)
        {
            return BuildFallbackAskState();
        }

        return new AskState
        {
            crowd = string.IsNullOrWhiteSpace(_latestState.crowd?.level) ? "unknown" : _latestState.crowd.level,
            noise = string.IsNullOrWhiteSpace(_latestState.noise?.level) ? "unknown" : _latestState.noise.level,
            detected = _latestState.hands != null && _latestState.hands.detected,
            both_holding = _latestState.hands != null && _latestState.hands.both_holding
        };
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

    private IEnumerator PollState()
    {
        while (true)
        {
            var req = UnityWebRequest.Get($"{serverBaseUrl}/state");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var state = JsonUtility.FromJson<StateResponse>(req.downloadHandler.text);
                if (state != null)
                {
                    _latestState = state;
                    _hasLatestState = true;
                    UpdateInputMode(state);
                }
            }
            else
            {
                Debug.LogWarning("InputLayerController: Poll /state failed -> " + req.error);
            }

            req.Dispose();
            yield return new WaitForSeconds(pollIntervalSeconds);
        }
    }

    private static AskState BuildFallbackAskState()
    {
        return new AskState
        {
            crowd = "unknown",
            noise = "unknown",
            detected = false,
            both_holding = false
        };
    }

    private static void UpdateInputMode(StateResponse state)
    {
        if (state == null)
        {
            return;
        }

        if (state.hands != null && state.hands.both_holding)
        {
            // Switch to eye / head gaze interaction
        }
        else if (state.noise != null && state.noise.level == "noisy")
        {
            // Disable voice, use gaze only
        }
        else
        {
            // Full voice input
        }
    }
}
