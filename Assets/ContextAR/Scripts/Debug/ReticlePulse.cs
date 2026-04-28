using UnityEngine;

public class ReticlePulse : MonoBehaviour
{
    public Transform dot;
    public Transform ring;

    Vector3 dotBase;
    Vector3 ringBase;

    void Start()
    {
        dotBase = dot.localScale;
        ringBase = ring.localScale;
    }

    public void HoverOn()
    {
        dot.localScale = dotBase * 1.35f;
        ring.localScale = ringBase * 0.9f;
    }

    public void HoverOff()
    {
        dot.localScale = dotBase;
        ring.localScale = ringBase;
    }
}