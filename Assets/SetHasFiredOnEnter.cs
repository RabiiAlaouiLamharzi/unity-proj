using UnityEngine;

public class SetHasFiredOnEnter : StateMachineBehaviour
{
    public string hasFiredParam = "HasFired";

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(hasFiredParam, true);
    }
}