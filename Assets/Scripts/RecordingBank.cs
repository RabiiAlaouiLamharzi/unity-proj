using System.Collections.Generic;
using UnityEngine;

public class RecordingBank : MonoBehaviour
{
    [SerializeField] private VoiceRecorderMVP recorder;

    private readonly List<AudioClip> clips = new List<AudioClip>();

    private void Awake()
    {
        if (recorder == null)
            recorder = GetComponent<VoiceRecorderMVP>();
    }

    private void OnEnable()
    {
        if (recorder != null)
            recorder.OnRecordingStoppedWithClip += HandleRecorded;
    }

    private void OnDisable()
    {
        if (recorder != null)
            recorder.OnRecordingStoppedWithClip -= HandleRecorded;
    }

    private void HandleRecorded(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[RecordingBank] Received null clip (recording invalid).");
            return;
        }

        clips.Add(clip);
        Debug.Log($"[RecordingBank] Stored recording #{clips.Count - 1} (len={clip.length:F2}s).");
    }

    public int Count => clips.Count;

    public AudioClip Get(int index)
    {
        if (index < 0 || index >= clips.Count) return null;
        return clips[index];
    }

    public void Clear()
    {
        clips.Clear();
        Debug.Log("[RecordingBank] Cleared recordings.");
    }
}