using UnityEngine;

public class HeadTouchSensor : MonoBehaviour
{
    [Tooltip("Set to a layer used by hand colliders (recommended).")]
    public LayerMask handLayerMask;

    public bool IsTouching { get; private set; }
    public int TouchCount { get; private set; } = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsHand(other)) return;
        TouchCount++;
        IsTouching = TouchCount > 0;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsHand(other)) return;
        TouchCount = Mathf.Max(0, TouchCount - 1);
        IsTouching = TouchCount > 0;
    }

    private bool IsHand(Collider other)
    {
        int otherLayerMask = 1 << other.gameObject.layer;
        return (handLayerMask.value & otherLayerMask) != 0;
    }
}