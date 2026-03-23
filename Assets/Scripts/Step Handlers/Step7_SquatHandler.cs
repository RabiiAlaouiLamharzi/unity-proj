using UnityEngine;

public class Step7_SquatHandler : DialogueStepHandler
{
    [Header("Squat Detection")]
    [Tooltip("How much the HMD height must drop (meters) to count as squat. 0.20 = 20cm.")]
    public float requiredDropMeters = 0.20f;

    [Tooltip("Hold time below threshold to confirm (seconds). 0 = instant.")]
    public float holdSeconds = 0.15f;

    [Tooltip("Optional: if player stands back up, reset hold timer.")]
    public bool resetIfStandUp = true;

    [Header("References")]
    [Tooltip("If null, will use Camera.main.")]
    public Transform hmdTransform;

    [Tooltip("Happy reaction triggered after squatting completes — plays with particles + giggle.")]
    public GirlHappyReaction girlHappyReaction;

    private float baselineY;
    private float holdTimer;
    private bool baselineSet;

    public override void OnStepEnter(DialogueFlowController controller, DialogueFlowController.StepDefinition step)
    {
        // Pick HMD
        if (hmdTransform == null)
        {
            if (Camera.main != null) hmdTransform = Camera.main.transform;
        }

        if (hmdTransform == null)
        {
            Debug.LogWarning("[Step7_SquatHandler] No HMD transform found. Assign hmdTransform in Inspector.");
            baselineSet = false;
            return;
        }

        baselineY = hmdTransform.position.y;
        holdTimer = 0f;
        baselineSet = true;

        Debug.Log($"[Step7_SquatHandler] Baseline Y = {baselineY:F3}, required drop = {requiredDropMeters:F2}m");
    }

    public override void Tick(DialogueFlowController controller, DialogueFlowController.StepDefinition step)
    {
        if (!baselineSet || hmdTransform == null) return;

        float currentY = hmdTransform.position.y;
        float drop = baselineY - currentY;

        bool below = drop >= requiredDropMeters;

        if (below)
        {
            if (holdSeconds <= 0f)
            {
                controller.CompleteCurrentStep();
                return;
            }

            holdTimer += Time.deltaTime;
            if (holdTimer >= holdSeconds)
            {
                controller.CompleteCurrentStep();
            }
        }
        else
        {
            if (resetIfStandUp) holdTimer = 0f;
        }
    }

    public override void OnStepExit(DialogueFlowController controller, DialogueFlowController.StepDefinition step)
    {
        baselineSet = false;
        holdTimer = 0f;

        // Fire happy animation (no particles) after squatting completes
        if (girlHappyReaction != null)
            girlHappyReaction.TriggerHappyReaction(withParticles: false);
        else
            Debug.LogWarning("[Step7_SquatHandler] girlHappyReaction not assigned – skipping happy reaction.");
    }
}