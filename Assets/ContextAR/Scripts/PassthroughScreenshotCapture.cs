using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class PassthroughScreenshotCapture : MonoBehaviour
{
    public enum CaptureSourceMode
    {
        AutoPreferPassthrough = 0,
        AutoPreferWebcam = 1,
        PassthroughOnly = 2,
        WebcamOnly = 3
    }

    [Header("Passthrough Camera")]
    [SerializeField] private PassthroughCameraAccess passthroughCameraAccess;
    [SerializeField] private bool autoFindPassthroughCameraAccess = true;

    [Header("Webcam")]
    [SerializeField] private WebcamObjectDetectionAgent webcamAgent;
    [SerializeField] private bool autoFindWebcamAgent = true;
    [SerializeField] private bool autoStartWebcamIfNeeded = true;

    [Header("Capture")]
    [SerializeField] private CaptureSourceMode captureSourceMode = CaptureSourceMode.AutoPreferPassthrough;
    [SerializeField, Range(1, 100)] private int jpgQuality = 70;
    [SerializeField, Min(0.1f)] private float captureTimeoutSeconds = 5f;
    [SerializeField] private bool autoRequestPermission = true;

    private readonly List<OVRPermissionsRequester.Permission> _cameraPermissionRequest =
        new List<OVRPermissionsRequester.Permission> { OVRPermissionsRequester.Permission.PassthroughCameraAccess };

    private Texture2D _captureTexture;
    private Color32[] _webcamPixels;

    public Texture2D LastCapturedScreenshot => _captureTexture;

    private void Awake()
    {
        if (autoFindPassthroughCameraAccess && passthroughCameraAccess == null)
        {
            passthroughCameraAccess = FindAnyObjectByType<PassthroughCameraAccess>();
        }

        if (autoFindWebcamAgent && webcamAgent == null)
        {
            webcamAgent = FindAnyObjectByType<WebcamObjectDetectionAgent>();
        }
    }

    private void OnDestroy()
    {
        if (_captureTexture != null)
        {
            Destroy(_captureTexture);
            _captureTexture = null;
        }
    }

    public IEnumerator CaptureScreenshot(Action<Texture2D> onScreenshotReady)
    {
        yield return CaptureScreenshot(captureSourceMode, onScreenshotReady);
    }

    public IEnumerator CaptureScreenshot(CaptureSourceMode sourceMode, Action<Texture2D> onScreenshotReady)
    {
        Texture sourceTexture = null;
        CaptureSourceMode resolvedSource = sourceMode;

        if (sourceMode == CaptureSourceMode.AutoPreferPassthrough || sourceMode == CaptureSourceMode.AutoPreferWebcam)
        {
            var preferPassthrough = sourceMode == CaptureSourceMode.AutoPreferPassthrough;
            yield return TryResolveAutoSourceTexture(preferPassthrough, texture => sourceTexture = texture, mode => resolvedSource = mode);
        }
        else
        {
            yield return TryResolveSourceTexture(sourceMode, texture => sourceTexture = texture);
        }

        if (sourceTexture == null)
        {
            Fail("Failed to capture from both passthrough and webcam sources.", onScreenshotReady);
            yield break;
        }

        if (resolvedSource == CaptureSourceMode.WebcamOnly)
        {
            if (!TryCopyFromWebcam(sourceTexture as WebCamTexture))
            {
                Fail("Failed to copy webcam pixels.", onScreenshotReady);
                yield break;
            }

            onScreenshotReady?.Invoke(_captureTexture);
            yield break;
        }

        yield return TryCopyFromTextureGpu(sourceTexture, texture =>
        {
            if (texture == null)
            {
                Fail("Failed to read back camera pixels from source texture.", onScreenshotReady);
                return;
            }

            onScreenshotReady?.Invoke(texture);
        });
    }

    public IEnumerator CaptureScreenshotFromPassthrough(Action<Texture2D> onScreenshotReady)
    {
        yield return CaptureScreenshot(CaptureSourceMode.PassthroughOnly, onScreenshotReady);
    }

    public IEnumerator CaptureScreenshotFromWebcam(Action<Texture2D> onScreenshotReady)
    {
        yield return CaptureScreenshot(CaptureSourceMode.WebcamOnly, onScreenshotReady);
    }

    public Coroutine CaptureScreenshotAsync(Action<Texture2D> onScreenshotReady)
    {
        return StartCoroutine(CaptureScreenshot(onScreenshotReady));
    }

    public Coroutine CaptureScreenshotAsync(CaptureSourceMode sourceMode, Action<Texture2D> onScreenshotReady)
    {
        return StartCoroutine(CaptureScreenshot(sourceMode, onScreenshotReady));
    }

    public Coroutine CaptureScreenshotFromPassthroughAsync(Action<Texture2D> onScreenshotReady)
    {
        return StartCoroutine(CaptureScreenshotFromPassthrough(onScreenshotReady));
    }

    public Coroutine CaptureScreenshotFromWebcamAsync(Action<Texture2D> onScreenshotReady)
    {
        return StartCoroutine(CaptureScreenshotFromWebcam(onScreenshotReady));
    }

    public string ScreenshotToBase64(Texture2D screenshot)
    {
        if (screenshot == null)
        {
            Debug.LogWarning("[PassthroughScreenshotCapture] Cannot encode null screenshot.");
            return null;
        }

        byte[] jpg = screenshot.EncodeToJPG(Mathf.Clamp(jpgQuality, 1, 100));
        if (jpg == null || jpg.Length == 0)
        {
            Debug.LogWarning("[PassthroughScreenshotCapture] JPEG encoding returned empty data.");
            return null;
        }

        return Convert.ToBase64String(jpg);
    }

    public string LastScreenshotToBase64()
    {
        return ScreenshotToBase64(_captureTexture);
    }

    public IEnumerator CaptureScreenshotBase64(Action<string> onBase64Ready)
    {
        yield return CaptureScreenshotBase64(captureSourceMode, onBase64Ready);
    }

    public IEnumerator CaptureScreenshotBase64(CaptureSourceMode sourceMode, Action<string> onBase64Ready)
    {
        Texture2D screenshot = null;
        yield return CaptureScreenshot(sourceMode, captured => screenshot = captured);
        onBase64Ready?.Invoke(screenshot == null ? null : ScreenshotToBase64(screenshot));
    }

    public IEnumerator CaptureScreenshotBase64FromPassthrough(Action<string> onBase64Ready)
    {
        yield return CaptureScreenshotBase64(CaptureSourceMode.PassthroughOnly, onBase64Ready);
    }

    public IEnumerator CaptureScreenshotBase64FromWebcam(Action<string> onBase64Ready)
    {
        yield return CaptureScreenshotBase64(CaptureSourceMode.WebcamOnly, onBase64Ready);
    }

    public Coroutine CaptureScreenshotBase64Async(Action<string> onBase64Ready)
    {
        return StartCoroutine(CaptureScreenshotBase64(onBase64Ready));
    }

    public Coroutine CaptureScreenshotBase64Async(CaptureSourceMode sourceMode, Action<string> onBase64Ready)
    {
        return StartCoroutine(CaptureScreenshotBase64(sourceMode, onBase64Ready));
    }

    public Coroutine CaptureScreenshotBase64FromPassthroughAsync(Action<string> onBase64Ready)
    {
        return StartCoroutine(CaptureScreenshotBase64FromPassthrough(onBase64Ready));
    }

    public Coroutine CaptureScreenshotBase64FromWebcamAsync(Action<string> onBase64Ready)
    {
        return StartCoroutine(CaptureScreenshotBase64FromWebcam(onBase64Ready));
    }

    private IEnumerator TryResolveAutoSourceTexture(bool preferPassthrough, Action<Texture> onTextureReady, Action<CaptureSourceMode> onSourceResolved)
    {
        Texture texture = null;
        var first = preferPassthrough ? CaptureSourceMode.PassthroughOnly : CaptureSourceMode.WebcamOnly;
        var second = preferPassthrough ? CaptureSourceMode.WebcamOnly : CaptureSourceMode.PassthroughOnly;

        yield return TryResolveSourceTexture(first, found => texture = found);
        if (texture != null)
        {
            onSourceResolved?.Invoke(first);
            onTextureReady?.Invoke(texture);
            yield break;
        }

        yield return TryResolveSourceTexture(second, found => texture = found);
        if (texture != null)
        {
            onSourceResolved?.Invoke(second);
            onTextureReady?.Invoke(texture);
            yield break;
        }

        onTextureReady?.Invoke(null);
    }

    private IEnumerator TryResolveSourceTexture(CaptureSourceMode sourceMode, Action<Texture> onTextureReady)
    {
        Texture sourceTexture = null;

        if (sourceMode == CaptureSourceMode.PassthroughOnly)
        {
            yield return TryGetPassthroughTexture(found => sourceTexture = found);
        }
        else if (sourceMode == CaptureSourceMode.WebcamOnly)
        {
            yield return TryGetWebcamTexture(found => sourceTexture = found);
        }

        onTextureReady?.Invoke(sourceTexture);
    }

    private IEnumerator TryGetPassthroughTexture(Action<Texture> onTextureReady)
    {
        if (!PassthroughCameraAccess.IsSupported)
        {
            Debug.LogWarning("[PassthroughScreenshotCapture] Passthrough Camera Access is not supported on this platform/headset.");
            onTextureReady?.Invoke(null);
            yield break;
        }

        if (passthroughCameraAccess == null)
        {
            if (autoFindPassthroughCameraAccess)
            {
                passthroughCameraAccess = FindAnyObjectByType<PassthroughCameraAccess>();
            }
        }

        if (passthroughCameraAccess == null)
        {
            Debug.LogWarning("[PassthroughScreenshotCapture] No PassthroughCameraAccess component assigned or found.");
            onTextureReady?.Invoke(null);
            yield break;
        }

        var startTime = Time.realtimeSinceStartup;

        if (autoRequestPermission && !HasCameraPermission())
        {
            OVRPermissionsRequester.Request(_cameraPermissionRequest);
        }

        while (!HasCameraPermission())
        {
            if (HasTimedOut(startTime))
            {
                Debug.LogWarning("[PassthroughScreenshotCapture] Timed out waiting for headset camera permission.");
                onTextureReady?.Invoke(null);
                yield break;
            }

            yield return null;
        }

        if (!passthroughCameraAccess.enabled)
        {
            passthroughCameraAccess.enabled = true;
        }

        while (!passthroughCameraAccess.IsPlaying)
        {
            if (HasTimedOut(startTime))
            {
                Debug.LogWarning("[PassthroughScreenshotCapture] Timed out waiting for PassthroughCameraAccess to start playing.");
                onTextureReady?.Invoke(null);
                yield break;
            }

            yield return null;
        }

        while (!passthroughCameraAccess.IsUpdatedThisFrame)
        {
            if (HasTimedOut(startTime))
            {
                Debug.LogWarning("[PassthroughScreenshotCapture] Timed out waiting for a fresh passthrough camera frame.");
                onTextureReady?.Invoke(null);
                yield break;
            }

            yield return null;
        }

        var sourceTexture = passthroughCameraAccess.GetTexture();
        if (sourceTexture == null || sourceTexture.width <= 0 || sourceTexture.height <= 0)
        {
            Debug.LogWarning("[PassthroughScreenshotCapture] Passthrough texture is null or has invalid dimensions.");
            onTextureReady?.Invoke(null);
            yield break;
        }

        onTextureReady?.Invoke(sourceTexture);
    }

    private IEnumerator TryGetWebcamTexture(Action<Texture> onTextureReady)
    {
        if (webcamAgent == null)
        {
            if (autoFindWebcamAgent)
            {
                webcamAgent = FindAnyObjectByType<WebcamObjectDetectionAgent>();
            }
        }

        if (webcamAgent == null)
        {
            Debug.LogWarning("[PassthroughScreenshotCapture] No WebcamObjectDetectionAgent component assigned or found.");
            onTextureReady?.Invoke(null);
            yield break;
        }

        if (!webcamAgent.IsWebcamRunning && autoStartWebcamIfNeeded)
        {
            webcamAgent.StartWebcam();
        }

        var startTime = Time.realtimeSinceStartup;
        while (true)
        {
            var webcamTexture = webcamAgent.WebcamTexture;
            var isReady = webcamTexture != null
                          && webcamTexture.isPlaying
                          && webcamTexture.didUpdateThisFrame
                          && webcamTexture.width > 0
                          && webcamTexture.height > 0;

            if (isReady)
            {
                onTextureReady?.Invoke(webcamTexture);
                yield break;
            }

            if (HasTimedOut(startTime))
            {
                Debug.LogWarning("[PassthroughScreenshotCapture] Timed out waiting for webcam frame.");
                onTextureReady?.Invoke(null);
                yield break;
            }

            yield return null;
        }
    }

    private bool TryCopyFromWebcam(WebCamTexture webcamTexture)
    {
        if (webcamTexture == null || webcamTexture.width <= 0 || webcamTexture.height <= 0)
        {
            return false;
        }

        EnsureTexture(webcamTexture.width, webcamTexture.height);

        var pixelCount = webcamTexture.width * webcamTexture.height;
        if (_webcamPixels == null || _webcamPixels.Length != pixelCount)
        {
            _webcamPixels = new Color32[pixelCount];
        }

        webcamTexture.GetPixels32(_webcamPixels);
        if (_webcamPixels.Length == 0)
        {
            return false;
        }

        _captureTexture.SetPixels32(_webcamPixels);
        _captureTexture.Apply(false, false);
        return true;
    }

    private IEnumerator TryCopyFromTextureGpu(Texture sourceTexture, Action<Texture2D> onCopyReady)
    {
        if (sourceTexture == null || sourceTexture.width <= 0 || sourceTexture.height <= 0)
        {
            onCopyReady?.Invoke(null);
            yield break;
        }

        EnsureTexture(sourceTexture.width, sourceTexture.height);

        var startTime = Time.realtimeSinceStartup;
        var readbackRequest = AsyncGPUReadback.Request(sourceTexture, 0);
        while (!readbackRequest.done)
        {
            if (HasTimedOut(startTime))
            {
                onCopyReady?.Invoke(null);
                yield break;
            }

            yield return null;
        }

        if (readbackRequest.hasError)
        {
            onCopyReady?.Invoke(null);
            yield break;
        }

        NativeArray<Color32> pixelData = readbackRequest.GetData<Color32>();
        if (!pixelData.IsCreated || pixelData.Length == 0)
        {
            onCopyReady?.Invoke(null);
            yield break;
        }

        _captureTexture.LoadRawTextureData(pixelData);
        _captureTexture.Apply(false, false);
        onCopyReady?.Invoke(_captureTexture);
    }

    private bool HasCameraPermission()
    {
        return OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess);
    }

    private bool HasTimedOut(float startTime)
    {
        return Time.realtimeSinceStartup - startTime >= captureTimeoutSeconds;
    }

    private void EnsureTexture(int width, int height)
    {
        if (_captureTexture != null && _captureTexture.width == width && _captureTexture.height == height)
        {
            return;
        }

        if (_captureTexture != null)
        {
            Destroy(_captureTexture);
        }

        _captureTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
    }

    private static void Fail(string message, Action<Texture2D> onScreenshotReady)
    {
        Debug.LogWarning($"[PassthroughScreenshotCapture] {message}");
        onScreenshotReady?.Invoke(null);
    }
}
