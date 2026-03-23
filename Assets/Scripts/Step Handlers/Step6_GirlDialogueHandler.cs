using UnityEngine;

public class Step6_GirlDialogueHandler : DialogueStepHandler
{
    [TextArea] public string[] girlLines;

    [Header("Animation")]
    [Tooltip("Plays Talking animation while girl dialogue bubble is shown.")]
    public GirlAnimationPlayer girlAnimationPlayer;

    private DialogueFlowController controller;
    private bool completed = false;

    public override void OnStepEnter(DialogueFlowController controller, DialogueFlowController.StepDefinition step)
    {
        this.controller = controller;
        completed = false;

        controller.SetGirlDialogueSegments(girlLines, showImmediately: true);

        // Start Talking animation while bubble is shown
        if (girlAnimationPlayer != null)
            girlAnimationPlayer.PlayTalking();

        // Subscribe: when last segment is reached -> complete step
        controller.OnGirlDialogueReachedLastSegment += HandleReachedLast;
    }

    public override void OnStepExit(DialogueFlowController controller, DialogueFlowController.StepDefinition step)
    {
        controller.OnGirlDialogueReachedLastSegment -= HandleReachedLast;
        controller.ClearGirlDialogue();

        // Return to idle when dialogue is done
        if (girlAnimationPlayer != null)
            girlAnimationPlayer.StopTalking();

        this.controller = null;
        completed = false;
    }

    private void HandleReachedLast()
    {
        if (completed) return;
        completed = true;

        // Auto-advance to next step
        controller.CompleteCurrentStep();
    }
}