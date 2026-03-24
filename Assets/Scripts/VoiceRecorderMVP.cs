using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class VoiceRecorderMVP : MonoBehaviour
{
    public enum MicSelectMode { AutoFirst, ByIndex, ByExactName }

    [Header("Input")]
    public InputActionReference recordToggleAction;

    [Header("Enable / Disable by Flow")]
    public bool recordingEnabled = true;

    [Header("Microphone Selection")]
    public MicSelectMode micSelectMode = MicSelectMode.AutoFirst;
    public int preferredMicIndex = 0;
    public string preferredMicName = "";
    public bool logDeviceListOnStart = true;

    [Header("Recording Params")]
    public int maxRecordSeconds = 10;
    public int frequency = 44100;

    [Header("Debug")]
    public bool playBackImmediately = false;

    public event Action OnRecordingStarted;
    public event Action<AudioClip> OnRecordingStoppedWithClip;

    private AudioSource playbackSource;
    private string micDevice;
    private bool isRecording = false;
    private AudioClip recordingClip;
    private int actualRecordingFrequency;

    void Awake()
    {
        playbackSource = GetComponent<AudioSource>();
        playbackSource.playOnAwake = false;
        playbackSource.pitch = 1f;
        playbackSource.spatialBlend = 0f;
        playbackSource.dopplerLevel = 0f;
    }

    void OnEnable()
    {
        if (recordToggleAction != null)
        {
            recordToggleAction.action.performed += OnRecordTogglePerformed;
            recordToggleAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (recordToggleAction != null)
        {
            recordToggleAction.action.performed -= OnRecordTogglePerformed;
            recordToggleAction.action.Disable();
        }
    }

    void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[VoiceRecorder] No microphone devices found.");
            return;
        }

        if (logDeviceListOnStart)
            LogMicrophoneDevices();

        micDevice = SelectMicrophone();
        Debug.Log($"[VoiceRecorder] Selected microphone: {micDevice}");

        int minFreq, maxFreq;
        Microphone.GetDeviceCaps(micDevice, out minFreq, out maxFreq);
        Debug.Log($"[VoiceRecorder] Device caps: min={minFreq}, max={maxFreq}");
    }

    private void LogMicrophoneDevices()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[VoiceRecorder] Available microphones:");
        for (int i = 0; i < Microphone.devices.Length; i++)
            sb.AppendLine($"  Mic[{i}] = {Microphone.devices[i]}");
        Debug.Log(sb.ToString());
    }

    private string SelectMicrophone()
    {
        switch (micSelectMode)
        {
            case MicSelectMode.ByIndex:
                preferredMicIndex = Mathf.Clamp(preferredMicIndex, 0, Microphone.devices.Length - 1);
                return Microphone.devices[preferredMicIndex];

            case MicSelectMode.ByExactName:
                if (!string.IsNullOrWhiteSpace(preferredMicName))
                {
                    foreach (var d in Microphone.devices)
                        if (d == preferredMicName) return d;

                    Debug.LogWarning($"[VoiceRecorder] Mic name not found: '{preferredMicName}'. Falling back to Mic[0].");
                }
                return Microphone.devices[0];

            default:
                return Microphone.devices[0];
        }
    }

    private int GetSafeFrequency(string device, int requested)
    {
        int minFreq, maxFreq;
        Microphone.GetDeviceCaps(device, out minFreq, out maxFreq);

        if (minFreq == 0 && maxFreq == 0)
            return requested;

        if (requested < minFreq) return minFreq;
        if (requested > maxFreq) return maxFreq;
        return requested;
    }

    private void OnRecordTogglePerformed(InputAction.CallbackContext ctx)
    {
        if (!recordingEnabled) return;
        if (Microphone.devices.Length == 0) return;

        Debug.Log($"[VoiceRecorder] Toggle fired by: {ctx.control.path}");

        if (!isRecording) StartRecording();
        else StopRecording();
    }

    private void StartRecording()
    {
        if (string.IsNullOrEmpty(micDevice))
        {
            Debug.LogError("[VoiceRecorder] Microphone not selected.");
            return;
        }

        if (playbackSource.isPlaying)
            playbackSource.Stop();

        actualRecordingFrequency = GetSafeFrequency(micDevice, frequency);
        Debug.Log($"[VoiceRecorder] StartRecording: device={micDevice}, requestedFreq={frequency}, actualFreq={actualRecordingFrequency}");

        recordingClip = Microphone.Start(micDevice, false, maxRecordSeconds, actualRecordingFrequency);

        if (recordingClip == null)
        {
            Debug.LogError("[VoiceRecorder] Microphone.Start returned null.");
            return;
        }

        StartCoroutine(WaitForMicStart());
    }

    private IEnumerator WaitForMicStart()
    {
        float timeout = 1f;
        float t = 0f;

        while (Microphone.GetPosition(micDevice) <= 0 && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        int pos = Microphone.GetPosition(micDevice);
        Debug.Log($"[VoiceRecorder] Mic warmup done. startPos={pos}");

        isRecording = true;
        OnRecordingStarted?.Invoke();
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        int endPos = Microphone.GetPosition(micDevice);
        Debug.Log($"[VoiceRecorder] StopRecording: endPos={endPos}, isMicRecording={Microphone.IsRecording(micDevice)}");

        Microphone.End(micDevice);
        isRecording = false;

        if (recordingClip == null || endPos <= 0)
        {
            Debug.LogWarning("[VoiceRecorder] Recording invalid.");
            OnRecordingStoppedWithClip?.Invoke(null);
            return;
        }

        Debug.Log($"[VoiceRecorder] Raw clip: len={recordingClip.length:F2}s, samples={recordingClip.samples}, freq={recordingClip.frequency}, ch={recordingClip.channels}");

        AudioClip trimmed = TrimClip(recordingClip, endPos);

        Debug.Log($"[VoiceRecorder] Trimmed clip: len={trimmed.length:F2}s, samples={trimmed.samples}, freq={trimmed.frequency}, ch={trimmed.channels}");

        if (playBackImmediately)
        {
            playbackSource.Stop();
            playbackSource.clip = trimmed;
            playbackSource.Play();
        }

        OnRecordingStoppedWithClip?.Invoke(trimmed);
    }

    private AudioClip TrimClip(AudioClip clip, int samplesRecorded)
    {
        float[] data = new float[samplesRecorded * clip.channels];
        clip.GetData(data, 0);

        AudioClip newClip = AudioClip.Create("Recorded_Trimmed", samplesRecorded, clip.channels, clip.frequency, false);
        newClip.SetData(data, 0);
        return newClip;
    }

    public bool IsRecording => isRecording;
}