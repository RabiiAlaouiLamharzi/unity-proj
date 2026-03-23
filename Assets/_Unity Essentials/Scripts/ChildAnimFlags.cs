using UnityEngine;

public class ChildAnimFlags : MonoBehaviour
{
    [SerializeField] private Animator childAnimator;
    [SerializeField] private string swapBoolName = "Swap";

    public void SetSwapTrue()
    {
        if (!childAnimator) childAnimator = GetComponent<Animator>();
        if (!childAnimator)
        {
            Debug.LogError("[ChildAnimFlags] No Animator found.");
            return;
        }

        childAnimator.SetBool(swapBoolName, true);
    }

    public void SetSwapFalse()
    {
        if (!childAnimator) childAnimator = GetComponent<Animator>();
        if (!childAnimator)
        {
            Debug.LogError("[ChildAnimFlags] No Animator found.");
            return;
        }

        childAnimator.SetBool(swapBoolName, false);
    }
}