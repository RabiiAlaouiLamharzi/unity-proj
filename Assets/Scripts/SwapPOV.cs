using UnityEngine;
using Unity.XR.CoreUtils;
using System.Collections;
using UnityEngine.Animations.Rigging;


public class SwapPOV : MonoBehaviour
{
    [Header("XR")]
    public XROrigin xrOrigin;

    public Transform interactionSetup;
    public Transform childPOVAnchor;
    public bool matchRotation = true;

    [Header("Child Cinematic (Animator driving the story)")]
    public Animator childCinematicAnimator;
    [Tooltip("Animator STATE name (not clip name) for the final state you want to wait for.")]
    public string finalStateName = "handsalongside";
    [Tooltip("Animator STATE name to park in after the cinematic ends (non-looping idle/done).")]
    public string parkStateName = "IDLEDONE";
    public float waitTimeoutSeconds = 8f;

    [Header("Adult Possession (keep adult visible!)")]
    public Behaviour adultIKFollowScript;  // adult IK Target Follow VR Rig script
    public Rig[] adultUpperRigs;           // rigs controlling adult arms/head (weight -> 0 on swap)
    public Rig[] adultLegRigs;             // optional: adult leg rigs (weight -> 0 on swap)

    [Header("Child Possession")]
    public Behaviour childIKFollowScript;  // child IK Target Follow VR Rig script
    public Rig[] childUpperRigs;           // rigs controlling child arms/head (weight -> 1 on swap)
    public Rig[] childLegRigs;             // optional: child leg rigs (weight -> 1 after delay)

    [Header("Leg IK timing")]
    [Tooltip("If legs jitter during swap, enable legs a bit later than arms/head.")]
    public bool enableLegsWithDelay = true;
    public float legEnableDelaySeconds = 0.15f;

    [Header("Debug")]
    public bool verboseLogs = true;

    private bool swapped = false;

    // Hook this to your SWAP button
    public void SwapToChildPOV()
    {
        if (swapped) return;

        if (!xrOrigin || !xrOrigin.Camera || !childPOVAnchor)
        {
            Debug.LogError("[SwapPOV] Missing xrOrigin/camera/childPOVAnchor.");
            return;
        }

        if (verboseLogs) Debug.Log("[SwapPOV] Swap pressed");

        // 1) Move POV immediately (XR-safe)
        MoveXROriginSoCameraHitsAnchor(childPOVAnchor);

        // 2) Start controlled handover (stop fights between Animator + Rigging)
        StartCoroutine(HandoverCoroutine());
    }

    private void MoveXROriginSoCameraHitsAnchor(Transform anchor)
    {
        // XR-safe move: translate XR Origin by the delta between current camera position and desired camera position
        Vector3 delta = anchor.position - xrOrigin.Camera.transform.position;
        xrOrigin.transform.position += delta;

        if (matchRotation)
        {
            float deltaYaw = Mathf.DeltaAngle(
                xrOrigin.Camera.transform.eulerAngles.y,
                anchor.eulerAngles.y
            );

            // rotate origin around camera so camera stays pinned
            xrOrigin.transform.RotateAround(
                xrOrigin.Camera.transform.position,
                Vector3.up,
                deltaYaw
            );


            interactionSetup.transform.position =
    new Vector3(
        interactionSetup.transform.position.x,
        0.25f,
        interactionSetup.transform.position.z
    );

        }

        if (verboseLogs) Debug.Log("[SwapPOV] XR Origin moved to child POV anchor.");
    }

    private IEnumerator HandoverCoroutine()
    {
        // ---- A) Prevent immediate double-control: disable BOTH follow scripts for a moment ----
        // (optional but helps avoid 1–2 frames of tug-of-war if both were enabled)
        if (adultIKFollowScript) adultIKFollowScript.enabled = false;
        if (childIKFollowScript) childIKFollowScript.enabled = false;

        // ---- B) Ensure rigs are in a known state before switching ----
        SetRigWeights(adultUpperRigs, 0f);
        SetRigWeights(adultLegRigs, 0f);
        SetRigWeights(childUpperRigs, 0f);
        SetRigWeights(childLegRigs, 0f);

        // ---- C) Wait for final cinematic animation to finish (or timeout) ----
        if (childCinematicAnimator)
        {
            // Make sure cinematic is running while we wait
            childCinematicAnimator.speed = 1f;

            // Wait until we enter final state
            float t = 0f;
            while (t < waitTimeoutSeconds)
            {
                var st = childCinematicAnimator.GetCurrentAnimatorStateInfo(0);
                if (IsState(st, finalStateName)) break;

                t += Time.deltaTime;
                yield return null;
            }

            // Wait until it played once (normalizedTime >= 1)
            t = 0f;
            while (t < waitTimeoutSeconds)
            {
                var st = childCinematicAnimator.GetCurrentAnimatorStateInfo(0);

                if (IsState(st, finalStateName) && st.normalizedTime >= 0.98f)
                    break;

                t += Time.deltaTime;
                yield return null;
            }

            // Park in a neutral state so it doesn’t keep changing pose / looping transitions
            if (!string.IsNullOrEmpty(parkStateName))
            {
                childCinematicAnimator.Play(parkStateName, 0, 0f);
                childCinematicAnimator.Update(0f);
            }

            // Freeze Animator so it stops fighting rigging
            childCinematicAnimator.applyRootMotion = false;
            childCinematicAnimator.speed = 0f;

            if (verboseLogs) Debug.Log("[SwapPOV] Child cinematic parked and frozen.");
        }
        else
        {
            if (verboseLogs) Debug.LogWarning("[SwapPOV] No childCinematicAnimator assigned; skipping wait.");
        }

        // ---- D) Handover: adult OFF (possession), child ON ----
        // Adult: keep visible, just stop following VR targets
        SetRigWeights(adultUpperRigs, 0f);
        SetRigWeights(adultLegRigs, 0f);
        if (adultIKFollowScript) adultIKFollowScript.enabled = false;

        // Child: enable upper body first (usually stabilizes instantly)
        SetRigWeights(childUpperRigs, 1f);
        if (childIKFollowScript) childIKFollowScript.enabled = true;

        // Legs: optional delayed enable to avoid “legs opening/closing” during the exact handoff frame
        if (enableLegsWithDelay && legEnableDelaySeconds > 0f)
            yield return new WaitForSeconds(legEnableDelaySeconds);

        SetRigWeights(childLegRigs, 1f);

        swapped = true;
        if (verboseLogs) Debug.Log("[SwapPOV] Handover complete: adult unpossessed, child possessed.");
    }

    private bool IsState(AnimatorStateInfo st, string name)
    {
        return st.IsName(name) || st.IsName("Base Layer." + name);
    }

    private void SetRigWeights(Rig[] rigs, float w)
    {
        if (rigs == null) return;
        for (int i = 0; i < rigs.Length; i++)
        {
            if (rigs[i]) rigs[i].weight = w;
        }
    }
}