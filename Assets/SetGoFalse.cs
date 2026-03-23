using UnityEngine;

public class SetGoFalse : StateMachineBehaviour
{
    public string parameterName = "Go";

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Set false as soon as we ENTER this state (no waiting)
        animator.SetBool(parameterName, false);
        Debug.Log($"[SetGoFalse] Set {parameterName}=false on ENTER of {stateInfo.fullPathHash}");
    }
}