using System;
using UnityEngine;
using UnityEngine.Events;

public class AmbientNoiseMonitor : MonoBehaviour
{
    [Header("Person Detection")]
    [SerializeField] private DetectionReader detectionReader;
    [SerializeField, Range(0f, 1f)] private float personConfidenceThreshold = 0.7f;

    [Header("Microphone")]
    [Tooltip("Leave empty to use the system default microphone.")]
    [SerializeField] private string microphoneDevice = "";
    [SerializeField] private bool startOnEnable = true;
    [SerializeField, Min(1)] private int sampleRate = 16000;
    [SerializeField, Min(1)] private int clipLengthSeconds = 1;

    [Header("Analysis")]
    [SerializeField, Min(64)] private int sampleWindowSize = 1024;
    [SerializeField, Min(0.01f)] private float analysisIntervalSeconds = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = true;

    [Serializable] public class NoiseLevelUpdatedEvent : UnityEvent<float, float> { } // dB, RMS
    [Serializable] public class NoiseStateChangedEvent : UnityEvent<bool, float> { } // IsNoisy, dB
    [Serializable] public class CrowdStateChangedEvent : UnityEvent<bool, int, float> { } // IsCrowdConfirmed, PersonCount, dB

    [SerializeField] private NoiseLevelUpdatedEvent onNoiseLevelUpdated = new();
    [SerializeField] private NoiseStateChangedEvent onNoiseStateChanged = new();
    [SerializeField] private CrowdStateChangedEvent onCrowdStateChanged = new();

    public NoiseLevelUpdatedEvent OnNoiseLevelUpdated => onNoiseLevelUpdated;
    public NoiseStateChangedEvent OnNoiseStateChanged => onNoiseStateChanged;
    public CrowdStateChangedEvent OnCrowdStateChanged => onCrowdStateChanged;

    public bool IsMonitoring => _micClip != null;
    public bool IsNoisy { get; private set; }
    public bool IsCrowdConfirmed { get; private set; }
    public string CurrentNoiseLevel { get; private set; } = NoiseQuiet;
    public string CurrentCrowdLevel { get; private set; } = CrowdLow;
    public int CurrentPersonCount { get; private set; }
    public float CurrentRms { get; private set; }
    public float CurrentDb { get; private set; } = -80f;
    public string ActiveDeviceName => _activeDeviceName;

    private const float MinRms = 1e-7f;
    private const string CrowdLow = "low";
    private const string CrowdModerate = "moderate";
    private const string CrowdCrowded = "crowded";
    private const string NoiseQuiet = "quiet";
    private const string NoiseModerate = "moderate";
    private const string NoiseNoisy = "noisy";

    private AudioClip _micClip;
    private string _activeDeviceName;
    private float _timer;
    private float[] _samples;
    private bool _missingDetectionReaderWarningShown;

    private void Awake()
    {
        if (detectionReader == null)
        {
            detectionReader = FindAnyObjectByType<DetectionReader>();
        }
    }

    private void OnEnable()
    {
        if (startOnEnable)
        {
            StartMonitoring();
        }
    }

    private void OnDisable()
    {
        StopMonitoring();
    }

    private void Update()
    {
        if (!IsMonitoring)
        {
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < analysisIntervalSeconds)
        {
            return;
        }
        _timer = 0f;

        AnalyzeNoiseLevel();
    }

    public bool StartMonitoring()
    {
        if (IsMonitoring)
        {
            return true;
        }

        var device = ResolveDeviceName();
        if (string.IsNullOrEmpty(device))
        {
            Debug.LogWarning("[AmbientNoiseMonitor] No microphone device found.");
            return false;
        }

        _micClip = Microphone.Start(device, true, clipLengthSeconds, sampleRate);
        if (_micClip == null)
        {
            Debug.LogError($"[AmbientNoiseMonitor] Failed to start microphone: {device}");
            return false;
        }

        _activeDeviceName = device;
        _samples = new float[Mathf.Max(64, sampleWindowSize)];
        _timer = 0f;
        return true;
    }

    public void StopMonitoring()
    {
        if (!IsMonitoring)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_activeDeviceName))
        {
            Microphone.End(_activeDeviceName);
        }

        _micClip = null;
        _activeDeviceName = null;
        _samples = null;
        IsNoisy = false;
        CurrentNoiseLevel = NoiseQuiet;
        IsCrowdConfirmed = false;
        CurrentCrowdLevel = CrowdLow;
        CurrentPersonCount = 0;
        CurrentRms = 0f;
        CurrentDb = -80f;

        if (StateManager.Instance != null)
        {
            StateManager.Instance.SetNoise(CurrentNoiseLevel);
            StateManager.Instance.SetCrowd(CurrentCrowdLevel);
        }
    }

    public bool HasMicrophoneDevices()
    {
        return Microphone.devices != null && Microphone.devices.Length > 0;
    }

    private string ResolveDeviceName()
    {
        var devices = Microphone.devices;
        if (devices == null || devices.Length == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(microphoneDevice))
        {
            foreach (var device in devices)
            {
                if (string.Equals(device, microphoneDevice, StringComparison.OrdinalIgnoreCase))
                {
                    return device;
                }
            }
        }

        return devices[0];
    }

    private void AnalyzeNoiseLevel()
    {
        if (_micClip == null || _samples == null || _samples.Length == 0)
        {
            return;
        }

        var position = Microphone.GetPosition(_activeDeviceName);
        if (position <= 0)
        {
            return;
        }

        int window = Mathf.Min(_samples.Length, _micClip.samples);
        if (window <= 0)
        {
            return;
        }

        int start = position - window;
        if (start < 0)
        {
            int tailCount = -start;
            int headCount = window - tailCount;
            var tail = new float[tailCount];
            var head = new float[headCount];

            _micClip.GetData(tail, _micClip.samples - tailCount);
            _micClip.GetData(head, 0);

            Array.Copy(tail, 0, _samples, 0, tailCount);
            Array.Copy(head, 0, _samples, tailCount, headCount);
        }
        else
        {
            _micClip.GetData(_samples, start);
        }

        double sumSquares = 0.0;
        for (int i = 0; i < window; i++)
        {
            float s = _samples[i];
            sumSquares += s * s;
        }

        CurrentRms = Mathf.Sqrt((float)(sumSquares / window));
        CurrentDb = 20f * Mathf.Log10(Mathf.Max(CurrentRms, MinRms));

        var nextNoiseLevel = EvaluateNoiseLevel(CurrentDb);
        bool noisyNow = nextNoiseLevel == NoiseNoisy;
        var levelChanged = !string.Equals(CurrentNoiseLevel, nextNoiseLevel, StringComparison.Ordinal);
        var noisyStateChanged = noisyNow != IsNoisy;
        if (levelChanged)
        {
            CurrentNoiseLevel = nextNoiseLevel;
            if (StateManager.Instance != null)
            {
                StateManager.Instance.SetNoise(CurrentNoiseLevel);
            }
        }

        if (noisyStateChanged)
        {
            IsNoisy = noisyNow;
            onNoiseStateChanged.Invoke(IsNoisy, CurrentDb);
        }

        if (logStateChanges && (levelChanged || noisyStateChanged))
        {
            Debug.Log($"[AmbientNoiseMonitor] NoiseLevel={CurrentNoiseLevel} NoiseState={IsNoisy} dB={CurrentDb:0.00} device={_activeDeviceName}");
        }

        onNoiseLevelUpdated.Invoke(CurrentDb, CurrentRms);
        EvaluateCrowdConfirmation();
    }

    private static string EvaluateNoiseLevel(float db)
    {
        if (db < -50f)
            return NoiseQuiet;

        if (db < -35f && db >= -25f)
            return NoiseModerate;

        return NoiseNoisy;
    }

    private void EvaluateCrowdConfirmation()
    {
        CurrentPersonCount = CountDetectedPeople();
        var nextLevel = EvaluateCrowdLevel(CurrentPersonCount);
        SetCrowdState(nextLevel);
    }

    private static string EvaluateCrowdLevel(int personCount)
    {
        if (personCount <= 0)
        {
            return CrowdLow;
        }

        if (personCount <= 1)
        {
            return CrowdModerate;
        }

        return CrowdCrowded;
    }

    private int CountDetectedPeople()
    {
        if (detectionReader == null)
        {
            if (!_missingDetectionReaderWarningShown)
            {
                _missingDetectionReaderWarningShown = true;
                Debug.LogWarning("[AmbientNoiseMonitor] DetectionReader is missing. Crowd level defaults to low.");
            }
            return 0;
        }

        return detectionReader.CountSpecificObject("person", personConfidenceThreshold);
    }

    private void SetCrowdState(string crowdLevel)
    {
        if (string.IsNullOrWhiteSpace(crowdLevel))
        {
            return;
        }

        var normalizedLevel = crowdLevel.Trim().ToLowerInvariant();
        var isCrowdConfirmed = normalizedLevel == CrowdCrowded;
        var levelChanged = !string.Equals(CurrentCrowdLevel, normalizedLevel, StringComparison.Ordinal);
        var boolChanged = IsCrowdConfirmed != isCrowdConfirmed;

        if (!levelChanged && !boolChanged)
        {
            return;
        }

        CurrentCrowdLevel = normalizedLevel;
        IsCrowdConfirmed = isCrowdConfirmed;
        onCrowdStateChanged.Invoke(IsCrowdConfirmed, CurrentPersonCount, CurrentDb);

        if (StateManager.Instance != null)
        {
            StateManager.Instance.SetCrowd(CurrentCrowdLevel);
        }

        if (logStateChanges)
        {
            Debug.Log($"[AmbientNoiseMonitor] CrowdLevel={CurrentCrowdLevel} CrowdState={IsCrowdConfirmed} people={CurrentPersonCount} dB={CurrentDb:0.00}");
        }
    }
}
