using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraCapture : MonoBehaviour
{
    [Header("Passthrough Camera")]
    [SerializeField] private PassthroughCameraAccess passthroughCameraAccess;
    [SerializeField] private bool autoFindPassthroughCameraAccess = true;

    [Header("Capture")]
    [SerializeField, Range(1, 100)] private int jpgQuality = 70;
    [SerializeField, Min(0.1f)] private float captureTimeoutSeconds = 5f;
    [SerializeField] private bool autoRequestPermission = true;

    private readonly List<OVRPermissionsRequester.Permission> _cameraPermissionRequest =
        new List<OVRPermissionsRequester.Permission> { OVRPermissionsRequester.Permission.PassthroughCameraAccess };

    private Texture2D _cameraTexture;

    private void Awake()
    {
        if (autoFindPassthroughCameraAccess && passthroughCameraAccess == null)
        {
            passthroughCameraAccess = FindAnyObjectByType<PassthroughCameraAccess>();
        }
    }

    private void OnDestroy()
    {
        if (_cameraTexture != null)
        {
            Destroy(_cameraTexture);
            _cameraTexture = null;
        }
    }

    public IEnumerator CaptureFrame(Action<string> onImageReady)
    {
        if (!PassthroughCameraAccess.IsSupported)
        {
            Fail("Passthrough Camera Access is not supported on this platform/headset.", onImageReady);
            yield break;
        }

        if (passthroughCameraAccess == null)
        {
            Fail("No PassthroughCameraAccess component assigned or found.", onImageReady);
            yield break;
        }

        float startTime = Time.realtimeSinceStartup;

        if (autoRequestPermission && !HasCameraPermission())
        {
            OVRPermissionsRequester.Request(_cameraPermissionRequest);
        }

        while (!HasCameraPermission())
        {
            if (HasTimedOut(startTime))
            {
                Fail("Timed out waiting for headset camera permission.", onImageReady);
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
                Fail("Timed out waiting for PassthroughCameraAccess to start playing.", onImageReady);
                yield break;
            }

            yield return null;
        }

        while (!passthroughCameraAccess.IsUpdatedThisFrame)
        {
            if (HasTimedOut(startTime))
            {
                Fail("Timed out waiting for a fresh passthrough camera frame.", onImageReady);
                yield break;
            }

            yield return null;
        }

        var sourceTexture = passthroughCameraAccess.GetTexture();
        if (sourceTexture == null)
        {
            Fail("Passthrough camera returned a null texture.", onImageReady);
            yield break;
        }

        if (sourceTexture.width <= 0 || sourceTexture.height <= 0)
        {
            Fail("Passthrough camera texture has invalid dimensions.", onImageReady);
            yield break;
        }

        EnsureTexture(sourceTexture.width, sourceTexture.height);

        var readbackRequest = AsyncGPUReadback.Request(sourceTexture, 0);
        while (!readbackRequest.done)
        {
            if (HasTimedOut(startTime))
            {
                Fail("Timed out waiting for GPU readback.", onImageReady);
                yield break;
            }

            yield return null;
        }

        if (readbackRequest.hasError)
        {
            Fail("AsyncGPUReadback failed while retrieving camera pixels.", onImageReady);
            yield break;
        }

        var pixelData = readbackRequest.GetData<Color32>();
        if (!pixelData.IsCreated || pixelData.Length == 0)
        {
            Fail("GPU readback returned empty pixel data.", onImageReady);
            yield break;
        }

        _cameraTexture.LoadRawTextureData(pixelData);
        _cameraTexture.Apply(false, false);

        byte[] jpg = _cameraTexture.EncodeToJPG(Mathf.Clamp(jpgQuality, 1, 100));
        string base64 = Convert.ToBase64String(jpg);
        onImageReady?.Invoke(base64);
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
        if (_cameraTexture != null && _cameraTexture.width == width && _cameraTexture.height == height)
        {
            return;
        }

        if (_cameraTexture != null)
        {
            Destroy(_cameraTexture);
        }

        _cameraTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
    }

    private static void Fail(string message, Action<string> onImageReady)
    {
        Debug.LogWarning($"[CameraCapture] {message}");
        onImageReady?.Invoke(null);
    }
}
