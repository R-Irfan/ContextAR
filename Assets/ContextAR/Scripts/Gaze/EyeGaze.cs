using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeGaze : MonoBehaviour
{
    public OVREyeGaze LeftEye, RightEye;

    private Vector3 _combinedGazeOrigin, _combinedGazeDir;

    private List<Quaternion> _headRotationBuffer;


    [Header("One Euro Filter")]

    public bool FilteringGaze = true;

    public float FilterFrequency = 90f;
    public float FilterMinCutOff = 0.05f;
    public float FilterBeta = 10f;
    public float FitlerDcutoff = 1f;

    private OneEuroFilter<Vector3> _gazeDirFilter;
    private OneEuroFilter<Vector3> _gazePosFilter;

    [Header("Gaze Correction")]
    public bool CorrectGaze;
    public int FrameOffset = 7;

    void Awake()
    {
        _gazeDirFilter = new OneEuroFilter<Vector3>(FilterFrequency);
        _gazePosFilter = new OneEuroFilter<Vector3>(FilterFrequency);

        _headRotationBuffer = new List<Quaternion>();
    }


    void Update()
    {
        _combinedGazeOrigin = Vector3.Lerp(LeftEye.transform.position, RightEye.transform.position, 0.5f);
        _combinedGazeDir = Quaternion.Slerp(LeftEye.transform.rotation, RightEye.transform.rotation, 0.5f).normalized * Vector3.forward;            

        if (FilteringGaze)
        {
            _gazeDirFilter.UpdateParams(FilterFrequency, FilterMinCutOff, FilterBeta, FitlerDcutoff);
            _gazePosFilter.UpdateParams(FilterFrequency, FilterMinCutOff, FilterBeta, FitlerDcutoff);

            _combinedGazeDir = _gazeDirFilter.Filter(_combinedGazeDir);
            _combinedGazeOrigin = _gazePosFilter.Filter(_combinedGazeOrigin);
        }

        if (CorrectGaze && _headRotationBuffer.Count == FrameOffset)
        {
            Quaternion headRotOffset = _headRotationBuffer[0] * Quaternion.Inverse(_headRotationBuffer[_headRotationBuffer.Count - 1]);
            _combinedGazeDir = headRotOffset * _combinedGazeDir;
        }

        UpdateHeadRotationBuffer();
    }

    void UpdateHeadRotationBuffer()
    {
        Quaternion currentHeadRotation = Camera.main.transform.rotation;
        _headRotationBuffer.Add(currentHeadRotation);
        if (_headRotationBuffer.Count > FrameOffset)
        {
            _headRotationBuffer.RemoveAt(0);
        }
    }

    public Ray GetGazeRay()
    {
        return new Ray(_combinedGazeOrigin, _combinedGazeDir);
    }

    public Vector3 GetGazeOrigin()
    {
        return _combinedGazeOrigin;
    }

    public Vector3 GetGazeDirection()
    {
        return _combinedGazeDir;
    }

}
