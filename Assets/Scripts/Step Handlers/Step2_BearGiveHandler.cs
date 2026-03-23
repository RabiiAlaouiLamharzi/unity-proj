using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Handles Step 2 (give the teddy bear to the girl).
///
/// Auto-grab logic (no button press required):
///   • Every frame checks the distance from Camera.main (the headset / user body)
///     to the bear's check-point.
///   • When the user is within <autoGrabDistance>, the bear is automatically
///     parented to <rightControllerTransform> (or to Camera.main if not set) with
///     a configurable local offset so it appears held in the right hand.
///   • TeddyTransferToGirl.overrideIsSelected is set so the girl can still
///     auto-take the bear even though XR grab is not used.
///
/// Give-to-girl:
///   • TeddyTransferToGirl already detects proximity to the girl hug-anchor and
///     parents the bear to her automatically.  Its onBearTaken callback is used to
///     advance the dialogue step.
/// </summary>
public class Step2_BearGiveHandler : DialogueStepHandler
{
    [Header("Bear")]
    [Tooltip("Root transform of the Teddy Bear GameObject.")]
    public Transform bearRoot;

    [Tooltip("Optional child checkpoint — used for the proximity check. Falls back to bearRoot.")]
    public Transform teddyCheckPoint;

    [Tooltip("XRGrabInteractable on the bear — disabled after auto-grab to prevent XR conflicts.")]
    public XRGrabInteractable bearGrab;

    [Tooltip("TeddyTransferToGirl script on the bear.")]
    public TeddyTransferToGirl teddyTransfer;

    [Header("Auto-Grab")]
    [Tooltip("Right hand controller Transform (from the XR rig). Bear is parented here when grabbed.\n" +
             "Leave empty to fall back to Camera.main.")]
    public Transform rightControllerTransform;

    [Tooltip("How close the user's head/body (Camera.main) must be to trigger the auto-grab.")]
    public float autoGrabDistance = 0.80f;

    [Tooltip("Local position offset applied to the bear after parenting to the controller/camera.")]
    public Vector3 attachLocalOffset = new Vector3(0f, -0.05f, 0.15f);

    [Tooltip("Local rotation offset applied to the bear after parenting.")]
    public Vector3 attachLocalRotEuler = Vector3.zero;

    [Header("Give-to-Girl")]
    [Tooltip("How close Camera.main must be to the girl's hug anchor to auto-give the bear.\n" +
             "Uses the user's head/body position — no need to aim the hand precisely.")]
    public float giveToGirlDistance = 1.0f;

    // -----------------------------------------------------------------------
    // Runtime state
    // -----------------------------------------------------------------------

    private DialogueFlowController _controller;
    private bool _stepActive;
    private bool _completed;
    private bool _bearGrabbed;

    // Saved once at Awake so we can restore the bear to its original place on re-entry
    private Transform _originalBearParent;
    private Vector3   _originalBearLocalPos;
    private Quaternion _originalBearLocalRot;

    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (bearRoot != null)
        {
            _originalBearParent   = bearRoot.parent;
            _originalBearLocalPos = bearRoot.localPosition;
            _originalBearLocalRot = bearRoot.localRotation;
        }
    }

    public override void OnStepEnter(DialogueFlowController controller,
                                     DialogueFlowController.StepDefinition step)
    {
        _controller  = controller;
        _stepActive  = true;
        _completed   = false;
        _bearGrabbed = false;

        // ---- Restore bear to its original position and state ----
        if (bearRoot != null)
        {
            bearRoot.gameObject.SetActive(true);
            bearRoot.SetParent(_originalBearParent, worldPositionStays: false);
            bearRoot.localPosition = _originalBearLocalPos;
            bearRoot.localRotation = _originalBearLocalRot;
        }

        // Reset TeddyTransferToGirl state (clears taken / takeCo / overrideIsSelected)
        if (teddyTransfer != null)
            teddyTransfer.ResetState();

        // Re-enable XR grab
        if (bearGrab != null) bearGrab.enabled = true;

        if (teddyTransfer != null)
            teddyTransfer.onBearTaken += HandleBearTaken;
        else
            Debug.LogWarning("[Step2_BearGiveHandler] teddyTransfer is not assigned — step won't auto-complete.");

        if (bearRoot == null)
            Debug.LogWarning("[Step2_BearGiveHandler] bearRoot is not assigned.");
    }

    public override void OnStepExit(DialogueFlowController controller,
                                    DialogueFlowController.StepDefinition step)
    {
        _stepActive = false;
        if (teddyTransfer != null)
            teddyTransfer.onBearTaken -= HandleBearTaken;
        _controller = null;
    }

    public override void Tick(DialogueFlowController controller,
                              DialogueFlowController.StepDefinition step)
    {
        if (!_stepActive || _completed) return;
        if (bearRoot == null || Camera.main == null) return;

        if (!_bearGrabbed)
        {
            // ---- Phase 1: auto-grab when user is near the bear ----

            // Skip if the player already grabbed it manually via XR
            if (bearGrab != null && bearGrab.isSelected)
            {
                _bearGrabbed = true;
                if (teddyTransfer != null) teddyTransfer.overrideIsSelected = true;
                return;
            }

            Vector3 bearPos = teddyCheckPoint != null ? teddyCheckPoint.position : bearRoot.position;
            float dist = Vector3.Distance(Camera.main.transform.position, bearPos);

            if (dist <= autoGrabDistance)
                AutoGrabBear();
        }
        else
        {
            // ---- Phase 2: auto-give to girl when user is near her ----
            if (teddyTransfer == null) return;

            Transform anchor = teddyTransfer.girlHugAnchor;
            if (anchor == null) return;

            float distToGirl = Vector3.Distance(Camera.main.transform.position, anchor.position);
            if (distToGirl <= giveToGirlDistance)
            {
                Debug.Log($"[Step2_BearGiveHandler] User close to girl ({distToGirl:F2}m) — forcing bear transfer.");
                teddyTransfer.ForceTake();
            }
        }
    }

    // -----------------------------------------------------------------------

    private void AutoGrabBear()
    {
        _bearGrabbed = true;

        Debug.Log("[Step2_BearGiveHandler] Auto-grabbing bear.");

        // Disable XR grab so it doesn't fight the manual parenting
        if (bearGrab != null) bearGrab.enabled = false;

        // Freeze physics
        var rb = bearRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
        }

        // Parent to right controller; fall back to camera if not wired
        Transform attachTarget = rightControllerTransform != null
            ? rightControllerTransform
            : Camera.main.transform;

        bearRoot.SetParent(attachTarget, worldPositionStays: false);
        bearRoot.localPosition = attachLocalOffset;
        bearRoot.localRotation = Quaternion.Euler(attachLocalRotEuler);

        // Tell TeddyTransferToGirl it's "held" even though XR grab isn't active
        if (teddyTransfer != null)
            teddyTransfer.overrideIsSelected = true;
    }

    private void HandleBearTaken()
    {
        if (_completed) return;
        _completed  = true;
        _stepActive = false;

        Debug.Log("[Step2_BearGiveHandler] Bear given to girl — completing step.");
        _controller?.CompleteCurrentStep();
    }

    // -----------------------------------------------------------------------
    // Scene-view gizmo: green sphere = auto-grab radius around bear check-point
    // -----------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Vector3 bearPos = teddyCheckPoint != null
            ? teddyCheckPoint.position
            : (bearRoot != null ? bearRoot.position : transform.position);

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(bearPos, autoGrabDistance);
    }
}
