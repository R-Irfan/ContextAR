// ContextARModels.cs
using System;

[Serializable] public class HandState   { public bool detected; public bool both_holding; }
[Serializable] public class CrowdState  { public int count; public string level; }
[Serializable] public class NoiseState  { public float db; public string level; }
[Serializable] public class StateResponse
{
    public float     timestamp;
    public HandState hands;
    public CrowdState crowd;
    public NoiseState noise;
    public string    suggestion;
}

[Serializable] public class AskState   { public string crowd; public string noise; public bool detected; public bool both_holding; }
[Serializable] public class AskRequest { public string question; public string image_base64; public AskState state; }
[Serializable] public class AskResponse{ public string mode; public string answer; public string audio_url; public string exhibit; }