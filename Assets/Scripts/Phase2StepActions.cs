using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Phase2StepActions : MonoBehaviour
{
    [Header("Flow References")]
    public DialogueFlowController phase1Flow;   
    public DialogueFlowController phase2Flow;   

    [Header("Audio (2D recommended)")]
    public AudioSource sfxSource2D;             
    public AudioClip cryingGirlClip;

    [Tooltip("Small delay before auto-completing steps after audio starts/ends.")]
    public float completeDelay = 0.1f;

    [Header("Phase 2 Step Ids (these are Phase2 stepIds)")]
    public int phase2Step_Cry = 1;
    public int phase2Step_PlayRec1 = 2;
    public int phase2Step_PlayRec2 = 6;
    public int phase2Step_PlayRec3 = 8;

    [Header("Phase 1 Recording StepIds (where recordings were SAVED in Phase1)")]
    public int phase1RecordingId_1 = 0;
    public int phase1RecordingId_2 = 5;
    public int phase1RecordingId_3 = 8;

    void Reset()
    {
        sfxSource2D = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (!sfxSource2D) sfxSource2D = GetComponent<AudioSource>();

  
        if (sfxSource2D)
        {
            sfxSource2D.playOnAwake = false;
            sfxSource2D.spatialBlend = 0f; 
        }

        if (phase2Flow != null)
            phase2Flow.OnStepStarted += HandlePhase2StepStarted;
    }

    void OnDisable()
    {
        if (phase2Flow != null)
            phase2Flow.OnStepStarted -= HandlePhase2StepStarted;
    }

    private void HandlePhase2StepStarted(int stepId)
    {
        if (phase1Flow == null || phase2Flow == null)
        {
            Debug.LogError("[Phase2StepActions] Missing phase1Flow or phase2Flow reference.");
            return;
        }

        // STEP 1: Crying sound
        if (stepId == phase2Step_Cry)
        {
            StartCoroutine(PlayClipThenComplete(cryingGirlClip, stepId));
            return;
        }

        // STEP 2: Playback recording 1
        if (stepId == phase2Step_PlayRec1)
        {
            TryPlayPhase1RecordingThenComplete(phase1RecordingId_1, stepId);
            return;
        }

        // STEP 6: Playback recording 2
        if (stepId == phase2Step_PlayRec2)
        {
            TryPlayPhase1RecordingThenComplete(phase1RecordingId_2, stepId);
            return;
        }

        // STEP 8: Playback recording 3
        if (stepId == phase2Step_PlayRec3)
        {
            TryPlayPhase1RecordingThenComplete(phase1RecordingId_3, stepId);
            return;
        }

    }

    private void TryPlayPhase1RecordingThenComplete(int phase1RecordingStepId, int currentPhase2StepId)
    {
        var recs = phase1Flow.Recordings;
        if (recs == null)
        {
            Debug.LogError("[Phase2StepActions] phase1Flow.Recordings is null.");
            return;
        }

        if (!recs.ContainsKey(phase1RecordingStepId))
        {
            var keys = recs.Keys.Any() ? string.Join(", ", recs.Keys) : "(none)";
            Debug.LogWarning(
                $"[Phase2StepActions] Missing recording for Phase1 stepId={phase1RecordingStepId}. " +
                $"Available keys: {keys}. " +
                $"(This usually means your Phase1 stepId is not what you think.)"
            );
          
            StartCoroutine(CompleteAfterDelay(currentPhase2StepId, completeDelay));
            return;
        }

        AudioClip clip = recs[phase1RecordingStepId];
        StartCoroutine(PlayClipThenComplete(clip, currentPhase2StepId));
    }

    private IEnumerator PlayClipThenComplete(AudioClip clip, int phase2StepId)
    {
        if (clip == null)
        {
            Debug.LogWarning("[Phase2StepActions] Tried to play a null clip.");
            yield return CompleteAfterDelay(phase2StepId, completeDelay);
            yield break;
        }

        if (sfxSource2D == null)
        {
            Debug.LogError("[Phase2StepActions] No AudioSource assigned.");
            yield break;
        }

        sfxSource2D.Stop();
        sfxSource2D.clip = clip;
        sfxSource2D.Play();

        yield return new WaitWhile(() => sfxSource2D.isPlaying);
        yield return CompleteAfterDelay(phase2StepId, completeDelay);
    }

    private IEnumerator CompleteAfterDelay(int stepId, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        phase2Flow.MarkStepComplete(stepId);
    }
}