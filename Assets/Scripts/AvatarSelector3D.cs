using UnityEngine;
using TMPro;

public class AvatarSelector3D : MonoBehaviour
{
    [Header("Preview Prefabs (size = 2)")]
    public GameObject[] avatarPreviewPrefabs;

    [Header("Where the preview instance is spawned")]
    public Transform spawnPoint;

    [Header("Optional UI")]
    public TMP_Text avatarNameText;
    public string[] avatarNames;

    [Header("Preview rotation")]
    public float rotationSpeed = 25f;
    public bool autoRotate = true;

    public int SelectedIndex { get; private set; } = 0;

    GameObject currentInstance;

    void Start() => Show(SelectedIndex);

    void Update()
    {
        if (autoRotate && currentInstance != null)
            spawnPoint.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }

    public void Next()
    {
        if (avatarPreviewPrefabs == null || avatarPreviewPrefabs.Length == 0) return;
        SelectedIndex = (SelectedIndex + 1) % avatarPreviewPrefabs.Length;
        Show(SelectedIndex);
    }

    public void Prev()
    {
        if (avatarPreviewPrefabs == null || avatarPreviewPrefabs.Length == 0) return;
        SelectedIndex = (SelectedIndex - 1 + avatarPreviewPrefabs.Length) % avatarPreviewPrefabs.Length;
        Show(SelectedIndex);
    }

    void Show(int index)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("[AvatarSelector3D] spawnPoint is not assigned.");
            return;
        }

        if (currentInstance != null)
            Destroy(currentInstance);

        var prefab = avatarPreviewPrefabs[index];
        currentInstance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);

        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;

        if (avatarNameText != null)
        {
            if (avatarNames != null && avatarNames.Length == avatarPreviewPrefabs.Length)
                avatarNameText.text = avatarNames[index];
            else
                avatarNameText.text = $"Avatar {index + 1}";
        }
    }
}