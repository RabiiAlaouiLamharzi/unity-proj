using UnityEngine;
using System.Collections;

public class ResetToStartPose : MonoBehaviour
{
    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 startScale;

    private Rigidbody rb;

    void Awake()
    {
        // Store exact original transform when scene starts
        startPos = transform.position;
        startRot = transform.rotation;
        startScale = transform.localScale;

        rb = GetComponent<Rigidbody>();
    }

    public void ResetNow()
    {
        if (rb)
        {
            // Stop physics motion
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Temporarily disable physics to prevent collision push
            rb.isKinematic = true;
        }

        // Hard reset transform
        transform.position = startPos;
        transform.rotation = startRot;
        transform.localScale = startScale;

        // Re-enable physics next frame
        if (rb)
            StartCoroutine(ReenablePhysics());
    }

    IEnumerator ReenablePhysics()
    {
        yield return null;
        rb.isKinematic = false;
    }
}