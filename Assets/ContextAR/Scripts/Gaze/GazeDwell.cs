using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class GazeDwell: MonoBehaviour
{
    public float dwellTime = 1.2f;

    [Header("UI Feedback")]
    public Image dwellProgressImage; // assign in inspector

    private float timer = 0f;
    private GameObject currentTarget;

    private PointerEventData eventData;
    private List<RaycastResult> results;

    void Start()
    {
        eventData = new PointerEventData(EventSystem.current);
        results = new List<RaycastResult>();

        if (dwellProgressImage != null)
            dwellProgressImage.fillAmount = 0f;
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        eventData.position = new Vector2(Screen.width / 2f, Screen.height / 2f);

        results.Clear();
        EventSystem.current.RaycastAll(eventData, results);

        GameObject hit = GetValidUIObject(results);

        if (hit != currentTarget)
        {
            currentTarget = hit;
            timer = 0f;

            ResetFeedback();
        }

        if (currentTarget != null)
        {
            timer += Time.deltaTime;

            float progress = timer / dwellTime;

            UpdateFeedback(currentTarget, progress);

            if (timer >= dwellTime)
            {
                TriggerClick(currentTarget);
                timer = 0f;
                ResetFeedback();
            }
        }
    }

    GameObject GetValidUIObject(List<RaycastResult> rayResults)
    {
        foreach (var r in rayResults)
        {
            var selectable = r.gameObject.GetComponentInParent<Selectable>();

            if (selectable != null && selectable.IsInteractable())
                return selectable.gameObject;
        }
        return null;
    }

    void UpdateFeedback(GameObject target, float progress)
    {
        if (dwellProgressImage == null) return;

        dwellProgressImage.gameObject.SetActive(true);
        dwellProgressImage.fillAmount = Mathf.Clamp01(progress);

        // follow UI element position
        RectTransform targetRect = target.GetComponent<RectTransform>();

        if (targetRect != null)
        {
            Vector3 worldPos = targetRect.position;
            dwellProgressImage.rectTransform.position = worldPos;
        }
    }

    void ResetFeedback()
    {
        if (dwellProgressImage == null) return;

        dwellProgressImage.fillAmount = 0f;
        dwellProgressImage.gameObject.SetActive(false);
    }

    void TriggerClick(GameObject target)
    {
        var data = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
    }
}