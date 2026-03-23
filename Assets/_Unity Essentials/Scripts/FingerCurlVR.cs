using UnityEngine;
using UnityEngine.InputSystem;

public class FingerCurlVR : MonoBehaviour
{
    [Header("XR Input (float 0..1)")]
    public InputActionProperty gripAction; 

    
    public Transform thumb1, thumb2, thumb3;
    public Transform index1, index2, index3;
    public Transform middle1, middle2, middle3;
    public Transform ring1, ring2, ring3;
    public Transform pinky1, pinky2, pinky3;

    [Header("Curl amounts (degrees)")]
    public float thumbCurl = 50f;
    public float fingerCurl = 80f;

    [Header("Which axis curls the finger?")]
    public Vector3 curlAxisLocal = Vector3.right; // try Vector3.right or Vector3.forward depending on rig

    [Header("Smoothing")]
    [Range(0f, 30f)] public float smooth = 15f;

    Quaternion t1Open, t2Open, t3Open;
    Quaternion i1Open, i2Open, i3Open;
    Quaternion m1Open, m2Open, m3Open;
    Quaternion r1Open, r2Open, r3Open;
    Quaternion p1Open, p2Open, p3Open;

    float current;

    void Awake()
    {
        // Store the current pose as the OPEN pose
        t1Open = LocalRot(thumb1); t2Open = LocalRot(thumb2); t3Open = LocalRot(thumb3);
        i1Open = LocalRot(index1); i2Open = LocalRot(index2); i3Open = LocalRot(index3);
        m1Open = LocalRot(middle1); m2Open = LocalRot(middle2); m3Open = LocalRot(middle3);
        r1Open = LocalRot(ring1); r2Open = LocalRot(ring2); r3Open = LocalRot(ring3);
        p1Open = LocalRot(pinky1); p2Open = LocalRot(pinky2); p3Open = LocalRot(pinky3);
    }

    void OnEnable()  { gripAction.action?.Enable(); }
    void OnDisable() { gripAction.action?.Disable(); }

    void LateUpdate()
    {
        float target = gripAction.action != null ? gripAction.action.ReadValue<float>() : 0f;
        current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        ApplyFinger(thumb1, t1Open, thumbCurl * 0.7f);
        ApplyFinger(thumb2, t2Open, thumbCurl);
        ApplyFinger(thumb3, t3Open, thumbCurl);

        ApplyFinger(index1, i1Open, fingerCurl * 0.7f);
        ApplyFinger(index2, i2Open, fingerCurl);
        ApplyFinger(index3, i3Open, fingerCurl * 0.7f);

        ApplyFinger(middle1, m1Open, fingerCurl * 0.7f);
        ApplyFinger(middle2, m2Open, fingerCurl);
        ApplyFinger(middle3, m3Open, fingerCurl * 0.7f);

        ApplyFinger(ring1, r1Open, fingerCurl * 0.7f);
        ApplyFinger(ring2, r2Open, fingerCurl);
        ApplyFinger(ring3, r3Open, fingerCurl * 0.7f);

        ApplyFinger(pinky1, p1Open, fingerCurl * 0.7f);
        ApplyFinger(pinky2, p2Open, fingerCurl);
        ApplyFinger(pinky3, p3Open, fingerCurl * 0.7f);
    }

    void ApplyFinger(Transform bone, Quaternion openRot, float degrees)
    {
        if (!bone) return;
        Quaternion closedOffset = Quaternion.AngleAxis(degrees, curlAxisLocal.normalized);
        Quaternion closedRot = openRot * closedOffset;
        bone.localRotation = Quaternion.Slerp(openRot, closedRot, current);
    }

    static Quaternion LocalRot(Transform t) => t ? t.localRotation : Quaternion.identity;
}