using UnityEngine;

public class StateManager : MonoBehaviour
{
    public static StateManager Instance;

    public AskState CurrentState = new AskState();

    void Awake()
    {
        Instance = this;
        CurrentState.crowd = "low";
        CurrentState.noise = "quiet";
        CurrentState.gaze_duration = 10f;
    }

    public void SetCrowd(string level)
    {
        CurrentState.crowd = level;
        
    }

    public void SetNoise(string level)
    {
        CurrentState.noise = level;
    }

    public void SetGaze(float seconds)
    {
        CurrentState.gaze_duration = 10f;
    }

    public void SetPainting(string paintingName)
    {
        // This can be used to track which painting the visitor is looking at,
        // if exhibit-specific behavior is desired.
    }
}
