using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class VisitorQuestionButtonTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExperienceLayerController experienceLayerController;
#if TMP_PRESENT
    [SerializeField] private TMP_InputField questionInputTmp;
#endif
    [SerializeField] private InputField questionInputLegacy;

    [Header("Fallback Values")]
    [SerializeField, TextArea] private string fallbackQuestion = "What is this painting?";
    [SerializeField, Min(0f)] private float fallbackGazeDurationSeconds = 13f;
    [SerializeField] private string fallbackCrowdLevel = "low";
    [SerializeField] private string fallbackNoiseLevel = "quiet";
    [SerializeField] private bool fallbackSendImage = false;

    [Header("State Source")]
    [SerializeField] private bool useStateManagerForCrowdNoiseAndGaze = true;

    [Header("Submission Control")]
    [SerializeField, Min(0f)] private float minimumSecondsBetweenSubmissions = 5f;

    [Header("Debug")]
    [SerializeField] private bool logSubmission = true;

    private float _lastSubmissionTime = float.NegativeInfinity;

    private void Awake()
    {
        if (experienceLayerController == null)
        {
            experienceLayerController = FindAnyObjectByType<ExperienceLayerController>();
        }
    }

    private void OnValidate()
    {
        if (minimumSecondsBetweenSubmissions < 5f)
        {
            minimumSecondsBetweenSubmissions = 5f;
        }
    }

    // Assign this method in Button.onClick
    public void SubmitQuestion()
    {
        if (experienceLayerController == null)
        {
            Debug.LogWarning("[VisitorQuestionButtonTrigger] Missing ExperienceLayerController reference.");
            return;
        }

        var now = Time.unscaledTime;
        var elapsed = now - _lastSubmissionTime;
        if (elapsed < minimumSecondsBetweenSubmissions)
        {
            var waitRemaining = minimumSecondsBetweenSubmissions - elapsed;
            Debug.LogWarning($"[VisitorQuestionButtonTrigger] Please wait {waitRemaining:0.0}s before submitting again.");
            return;
        }

        var question = ResolveQuestion();
        if (string.IsNullOrWhiteSpace(question))
        {
            Debug.LogWarning("[VisitorQuestionButtonTrigger] Question is empty. Nothing sent.");
            return;
        }

        var gazeDuration = fallbackGazeDurationSeconds;
        var crowdLevel = fallbackCrowdLevel;
        var noiseLevel = fallbackNoiseLevel;

        if (useStateManagerForCrowdNoiseAndGaze && StateManager.Instance != null && StateManager.Instance.CurrentState != null)
        {
            var state = StateManager.Instance.CurrentState;
            gazeDuration = Mathf.Max(0f, state.gaze_duration);

            if (!string.IsNullOrWhiteSpace(state.crowd))
            {
                crowdLevel = state.crowd;
            }

            if (!string.IsNullOrWhiteSpace(state.noise))
            {
                noiseLevel = state.noise;
            }
        }

        if (logSubmission)
        {
            Debug.Log($"[VisitorQuestionButtonTrigger] Submitting question=\"{question}\" gaze={gazeDuration:0.00}s crowd={crowdLevel} noise={noiseLevel}");
        }

        _lastSubmissionTime = now;
        //experienceLayerController.OnVisitorQuestion(question, gazeDuration, crowdLevel, noiseLevel, fallbackSendImage);
        experienceLayerController.OnVisitorQuestion(question,  fallbackGazeDurationSeconds, fallbackCrowdLevel, fallbackNoiseLevel, fallbackSendImage);
    }

    private string ResolveQuestion()
    {
#if TMP_PRESENT
        if (questionInputTmp != null && !string.IsNullOrWhiteSpace(questionInputTmp.text))
        {
            return questionInputTmp.text.Trim();
        }
#endif

        if (questionInputLegacy != null && !string.IsNullOrWhiteSpace(questionInputLegacy.text))
        {
            return questionInputLegacy.text.Trim();
        }

        return string.IsNullOrWhiteSpace(fallbackQuestion) ? string.Empty : fallbackQuestion.Trim();
    }
}
