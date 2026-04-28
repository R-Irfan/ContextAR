using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Add to any UI object (Button, TMP text box, Image, Panel, etc.)
/// Exposes hover enter / hover exit / hover stay events in Inspector,
/// similar to Button OnClick().
///
/// Requires:
/// - Canvas with GraphicRaycaster
/// - EventSystem in scene
/// - UI element with a raycastable Graphic (Image/Text/TMP etc.)
/// </summary>
public class UIHoverEventTrigger : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Hover Events")]
    public UnityEvent OnHoverEnter;
    public UnityEvent OnHoverExit;

    [Tooltip("Invoked every frame while pointer remains over object.")]
    public UnityEvent OnHoverStay;

    [Header("Press Events")]
    public UnityEvent OnPressDown;
    public UnityEvent OnPressUp;

    private bool _isHovering;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        OnHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        OnHoverExit?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnPressDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnPressUp?.Invoke();
    }

    private void Update()
    {
        if (_isHovering)
        {
            OnHoverStay?.Invoke();
        }
    }
}