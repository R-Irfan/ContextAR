using UnityEngine;

public class GazeCursor : MonoBehaviour
{
    public EyeGaze EyeGaze;

    void Update()
    {
        transform.position = EyeGaze.GetGazeOrigin() + EyeGaze.GetGazeDirection() * 2f;
    }
}
