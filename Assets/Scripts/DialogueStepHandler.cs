using UnityEngine;

public abstract class DialogueStepHandler : MonoBehaviour
{
    // Called when this step becomes active
    public virtual void OnStepEnter(DialogueFlowController controller, DialogueFlowController.StepDefinition step) {}

    // Called when leaving this step
    public virtual void OnStepExit(DialogueFlowController controller, DialogueFlowController.StepDefinition step) {}

    // Optional per-frame update for this step (only if needed)
    public virtual void Tick(DialogueFlowController controller, DialogueFlowController.StepDefinition step) {}
}
