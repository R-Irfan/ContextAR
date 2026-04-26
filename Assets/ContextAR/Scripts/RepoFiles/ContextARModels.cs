using System;

[Serializable]
public class AskState
{
    public string crowd;          // "low" | "moderate" | "crowded"
    public string noise;          // "quiet" | "moderate" | "noisy"
    public float  gaze_duration;  // seconds
}

[Serializable]
public class AskRequest
{
    public string   question;
    public string   image_base64;  // optional — omit to skip exhibit recognition
    public AskState state;
}

[Serializable]
public class AskResponse
{
    public string mode;     // NO_RESPONSE | BRIEF_TEXT | GLANCE_CARD | FULL_VOICE | BRIEF_TEXT_PROMPT
    public string answer;   // empty for NO_RESPONSE
    public string exhibit;  // recognised exhibit name; empty if not identified
}

[Serializable]
public class SetPaintingRequest
{
    public string painting_name;
}
