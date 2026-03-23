using UnityEngine;

public class DialogueStartTrigger : MonoBehaviour
{
    public DialogueFlowController dialogueFlow;
    public string playerTag = "Player";

    private bool started = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TriggerDebug] ENTER by: {other.name}, tag={other.tag}, layer={LayerMask.LayerToName(other.gameObject.layer)}");
    
        if (started) return;

        // Option A: tag-based (recommended if you tag your XR Origin)
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
        {
            started = true;
            dialogueFlow.StartFlow();
            Debug.Log("[DialogueStartTrigger] Player entered. Dialogue flow started.");
            return;
        }

        // Option B: fallback if you don't want tags
        // if (other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null) { ... }
    }
}
