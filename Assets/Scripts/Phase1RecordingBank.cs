using System.Collections.Generic;
using UnityEngine;

public class Phase1RecordingBank : MonoBehaviour
{
    private readonly Dictionary<int, AudioClip> recordings = new Dictionary<int, AudioClip>();

    public void SaveRecording(int stepId, AudioClip clip)
    {
        if (clip == null) return;
        recordings[stepId] = clip;
        Debug.Log($"[RecordingBank] Saved recording for step {stepId}, length={clip.length:F2}s");
    }

    public AudioClip GetRecording(int stepId)
    {
        recordings.TryGetValue(stepId, out var clip);
        return clip;
    }
}