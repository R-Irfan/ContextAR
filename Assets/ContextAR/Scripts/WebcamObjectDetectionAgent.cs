using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Meta.XR.BuildingBlocks.AIBlocks;
using UnityEngine;

public class WebcamObjectDetectionAgent : MonoBehaviour
{
    private const int DefaultBatchCapacity = 32;

    [Header("Provider")]
    [SerializeField] private AIProviderBase providerAsset;
    [Tooltip("Filter out detections below this confidence threshold.")]
    [Range(0f, 1f)] public float minConfidence = 0.5f;
    [Tooltip("Run detection every N frames. Set to 0 for manual detection only.")]
    [SerializeField, Range(0, 120)] private int detectEveryNFrames = 1;

    [Header("Webcam")]
    [Tooltip("Optional exact webcam name. Leave empty to use the first available device.")]
    [SerializeField] private string preferredDeviceName = "";
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFps = 30;
    [SerializeField] private bool autoStartOnEnable = true;

    [SerializeField] private OnDetectionResponseReceived onDetectionResponseReceived = new();
    public OnDetectionResponseReceived OnDetectionResponseReceived => onDetectionResponseReceived;

    public WebCamTexture WebcamTexture => _webcamTexture;
    public bool IsWebcamRunning => _webcamTexture != null && _webcamTexture.isPlaying;

    private readonly List<BoxData> _batch = new(DefaultBatchCapacity);
    private IObjectDetectionTask _detector;
    private UnityInferenceEngineProvider _unityProvider;
    private WebCamTexture _webcamTexture;
    private Task _providerInitTask;
    private bool _busy;
    private bool _unsupportedProviderWarningShown;

    private async void Awake()
    {
        _providerInitTask = InitializeProviderAsync();
        await _providerInitTask;
    }

    private void OnEnable()
    {
        if (autoStartOnEnable)
        {
            StartWebcam();
        }
    }

    private void OnDisable()
    {
        StopWebcam();
    }

    private async Task InitializeProviderAsync()
    {
        _detector = providerAsset as IObjectDetectionTask;
        _unityProvider = providerAsset as UnityInferenceEngineProvider;

        if (_detector == null)
        {
            Debug.LogError("[WebcamObjectDetectionAgent] providerAsset must implement IObjectDetectionTask.");
            return;
        }

        if (_unityProvider != null)
        {
            try
            {
                await _unityProvider.WarmUp();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebcamObjectDetectionAgent] Provider warmup failed: {ex.Message}");
            }
        }
    }

    private void Update()
    {
        if (_providerInitTask != null && !_providerInitTask.IsCompleted)
        {
            return;
        }

        if (_busy || _webcamTexture == null || !_webcamTexture.isPlaying || !_webcamTexture.didUpdateThisFrame)
        {
            return;
        }

        if (detectEveryNFrames > 0 && Time.frameCount % detectEveryNFrames == 0)
        {
            CallInference();
        }
    }

    public void StartWebcam()
    {
        if (_webcamTexture != null && _webcamTexture.isPlaying)
        {
            return;
        }

        var deviceName = ResolveDeviceName();
        if (string.IsNullOrEmpty(deviceName))
        {
            Debug.LogError("[WebcamObjectDetectionAgent] No webcam devices found.");
            return;
        }

        _webcamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFps);
        _webcamTexture.Play();
    }

    public void StopWebcam()
    {
        if (_webcamTexture == null)
        {
            return;
        }

        if (_webcamTexture.isPlaying)
        {
            _webcamTexture.Stop();
        }

        Destroy(_webcamTexture);
        _webcamTexture = null;
    }

    public void CallInference() => _ = RunDetection();

    private async Task RunDetection()
    {
        if (_busy)
        {
            return;
        }

        if (_providerInitTask != null && !_providerInitTask.IsCompleted)
        {
            await _providerInitTask;
        }

        _busy = true;
        try
        {
            if (_unityProvider == null)
            {
                if (!_unsupportedProviderWarningShown)
                {
                    _unsupportedProviderWarningShown = true;
                    Debug.LogWarning("[WebcamObjectDetectionAgent] Only UnityInferenceEngineProvider is supported in this webcam test script.");
                }
                return;
            }

            if (_webcamTexture == null || !_webcamTexture.isPlaying)
            {
                return;
            }

            var bin = await _unityProvider.DetectAsync(_webcamTexture);
            if (bin == null || bin.Length == 0)
            {
                return;
            }

            var predictions = DecodeBinaryDetections(bin);
            if (predictions == null || predictions.Length == 0)
            {
                return;
            }

            _batch.Clear();
            foreach (var p in predictions)
            {
                if (p.score < minConfidence || p.box == null || p.box.Length < 4)
                {
                    continue;
                }

                _batch.Add(new BoxData
                {
                    position = new Vector3(p.box[0], p.box[1], 0f),
                    scale = new Vector3(p.box[2], p.box[3], 0f),
                    rotation = Quaternion.identity,
                    label = $"{p.label} {p.score:0.00}"
                });
            }

            onDetectionResponseReceived.Invoke(_batch);
        }
        finally
        {
            _busy = false;
        }
    }

    private string ResolveDeviceName()
    {
        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(preferredDeviceName))
        {
            foreach (var device in devices)
            {
                if (string.Equals(device.name, preferredDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    return device.name;
                }
            }
        }

        return devices[0].name;
    }

    private static DetectionPrediction[] DecodeBinaryDetections(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        try
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);
            var count = br.ReadInt32();
            var predictions = new DetectionPrediction[count];

            for (var i = 0; i < count; i++)
            {
                var x = br.ReadSingle();
                var y = br.ReadSingle();
                var w = br.ReadSingle();
                var h = br.ReadSingle();
                var score = br.ReadSingle();
                br.ReadInt32(); // classId
                var label = br.ReadString();

                predictions[i] = new DetectionPrediction
                {
                    box = new[] { x, y, w, h },
                    score = score,
                    label = label
                };
            }

            return predictions;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebcamObjectDetectionAgent] Failed to decode detections: {ex.Message}");
            return null;
        }
    }

    private sealed class DetectionPrediction
    {
        public float score;
        public string label;
        public float[] box;
    }
}
