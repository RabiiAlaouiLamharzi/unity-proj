using UnityEngine;
using Unity.XR.CoreUtils;
using System.Collections;

public class SwapPOV : MonoBehaviour
{
    public XROrigin xrOrigin;
    public Transform childPOVAnchor;
    public bool matchRotation = true;

    [Header("Cinematic Animator")]
    public Animator cinematicAnimator;          // drag the child's Animator here
    public string finalStateName = "handsalongside"; // exact name in Animator

  
    [Header("Disable Adult Control")]
    public Behaviour adultIKFollowScript;
    public GameObject adultRigBuilder;

    [Header("Enable Child Control")]
    public Behaviour childIKFollowScript;
    public GameObject childRigBuilder;

    private bool swapped = false;

    public void SwapToChildPOV()
    {
        if (swapped) return;

        StartCoroutine(SwapWhenAnimationFinished());
    }

    private IEnumerator SwapWhenAnimationFinished()
    {
        if (!cinematicAnimator)
        {
            Debug.LogError("Missing cinematicAnimator reference.");
            yield break;
        }

        // 🔹 Wait until we are in the final state
        while (!cinematicAnimator.GetCurrentAnimatorStateInfo(0).IsName(finalStateName))
        {
            yield return null;
        }

        // 🔹 Wait until animation has completed
        while (cinematicAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        // 🔹 Stop cinematic animator so it stops overriding pose
        cinematicAnimator.enabled = false;

        // 🔹 Restore IK weights
      

        // 🔹 Move XR camera
        xrOrigin.MoveCameraToWorldLocation(childPOVAnchor.position);

        if (matchRotation)
        {
            var cam = xrOrigin.Camera;
            float deltaYaw = Mathf.DeltaAngle(cam.transform.eulerAngles.y, childPOVAnchor.eulerAngles.y);
            xrOrigin.transform.RotateAround(cam.transform.position, Vector3.up, deltaYaw);
        }

        // 🔹 Disable adult control
        if (adultIKFollowScript) adultIKFollowScript.enabled = false;
        if (adultRigBuilder) adultRigBuilder.SetActive(false);

        // 🔹 Enable child control
        if (childIKFollowScript) childIKFollowScript.enabled = true;
        if (childRigBuilder) childRigBuilder.SetActive(true);

        

        swapped = true;

        Debug.Log("[SwapPOV] Swapped AFTER animation finished.");
    }
}