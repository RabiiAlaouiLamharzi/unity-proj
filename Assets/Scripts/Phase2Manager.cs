using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

public class Phase2Manager : MonoBehaviour
{
    [Header("Phase 1 -> Phase 2 Trigger")]
    public DialogueFlowController phase1;
    public int phase1EndStepId = 8;

    [Header("Dialogue Systems")]
    public GameObject dialogueSystemPhase1;
    public GameObject dialogueSystemPhase2;

    [Header("Success Celebration (before POV swap)")]
    [Tooltip("GirlHappyReaction on DialogueSystem — triggers happy animation + particles before the POV swap.")]
    public GirlHappyReaction girlHappyReaction;

    [Tooltip("Success sound played at the moment the celebration starts (Success.wav).")]
    public AudioClip successClip;

    [Tooltip("Volume for the success sound (0–1).")]
    [Range(0f, 1f)]
    public float successVolume = 1f;

    [Tooltip("Seconds to wait after triggering the happy animation before the POV swap begins.")]
    public float celebrationDuration = 3f;

    [Header("XR")]
    public XROrigin xrOrigin;
    public Transform childViewpointAnchor;

    [Header("Freeze movement after swap (recommended)")]
    [Tooltip("Disable these locomotion providers after teleport so the rig doesn't move.")]
    public LocomotionProvider[] locomotionToDisable;

    [Header("Optional: also disable CharacterController movement collisions drift")]
    public CharacterController characterController;

    [Header("Optional scripts to disable (no embodiment)")]
    public Behaviour adultFollow;
    public Behaviour childFollow;
    public Animator childAnimator;

    [Header("Options")]
    public bool matchYawRotation = true;

    private bool phase2Started = false;

    private void OnEnable()
    {
        if (phase1 != null)
            phase1.OnStepCompleted += HandleStepCompleted;
    }

    private void OnDisable()
    {
        if (phase1 != null)
            phase1.OnStepCompleted -= HandleStepCompleted;
    }

    private void HandleStepCompleted(int stepId)
    {
        if (phase2Started) return;
        if (stepId != phase1EndStepId) return;

        phase2Started = true;

        if (phase1 != null)
            phase1.OnStepCompleted -= HandleStepCompleted;

        StartCoroutine(BeginPhase2Routine());
    }

    private IEnumerator BeginPhase2Routine()
    {
        yield return null;

        // --- Success celebration: happy animation + particles + sound ---
        if (successClip != null)
            AudioSource.PlayClipAtPoint(successClip, transform.position, successVolume);

        if (girlHappyReaction != null)
            girlHappyReaction.TriggerHappyReaction(withParticles: true);

        if (girlHappyReaction != null || successClip != null)
            yield return new WaitForSeconds(celebrationDuration);
        // -----------------------------------------------------------------

        if (adultFollow != null) adultFollow.enabled = false;
        if (childFollow != null) childFollow.enabled = false;
        if (childAnimator != null) childAnimator.enabled = false;


        yield return new WaitForEndOfFrame();


        MoveCameraExactlyToAnchor(childViewpointAnchor);

        if (phase1 != null)
        {
            yield return StartCoroutine(phase1.PlayAllRecordedVoicesInOrderCoroutine());
        }

        DisableLocomotion();

        if (phase1 != null)
        {
            phase1.enabled = false;
        }


        if (dialogueSystemPhase1 != null) dialogueSystemPhase1.SetActive(false);
        if (dialogueSystemPhase2 != null) dialogueSystemPhase2.SetActive(true);




        var phase2Flow = dialogueSystemPhase2.GetComponent<DialogueFlowController>();
        if (phase2Flow != null) phase2Flow.StartFlow();
    }

    private void MoveCameraExactlyToAnchor(Transform anchor)
    {
        if (xrOrigin == null || xrOrigin.Camera == null || anchor == null)
        {
            Debug.LogWarning("[Phase2Manager] Missing xrOrigin/camera/anchor.");
            return;
        }


        if (matchYawRotation)
        {
            float currentYaw = xrOrigin.Camera.transform.eulerAngles.y;
            float targetYaw = anchor.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(currentYaw, targetYaw);


            xrOrigin.transform.RotateAround(xrOrigin.Camera.transform.position, Vector3.up, deltaYaw);
        }


        xrOrigin.MoveCameraToWorldLocation(anchor.position);
    }

    private void DisableLocomotion()
    {

        if (locomotionToDisable != null)
        {
            foreach (var p in locomotionToDisable)
                if (p != null) p.enabled = false;
        }


        if (characterController != null)
            characterController.enabled = false;
    }
}