using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class VoiceRecorderMVP : MonoBehaviour
{
    public enum MicSelectMode { AutoFirst, ByIndex, ByExactName }

    [Header("Input")]
    public InputActionReference recordToggleAction; // your bound controller button

    [Header("Enable / Disable by Flow")]
    public bool recordingEnabled = true; // DialogueFlowController will control this

    [Header("Microphone Selection")]
    public MicSelectMode micSelectMode = MicSelectMode.AutoFirst;
    public int preferredMicIndex = 0;
    public string preferredMicName = "";
    public bool logDeviceListOnStart = true;

    [Header("Recording Params")]
    public int maxRecordSeconds = 10;
    public int frequency = 44100;

    [Header("Debug")]
    public bool playBackImmediately = false; // set true only for debugging

    // Events for flow
    public event Action OnRecordingStarted;
    public event Action<AudioClip> OnRecordingStoppedWithClip;

    // runtime state
    private AudioSource playbackSource;
    private string micDevice;
    private bool isRecording = false;
    private AudioClip recordingClip;

    void Awake()
    {
        playbackSource = GetComponent<AudioSource>();
        playbackSource.playOnAwake = false;
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
            Debug.LogError("[VoiceRecorder] No microphone devices found. Check OS mic permissions/settings.");
            return;
        }

        if (logDeviceListOnStart)
            LogMicrophoneDevices();

        micDevice = SelectMicrophone();
        Debug.Log($"[VoiceRecorder] Selected microphone: {micDevice}");
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

            case MicSelectMode.AutoFirst:
            default:
                return Microphone.devices[0];
        }
    }

    private void OnRecordTogglePerformed(InputAction.CallbackContext ctx)
    {
        if (!recordingEnabled) return;
        if (Microphone.devices.Length == 0) return;

        // Helpful log for binding verification
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

        recordingClip = Microphone.Start(micDevice, false, maxRecordSeconds, frequency);
        isRecording = true;

        Debug.Log("[VoiceRecorder] Recording started...");
        OnRecordingStarted?.Invoke();
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        int endPos = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        isRecording = false;

        if (recordingClip == null || endPos <= 0)
        {
            Debug.LogWarning("[VoiceRecorder] Recording invalid (clip null or endPos <= 0).");
            OnRecordingStoppedWithClip?.Invoke(null);
            return;
        }

        AudioClip trimmed = TrimClip(recordingClip, endPos);
        Debug.Log($"[VoiceRecorder] Recording stopped. Length: {trimmed.length:F2}s");

        if (playBackImmediately)
        {
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