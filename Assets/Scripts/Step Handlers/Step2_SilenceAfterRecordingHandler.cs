using System.Collections;
using UnityEngine;

public class Step2_SilenceBubbleHandler : DialogueStepHandler
{
    [Header("Silence Bubble")]
    public string silenceText = "...";
    public float durationSeconds = 3f;

    [Tooltip("Hide bubble after duration.")]
    public bool hideAfter = true;

    [Header("Animation")]
    [Tooltip("Plays Sad animation while the silence bubble is shown.")]
    public GirlAnimationPlayer girlAnimationPlayer;

    private Coroutine routine;

    public override void OnStepEnter(DialogueFlowController controller, DialogueFlowController.StepDefinition step)
    {
        // Show bubble with single segment "..."
        controller.SetGirlDialogueSegments(new[] { silenceText }, showImmediately: true);

        // Play Sad animation
        if (girlAnimationPlayer != null)
            girlAnimationPlayer.PlaySad();

        // Start timer to auto-advance
        routine = controller.StartCoroutine(Run(controller));
    }

    public override void OnStepExit(DialogueFlowController controller, DialogueFlowController.StepDefinition step)
    {
        if (routine != null)
        {
            controller.StopCoroutine(routine);
            routine = null;
        }

        if (hideAfter)
            controller.ClearGirlDialogue();
    }

    private IEnumerator Run(DialogueFlowController controller)
    {
        yield return new WaitForSeconds(durationSeconds);

        if (hideAfter)
            controller.ClearGirlDialogue();

        controller.CompleteCurrentStep();
        routine = null;
    }
}