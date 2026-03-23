using UnityEngine;
using TMPro;

public class GirlDialogueBubble : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text bubbleText;
    public Transform anchor;      // where the bubble should stick to (girl head / chest)
    public Camera targetCamera;   // usually main camera

    [Header("Behavior")]
    public Vector3 offset = new Vector3(0, 0.15f, 0);
    public bool faceCamera = true;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (bubbleText == null) bubbleText = GetComponentInChildren<TMP_Text>(true);
    }

    void LateUpdate()
    {
        if (anchor != null)
            transform.position = anchor.position + offset;

        if (faceCamera && targetCamera != null)
        {
            // billboard: face camera
            Vector3 forward = transform.position - targetCamera.transform.position;
            forward.y = 0f; // optional: keep it upright
            if (forward.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(forward);
        }
    }

    public void SetText(string text)
    {
        if (bubbleText != null) bubbleText.text = text;
    }

    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }
} 