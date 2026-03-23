using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class SimpleMicRecorder : MonoBehaviour
{
    [Header("XR / Input System (optional)")]
    public InputActionProperty recordToggleAction;

    [Header("Recording")]
    public int sampleRate = 44100;
    public int maxSeconds = 30;

    [Header("Test Playback")]
    public bool playBackAfterStop = true;
    public float playbackDelaySeconds = 30f;

    public AudioClip recordedClip { get; private set; }
    public bool IsRecording { get; private set; }

    private string deviceName;     // null = default mic
    private AudioSource audioSource;
    private Coroutine playbackRoutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // For easy testing: make it clearly audible regardless of listener position.
       audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D
        audioSource.volume = 1f;
        audioSource.mute = false;
    }

    void OnEnable()
    {
        if (recordToggleAction.action != null)
        {
            recordToggleAction.action.Enable();
            recordToggleAction.action.performed += OnRecordToggle;
        }
    }

    void OnDisable()
    {
        if (recordToggleAction.action != null)
        {
            recordToggleAction.action.performed -= OnRecordToggle;
            recordToggleAction.action.Disable();
        }
    }

    private static float GetPeak(AudioClip clip)
{
    if (clip == null) return 0f;
    float[] data = new float[Mathf.Min(clip.samples * clip.channels, 44100 * clip.channels)]; // up to ~1 sec
    clip.GetData(data, 0);
    float peak = 0f;
    for (int i = 0; i < data.Length; i++)
        peak = Mathf.Max(peak, Mathf.Abs(data[i]));
    return peak;
}

    void Start()
    {
        deviceName = null; // default system mic

        if (Microphone.devices.Length == 0)
            Debug.LogWarning("[Mic] No microphone devices found at Start().");
        else
            Debug.Log("[Mic] Default device: " + Microphone.devices[0]);
    }

    private void OnRecordToggle(InputAction.CallbackContext ctx)
    {
        if (!IsRecording) StartRecording();
        else StopRecording();
    }

    public void StartRecording()
    {
        if (IsRecording) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[Mic] No microphone devices found.");
            return;
        }

        // If a delayed playback was scheduled from a previous recording, cancel it.
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        recordedClip = Microphone.Start(deviceName, false, maxSeconds, sampleRate);
        IsRecording = true;

        Debug.Log("[Mic] Recording started.");
    }

    public void StopRecording()
    {
        if (!IsRecording) return;

        int position = Microphone.GetPosition(deviceName);
        Microphone.End(deviceName);
        IsRecording = false;

        if (recordedClip == null)
        {
            Debug.LogWarning("[Mic] Recording stopped but recordedClip is null.");
            return;
        }

        // If position is 0, you effectively captured nothing.
        if (position <= 0)
        {
            Debug.LogWarning("[Mic] Recording stopped but no samples were captured (position=0).");
            return;
        }

        // Trim the clip so playback doesn't include trailing silence up to maxSeconds.
        recordedClip = TrimClip(recordedClip, position);

        Debug.Log($"[Mic] Recording stopped. Trimmed samples={position}, length={recordedClip.length:0.00}s");
        Debug.Log("[Mic] Peak amplitude: " + GetPeak(recordedClip));

        if (playBackAfterStop)
        {
            playbackRoutine = StartCoroutine(PlayAfterDelay(playbackDelaySeconds));
        }

        
    }

    private IEnumerator PlayAfterDelay(float delay)
    {
        Debug.Log($"[Mic] Will play back in {delay:0.##}s...");
        yield return new WaitForSeconds(delay);

        if (recordedClip == null)
        {
            Debug.LogWarning("[Mic] No recorded clip to play.");
            playbackRoutine = null;
            yield break;
        }

        audioSource.Stop();
        audioSource.clip = recordedClip;
        audioSource.Play();

        Debug.Log("[Mic] Playback started.");
        playbackRoutine = null;
    }

    public void PlayNow()
    {
        if (recordedClip == null)
        {
            Debug.LogWarning("[Mic] No recorded clip to play.");
            return;
        }

        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        audioSource.Stop();
        audioSource.clip = recordedClip;
        audioSource.Play();
        Debug.Log("[Mic] Playback started (PlayNow).");
    }

    public void StopPlayback()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[Mic] Playback stopped.");
        }
    }

    private static AudioClip TrimClip(AudioClip original, int samplesToKeep)
    {
        // samplesToKeep is per-channel sample frames count returned by Microphone.GetPosition
        int channels = original.channels;

        float[] data = new float[samplesToKeep * channels];
        original.GetData(data, 0);

        AudioClip trimmed = AudioClip.Create(
            original.name + "_trimmed",
            samplesToKeep,
            channels,
            original.frequency,
            false
        );

        trimmed.SetData(data, 0);
        return trimmed;
    }
}