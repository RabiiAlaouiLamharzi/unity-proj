using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TeddyTransferToGirl : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Teddy's XRGrabInteractable (usually on the teddy root).")]
    public XRGrabInteractable grab;

    [Tooltip("Girl root transform (VRChild). Not used for distance anymore, but kept for clarity.")]
    public Transform girlRoot;

    [Tooltip("Girl Animator (on VRChild).")]
    public Animator girlAnimator;

    [Tooltip("Where the teddy should snap to on the girl (chest/hug point). This is ALSO used for distance check.")]
    public Transform girlHugAnchor;

    [Tooltip("Optional: a checkpoint on the teddy (child transform) placed at the 'center' of where it should be measured from. If null, uses teddy transform.")]
    public Transform teddyCheckPoint;

    [Header("Settings")]
    [Tooltip("How close the teddy checkpoint must be to the girl's hug anchor to trigger the take.")]
    public float takeDistance = 0.35f;

    [Tooltip("Seconds to wait after triggering reach animation before snapping teddy.")]
    public float reachAnimDelay = 0.15f;

    [Tooltip("Animator trigger name on the girl's controller.")]
    public string girlReachTrigger = "Reach";

    [Tooltip("Disable grab after the girl takes it.")]
    public bool disableGrabAfterTaken = true;

    [Tooltip("If true, we freeze the rigidbody (recommended) when snapping.")]
    public bool makeKinematicWhenTaken = true;

    [Header("Hug Sequence")]
    [Tooltip("Animator trigger name for the hug animation.")]
    public string hugTriggerName = "PlayHug";

    [Tooltip("Animator state to cross-fade into after the hug ends (must match the state name exactly).")]
    public string postHugStateName = "IdleStanding";

    [Tooltip("How many seconds to hold the hug animation before fading back to idle.")]
    public float hugDuration = 3f;

    [Tooltip("Cross-fade duration (seconds) from Hug back to idle.")]
    public float hugFadeOut = 0.4f;

    /// <summary>Fired once when the girl takes the teddy. Subscribe from step handlers.</summary>
    public System.Action onBearTaken;

    /// <summary>
    /// Set true by Step2_BearGiveHandler after the bear is auto-attached to a controller.
    /// Bypasses the XR grab isSelected guard so the girl can still take it.
    /// </summary>
    [HideInInspector] public bool overrideIsSelected = false;

    private bool taken;
    private Coroutine takeCo;

    /// <summary>
    /// Resets all runtime state so the step can be re-entered cleanly.
    /// Called by Step2_BearGiveHandler.OnStepEnter().
    /// </summary>
    public void ResetState()
    {
        if (takeCo != null)
        {
            StopCoroutine(takeCo);
            takeCo = null;
        }
        taken              = false;
        overrideIsSelected = false;
    }

    void Reset()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    void Awake()
    {
        if (!grab) grab = GetComponent<XRGrabInteractable>();
    }

    void Update()
    {
        if (taken) return;
        if (!girlHugAnchor || !girlAnimator || !grab) return;

        // Only allow "take" if player is currently holding it (or override flag is set)
        if (!grab.isSelected && !overrideIsSelected) return;

        // Measure distance from teddy check-point to the girl's hug anchor
        Vector3 teddyPos = teddyCheckPoint ? teddyCheckPoint.position : transform.position;
        float d = Vector3.Distance(teddyPos, girlHugAnchor.position);

        if (d <= takeDistance && takeCo == null)
        {
            takeCo = StartCoroutine(TakeRoutine());
        }
    }

    /// <summary>
    /// Programmatically trigger the transfer regardless of distance or selection state.
    /// Called by Step2_BearGiveHandler when the user's body is close to the girl.
    /// </summary>
    public void ForceTake()
    {
        if (taken || takeCo != null) return;
        takeCo = StartCoroutine(TakeRoutine());
    }

    IEnumerator TakeRoutine()
    {
        taken = true;

        // ---- 1. Fire reach trigger so the girl reaches for the bear ----
        if (!string.IsNullOrEmpty(girlReachTrigger))
        {
            bool hasTrigger = false;
            foreach (var p in girlAnimator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == girlReachTrigger)
                { hasTrigger = true; break; }
            }
            if (hasTrigger)
            {
                girlAnimator.ResetTrigger(girlReachTrigger);
                girlAnimator.SetTrigger(girlReachTrigger);
            }
        }

        if (reachAnimDelay > 0f)
            yield return new WaitForSeconds(reachAnimDelay);

        // ---- 2. Force-release from XR interactor ----
        if (grab != null && grab.isSelected &&
            grab.interactorsSelecting != null && grab.interactorsSelecting.Count > 0)
        {
            var interactor = grab.interactorsSelecting[0];
            if (grab.interactionManager)
                grab.interactionManager.SelectExit(interactor, grab);
        }

        // ---- 3. Freeze physics ----
        var rb = GetComponent<Rigidbody>();
        if (rb && makeKinematicWhenTaken)
        {
            rb.velocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
        }

        // ---- 4. Snap bear to girl's hug anchor ----
        transform.SetParent(girlHugAnchor, worldPositionStays: false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (disableGrabAfterTaken && grab != null)
            grab.enabled = false;

        Debug.Log("[TeddyTransferToGirl] Bear snapped to girl hug anchor.");

        // ---- 5. Play Hug animation ----
        bool hasHugTrigger = false;
        if (!string.IsNullOrEmpty(hugTriggerName))
        {
            foreach (var p in girlAnimator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == hugTriggerName)
                { hasHugTrigger = true; break; }
            }
        }

        if (hasHugTrigger)
        {
            girlAnimator.ResetTrigger(hugTriggerName);
            girlAnimator.SetTrigger(hugTriggerName);
            Debug.Log("[TeddyTransferToGirl] Hug animation triggered.");
        }

        // ---- 6. Hold for hugDuration seconds ----
        yield return new WaitForSeconds(hugDuration);

        // ---- 7. Cross-fade back to idle ----
        if (!string.IsNullOrEmpty(postHugStateName))
            girlAnimator.CrossFade(postHugStateName, hugFadeOut);

        // ---- 8. Notify listeners BEFORE deactivating — SetActive(false) kills
        //         the coroutine so anything after it would never run. ----
        takeCo = null;
        onBearTaken?.Invoke();
        Debug.Log("[TeddyTransferToGirl] Hug done — bear hidden, returning to idle.");

        // ---- 9. Hide the bear last so the coroutine is already finished ----
        gameObject.SetActive(false);
    }

    // Optional: draw the distance sphere in Scene view to tune takeDistance
    void OnDrawGizmosSelected()
    {
        if (!girlHugAnchor) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(girlHugAnchor.position, takeDistance);

        if (teddyCheckPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(teddyCheckPoint.position, 0.02f);
        }
    }
}