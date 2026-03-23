using UnityEngine;

/// <summary>
/// Attach to the DialogueSystem GameObject.
/// Provides PlaySad(), PlayTalking(), and StopTalking() so any step handler
/// can drive the girl's animation without knowing the Animator internals.
/// </summary>
public class GirlAnimationPlayer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The girl character's Animator (Ch46_nonPBR).")]
    public Animator girlAnimator;

    [Header("Animator Parameter Names")]
    public string sadTriggerName     = "PlaySad";
    public string talkingTriggerName = "PlayTalking";
    public string stopTalkingTrigger = "StopTalking";

    // -----------------------------------------------------------------------

    /// <summary>Play Sad animation. Animator auto-transitions to IdleStanding at exit time.</summary>
    public void PlaySad()
    {
        if (!CheckAnimator()) return;
        girlAnimator.ResetTrigger(talkingTriggerName);
        girlAnimator.ResetTrigger(stopTalkingTrigger);
        girlAnimator.SetTrigger(sadTriggerName);
        Debug.Log("[GirlAnimationPlayer] PlaySad triggered.");
    }

    /// <summary>Start looping Talking animation (while dialogue bubble is visible).</summary>
    public void PlayTalking()
    {
        if (!CheckAnimator()) return;
        girlAnimator.ResetTrigger(stopTalkingTrigger);
        girlAnimator.SetTrigger(talkingTriggerName);
        Debug.Log("[GirlAnimationPlayer] PlayTalking triggered.");
    }

    /// <summary>Stop Talking animation and blend back to IdleStanding.</summary>
    public void StopTalking()
    {
        if (!CheckAnimator()) return;
        girlAnimator.ResetTrigger(talkingTriggerName);
        girlAnimator.SetTrigger(stopTalkingTrigger);
        Debug.Log("[GirlAnimationPlayer] StopTalking triggered.");
    }

    // -----------------------------------------------------------------------

    private bool CheckAnimator()
    {
        if (girlAnimator != null) return true;
        Debug.LogWarning("[GirlAnimationPlayer] girlAnimator is not assigned — skipped.");
        return false;
    }
}
