// DialogueFlowController.cs  (FULL, with girl bubble multi-segment controlled by handlers)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueFlowController : MonoBehaviour
{
    public enum StepCompletionMode
    {
        AutoAfterDelay,
        WaitForExternalSignal,
        Recording,
        PressConfirmButton
    }

    [Serializable]
    public class StepDefinition
    {
        public int stepId;

        [Header("Head prompt (multi-segment)")]
        [TextArea] public string[] headPromptSegments;   // shown segment by segment, continued by confirmAction

        [TextArea] public string hudTaskText;            // shown on top-right HUD (only after last segment)
        [TextArea] public string girlDialogueText;       // optional legacy (not used if handler drives bubble)

        public StepCompletionMode completionMode = StepCompletionMode.WaitForExternalSignal;

        [Tooltip("If true, when the last head prompt segment is shown, HUD appears immediately (no extra Continue).")]
        public bool showHudOnLastHeadSegment = false;

        [Tooltip("Used when completionMode = AutoAfterDelay")]
        public float autoCompleteDelay = 2f;

        [Tooltip("Optional legacy flag (only used if useCompletionModeForRecording = false)")]
        public bool requiresRecording = false;

        [Tooltip("Optional: per-step handler component for custom logic")]
        public DialogueStepHandler handler; // can be null
    }

    [Header("UI References")]
    public TMP_Text topRightTaskText;   // top-right HUD
    public TMP_Text headPromptText;     // world-space TMP text near the girl's head
    public Transform headPromptAnchor;  // anchor transform on girl's head

    [Header("Girl Dialogue Bubble (world-space)")]
    public GirlDialogueBubble girlBubble; // drag bubble root (with GirlDialogueBubble) here

    [Header("Task Completion Feedback (HUD + SFX)")]
    public Color hudNormalColor = Color.white;
    public Color hudCompletedColor = Color.green;
    [Tooltip("Shown briefly when a step completes (appended on HUD).")]
    public string completedSuffix = "  ✓";
    public float completedFlashDuration = 0.8f;

    [Tooltip("Optional: assign an AudioSource (2D) for UI SFX.")]
    public AudioSource uiSfxSource;
    [Tooltip("Optional: assign a completion sound.")]
    public AudioClip stepCompleteSfx;

    [Header("Recording Validation (MVP)")]
    public float minRecordingSeconds = 1.0f;
    public float hudMessageDuration = 1.2f;

    [Header("Steps (MVP)")]
    public List<StepDefinition> steps = new List<StepDefinition>();

    [Header("Voice Recording Integration")]
    public VoiceRecorderMVP voiceRecorder;

    [Tooltip("If true, record steps are those with completionMode == Recording. If false, use requiresRecording flag.")]
    public bool useCompletionModeForRecording = true;

    [Header("Control")]
    public bool autoStartOnPlay = false;

    [Header("Debug / Test Mode")]
    public bool testModeIgnoreConditions = false;

    public InputActionReference nextStepAction;   // bind to Keyboard N + Controller button
    public InputActionReference confirmAction;    // bind to Keyboard Space + Controller button

    // state
    public int CurrentStepId { get; private set; } = -1;
    public StepDefinition CurrentStep { get; private set; } = null;
    private int currentIndex = -1;
    private bool flowStarted = false;
    private Coroutine autoCompleteRoutine;

    public bool clearGirlDialogueOnStepChange = true;

    // events
    public event Action<int> OnStepStarted;
    public event Action<int> OnStepCompleted;
    public event Action OnPhase1Completed;

    public event Action OnGirlDialogueReachedLastSegment;

    // recordings
    private Dictionary<int, AudioClip> recordings = new Dictionary<int, AudioClip>();
    public IReadOnlyDictionary<int, AudioClip> Recordings => recordings;

    // HUD REC status
    private string baseHudText = "";
    private bool recOn = false;

    // HUD temp message
    private Coroutine hudTempMessageRoutine;

    // Head prompt multi-segment state
    private int headSegIndex = 0;
    private bool waitingHeadSegments = false; // true = still showing head prompt segments, HUD not shown yet

    // Girl bubble multi-segment state (CONTROLLED BY HANDLERS)
    private string[] girlSegments = null;
    private int girlSegIndex = 0;
    private bool girlDialogueActive = false;

    // completion guard
    private bool isCompletingStep = false;

    void OnEnable()
    {
        if (voiceRecorder != null)
        {
            voiceRecorder.OnRecordingStarted += HandleRecStarted;
            voiceRecorder.OnRecordingStoppedWithClip += HandleRecStopped;
        }

        if (nextStepAction != null)
        {
            nextStepAction.action.performed += OnNextStepPerformed;
            nextStepAction.action.Enable();
        }

        if (confirmAction != null)
        {
            confirmAction.action.performed += OnConfirmPerformed;
            confirmAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (voiceRecorder != null)
        {
            voiceRecorder.OnRecordingStarted -= HandleRecStarted;
            voiceRecorder.OnRecordingStoppedWithClip -= HandleRecStopped;
        }

        if (nextStepAction != null)
        {
            nextStepAction.action.performed -= OnNextStepPerformed;
            nextStepAction.action.Disable();
        }

        if (confirmAction != null)
        {
            confirmAction.action.performed -= OnConfirmPerformed;
            confirmAction.action.Disable();
        }
    }

    void Start()
    {
        if (headPromptText != null)
            headPromptText.gameObject.SetActive(false);

        // HUD init
        baseHudText = "";
        recOn = false;
        if (topRightTaskText != null) topRightTaskText.color = hudNormalColor;
        UpdateHudText();

        // Bubble init (hidden)
        if (girlBubble != null) girlBubble.Show(false);

        // Safety: disable recording until flow says otherwise
        if (voiceRecorder != null)
            voiceRecorder.recordingEnabled = false;

        if (autoStartOnPlay)
            StartFlow();
    }

    void Update()
    {
        if (!flowStarted) return;

        // Follow head anchor
        if (headPromptText != null && headPromptAnchor != null)
            headPromptText.transform.position = headPromptAnchor.position;

        // Optional per-step tick (only after head segments finished, unless you want otherwise)
        if (!testModeIgnoreConditions && !waitingHeadSegments && CurrentStep != null && CurrentStep.handler != null)
        {
            CurrentStep.handler.Tick(this, CurrentStep);
        }
    }

    public void StartFlow()
    {
        if (flowStarted) return;
        flowStarted = true;
        currentIndex = -1;
        MoveNextStep();
    }

    private void MoveNextStep()
    {
        // Cleanup previous step
        StopAutoCompleteRoutineIfAny();

        if (CurrentStep != null && CurrentStep.handler != null)
            CurrentStep.handler.OnStepExit(this, CurrentStep);

        // Reset indicators
        isCompletingStep = false;
        recOn = false;
        waitingHeadSegments = false;
        headSegIndex = 0;

        // Reset girl bubble state each step (default behavior)
        if (clearGirlDialogueOnStepChange)
            ClearGirlDialogue();

        // Reset HUD color for new step
        if (topRightTaskText != null) topRightTaskText.color = hudNormalColor;

        // Hide head prompt when switching steps
        if (headPromptText != null)
            headPromptText.gameObject.SetActive(false);

        currentIndex++;

        if (currentIndex >= steps.Count)
        {
            Debug.Log("[DialogueFlow] Phase 1 completed.");
            OnPhase1Completed?.Invoke();
            LogRecordingKeys();

            if (voiceRecorder != null)
                voiceRecorder.recordingEnabled = false;

            // Clear HUD (optional)
            baseHudText = "";
            recOn = false;
            UpdateHudText();

            return;
        }

        var step = steps[currentIndex];
        CurrentStep = step;
        CurrentStepId = step.stepId;

        // 1) Start head prompt multi-segment
        string[] segs = step.headPromptSegments;
        bool hasSegs = (segs != null && segs.Length > 0);

        if (hasSegs)
        {
            waitingHeadSegments = true;
            headSegIndex = 0;

            ShowHeadPromptImmediate(segs[0]);

            // NEW: If the first segment is also the last, optionally show HUD immediately
            if (step.showHudOnLastHeadSegment && segs.Length == 1)
            {
                waitingHeadSegments = false;
                SetupHudForStep(step);
                StartStepRulesAfterHud(step);
            }

            // HUD stays hidden until last segment appears
            baseHudText = "";
            recOn = false;
            UpdateHudText();

            // During head segments: disable recording
            if (voiceRecorder != null)
                voiceRecorder.recordingEnabled = false;
        }
        else
        {
            // No head prompt segments: show HUD immediately and start step rules
            waitingHeadSegments = false;
            SetupHudForStep(step);
            StartStepRulesAfterHud(step);
        }

        Debug.Log($"[DialogueFlow] Step started: {step.stepId} mode={step.completionMode}");
        OnStepStarted?.Invoke(step.stepId);

        // 2) handler enter (handler controls bubble + actions)
        if (step.handler != null)
            step.handler.OnStepEnter(this, step);
    }

    // Called when we are ready to show HUD for this step (at last head segment)
    private void SetupHudForStep(StepDefinition step)
    {
        bool isRecStepForHud = useCompletionModeForRecording
            ? (step.completionMode == StepCompletionMode.Recording)
            : step.requiresRecording;

        baseHudText = isRecStepForHud
            ? (step.hudTaskText + "\n(Press A to start/stop recording)")
            : step.hudTaskText;

        recOn = false;
        UpdateHudText();
    }

    // Called after HUD appears (start AutoAfterDelay timer / enable recording, etc.)
    private void StartStepRulesAfterHud(StepDefinition step)
    {
        // enable/disable recording based on step (only after HUD is shown)
        if (voiceRecorder != null)
        {
            bool isRecStep = useCompletionModeForRecording
                ? (step.completionMode == StepCompletionMode.Recording)
                : step.requiresRecording;

            voiceRecorder.recordingEnabled = isRecStep;
        }

        // completion mode setup
        if (!testModeIgnoreConditions)
        {
            if (step.completionMode == StepCompletionMode.AutoAfterDelay)
            {
                StopAutoCompleteRoutineIfAny();
                autoCompleteRoutine = StartCoroutine(AutoCompleteAfter(step.autoCompleteDelay));
            }
        }
    }

    private IEnumerator AutoCompleteAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteCurrentStep();
    }

    private void StopAutoCompleteRoutineIfAny()
    {
        if (autoCompleteRoutine != null)
        {
            StopCoroutine(autoCompleteRoutine);
            autoCompleteRoutine = null;
        }
    }

    private void ShowHeadPromptImmediate(string text)
    {
        if (headPromptText == null) return;

        headPromptText.text = text;
        headPromptText.gameObject.SetActive(true);
    }

    private void UpdateHudText()
    {
        if (topRightTaskText == null) return;

        if (string.IsNullOrEmpty(baseHudText))
        {
            topRightTaskText.text = "";
            return;
        }

        topRightTaskText.text = recOn ? (baseHudText + "\n● REC") : baseHudText;
    }

    // ===== Public API for handlers to drive GIRL dialogue bubble =====
    public void SetGirlDialogueSegments(string[] segments, bool showImmediately = true)
    {
        if (segments == null || segments.Length == 0)
        {
            ClearGirlDialogue();
            return;
        }

        girlSegments = segments;
        girlSegIndex = 0;
        girlDialogueActive = true;

        if (girlBubble != null)
        {
            girlBubble.SetText(girlSegments[0]);
            girlBubble.Show(showImmediately);
        }
    }

    public void ClearGirlDialogue()
    {
        girlSegments = null;
        girlSegIndex = 0;
        girlDialogueActive = false;

        if (girlBubble != null)
            girlBubble.Show(false);
    }

    private void ContinueGirlDialogueSegments()
    {
        if (!girlDialogueActive) return;
        if (girlSegments == null || girlSegments.Length == 0) return;

        // if already at last segment, do nothing (do NOT hide)
        if (girlSegIndex >= girlSegments.Length - 1)
            return;

        girlSegIndex++;
        if (girlSegIndex >= girlSegments.Length - 1)
        {
            OnGirlDialogueReachedLastSegment?.Invoke();
        }
        if (girlBubble != null)
            girlBubble.SetText(girlSegments[girlSegIndex]);
    }

    // ===== Recorder event handlers =====
    private void HandleRecStarted()
    {
        if (CurrentStep == null) return;
        if (waitingHeadSegments) return;

        bool isRecStep = useCompletionModeForRecording
            ? (CurrentStep.completionMode == StepCompletionMode.Recording)
            : CurrentStep.requiresRecording;

        if (!isRecStep) return;

        recOn = true;
        UpdateHudText();
    }

    private void HandleRecStopped(AudioClip clip)
    {
        if (CurrentStep == null) return;
        if (waitingHeadSegments) return;

        bool isRecStep = useCompletionModeForRecording
            ? (CurrentStep.completionMode == StepCompletionMode.Recording)
            : CurrentStep.requiresRecording;

        if (!isRecStep) return;

        recOn = false;
        UpdateHudText();

        // Validation
        if (clip == null)
        {
            Debug.LogWarning($"[DialogueFlow] Recording returned null for stepId={CurrentStepId}");
            ShowHudTempMessage("No voice captured. Please try again.");
            return;
        }

        if (clip.length < minRecordingSeconds)
        {
            Debug.LogWarning($"[DialogueFlow] Recording too short ({clip.length:F2}s) for stepId={CurrentStepId}");
            ShowHudTempMessage($"Too short ({clip.length:F1}s). Hold and speak, then stop.");
            return;
        }

        recordings[CurrentStepId] = clip;
        Debug.Log($"[DialogueFlow] Saved recording stepId={CurrentStepId}, length={clip.length:F2}s");

        CompleteCurrentStep();
    }

    // ===== Input actions =====
    private void OnNextStepPerformed(InputAction.CallbackContext ctx)
    {
        if (!flowStarted) return;
        CompleteCurrentStep();
    }

    private void OnConfirmPerformed(InputAction.CallbackContext ctx)
    {
        if (!flowStarted) return;
        if (testModeIgnoreConditions) return;
        if (CurrentStep == null) return;

        // Priority 1: head prompt segment continue
        if (waitingHeadSegments)
        {
            ContinueHeadPromptSegments();
            return;
        }

        // Priority 2: girl's bubble continue (if active) - do NOT auto hide
        if (girlDialogueActive)
        {
            ContinueGirlDialogueSegments();
            return;
        }

        // Priority 3: confirm-to-complete steps
        if (CurrentStep.completionMode == StepCompletionMode.PressConfirmButton)
        {
            CompleteCurrentStep();
        }
    }

    private void ContinueHeadPromptSegments()
    {
        if (CurrentStep == null) return;

        var segs = CurrentStep.headPromptSegments;
        if (segs == null || segs.Length == 0)
        {
            waitingHeadSegments = false;
            SetupHudForStep(CurrentStep);
            StartStepRulesAfterHud(CurrentStep);
            return;
        }

        headSegIndex++;

        if (headSegIndex < segs.Length)
        {
            ShowHeadPromptImmediate(segs[headSegIndex]);

            // NEW: If configured, show HUD immediately when last segment is shown
            if (headSegIndex == segs.Length - 1 && CurrentStep.showHudOnLastHeadSegment)
            {
                waitingHeadSegments = false;
                SetupHudForStep(CurrentStep);
                StartStepRulesAfterHud(CurrentStep);
            }
        }
        else
        {
            // Safety fallback if somehow goes past end
            waitingHeadSegments = false;
            SetupHudForStep(CurrentStep);
            StartStepRulesAfterHud(CurrentStep);
        }
    }

    // ===== HUD temp message =====
    private void ShowHudTempMessage(string message)
    {
        if (topRightTaskText == null) return;

        if (hudTempMessageRoutine != null)
            StopCoroutine(hudTempMessageRoutine);

        hudTempMessageRoutine = StartCoroutine(HudTempMessageRoutine(message));
    }

    private IEnumerator HudTempMessageRoutine(string message)
    {
        string previousBase = baseHudText;
        bool previousRec = recOn;
        Color previousColor = topRightTaskText != null ? topRightTaskText.color : hudNormalColor;

        topRightTaskText.color = hudNormalColor;
        topRightTaskText.text = message;

        yield return new WaitForSeconds(hudMessageDuration);

        baseHudText = previousBase;
        recOn = previousRec;

        if (topRightTaskText != null) topRightTaskText.color = previousColor;
        UpdateHudText();

        hudTempMessageRoutine = null;
    }

    private void LogRecordingKeys()
    {
        if (recordings == null || recordings.Count == 0)
        {
            Debug.Log("[DialogueFlow] Recordings: (none)");
            return;
        }

        Debug.Log("[DialogueFlow] Recordings keys: " + string.Join(", ", recordings.Keys));
    }

    public void CompleteCurrentStepWithDelay(float delaySeconds)
    {
        StartCoroutine(CompleteAfterDelay(delaySeconds));
    }

    private IEnumerator CompleteAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        CompleteCurrentStep();
    }

    // ===== Completion feedback =====
    private IEnumerator StepCompleteFeedbackThenAdvance()
    {
        // Play SFX (optional)
        if (uiSfxSource != null && stepCompleteSfx != null)
            uiSfxSource.PlayOneShot(stepCompleteSfx);

        // Show green completed HUD briefly
        if (topRightTaskText != null)
        {
            topRightTaskText.color = hudCompletedColor;

            if (!string.IsNullOrEmpty(baseHudText))
                topRightTaskText.text = baseHudText + completedSuffix;
        }

        yield return new WaitForSeconds(completedFlashDuration);

        // restore normal color before next step
        if (topRightTaskText != null)
            topRightTaskText.color = hudNormalColor;

        MoveNextStep();
    }

    // ===== Public completion APIs =====
    public void CompleteCurrentStep()
    {
        if (!flowStarted) return;
        if (CurrentStep == null) return;
        if (isCompletingStep) return;

        // Don't allow advancing while recording
        if (voiceRecorder != null && voiceRecorder.IsRecording)
        {
            ShowHudTempMessage("Stop recording first.");
            return;
        }

        // If still showing head segments, don't complete (must continue)
        if (waitingHeadSegments)
        {
            ShowHudTempMessage("Continue first.");
            return;
        }

        isCompletingStep = true;

        Debug.Log($"[DialogueFlow] Step completed: {CurrentStepId}");
        OnStepCompleted?.Invoke(CurrentStepId);

        StartCoroutine(StepCompleteFeedbackThenAdvance());
    }

    public void MarkStepComplete(int stepId)
    {
        if (!flowStarted) return;
        if (stepId != CurrentStepId)
        {
            Debug.LogWarning($"[DialogueFlow] Ignored completion: stepId={stepId}, but CurrentStepId={CurrentStepId}");
            return;
        }

        CompleteCurrentStep();
    }
}