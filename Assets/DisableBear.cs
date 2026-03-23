using UnityEngine;

public class DisableBear : StateMachineBehaviour
{
    public string goParameter = "Go";
    public string bearTag = "Bear";

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.GetBool(goParameter)) return;

        var bears = GameObject.FindGameObjectsWithTag(bearTag);
        if (bears == null || bears.Length == 0)
        {
            Debug.LogWarning($"[DisableBear] No objects found with tag '{bearTag}'.");
            return;
        }

        foreach (var bear in bears)
        {
            // Disable the whole bear root
            bear.SetActive(false);
        }
    }
}