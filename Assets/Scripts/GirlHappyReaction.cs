using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this to the DialogueSystem GameObject in the scene.
/// Call TriggerHappyReaction() when the head-stroking step completes.
/// It will:
///   1. Fire the "PlayHappy" trigger on the girl's Animator → plays Happy.anim
///   2. Play (or auto-create) a sparkle ParticleSystem burst on the girl
///   3. Wait for the Happy animation to reach its end (normalizedTime >= doneThreshold)
///   4. Stop particle emission so particles fade out naturally
///   5. The Animator's exit-time transition handles the return to Crying/Idle automatically
/// </summary>
public class GirlHappyReaction : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The girl character's Animator (Ch46_nonPBR). Wired automatically via scene.")]
    public Animator girlAnimator;

    [Tooltip("Optional: drag a pre-made ParticleSystem here. If left empty a default sparkle burst is created at runtime.")]
    public ParticleSystem happyParticles;

    [Tooltip("The girl's root Transform — used to position auto-created particles. Wired automatically.")]
    public Transform girlTransform;

    [Tooltip("Giggle sound clip played the moment the Happy animation starts (giggle.mp3).")]
    public AudioClip giggleClip;

    [Tooltip("Volume for the giggle sound (0–1).")]
    [Range(0f, 1f)]
    public float giggleVolume = 1f;

    [Header("Animator Parameter Names")]
    [Tooltip("Must exactly match the Trigger parameter name in ChildController.")]
    public string happyTriggerName = "PlayHappy";

    [Tooltip("Must exactly match the state name in ChildController.")]
    public string happyStateName = "Happy";

    [Tooltip("Animator layer index (usually 0).")]
    public int animatorLayer = 0;

    [Header("Timing")]
    [Tooltip("Seconds to wait after triggering before polling the animator state.")]
    public float pollStartDelay = 0.15f;

    [Tooltip("Normalised time (0–1) at which the Happy clip is considered done.")]
    [Range(0.8f, 1f)]
    public float doneThreshold = 0.92f;

    // -----------------------------------------------------------------------

    void Awake()
    {
        // Auto-create a sparkle particle system if none was assigned in the Inspector
        if (happyParticles == null)
            happyParticles = BuildDefaultParticles();
    }

    /// <summary>
    /// Trigger the happy reaction.
    /// <param name="withParticles">True = burst particles (squat step). False = no particles (stroke step).</param>
    /// </summary>
    public void TriggerHappyReaction(bool withParticles = true)
    {
        if (girlAnimator == null)
        {
            Debug.LogWarning("[GirlHappyReaction] girlAnimator is not assigned – reaction skipped.");
            return;
        }

        StartCoroutine(HappyReactionRoutine(withParticles));
    }

    private IEnumerator HappyReactionRoutine(bool withParticles)
    {
        // --- 1. Burst the particle system (only when requested) ---
        if (withParticles && happyParticles != null)
        {
            happyParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            happyParticles.Play();
        }

        // --- 2. Trigger the Happy animation ---
        girlAnimator.SetTrigger(happyTriggerName);

        // --- 2b. Play giggle sound at the girl's position ---
        if (giggleClip != null)
        {
            Vector3 playPos = girlTransform != null ? girlTransform.position : transform.position;
            AudioSource.PlayClipAtPoint(giggleClip, playPos, giggleVolume);
        }

        yield return new WaitForSeconds(pollStartDelay);

        // --- 3. Wait until the Animator has entered the Happy state ---
        float waitTimeout = 2f;
        float waited = 0f;
        while (!IsInHappyState())
        {
            waited += Time.deltaTime;
            if (waited > waitTimeout)
            {
                AnimatorStateInfo dbg = girlAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
                Debug.LogWarning($"[GirlHappyReaction] Timed out waiting for Happy state. " +
                                 $"Animator is currently in state hash={dbg.shortNameHash}. " +
                                 $"Check that the PlayHappy AnyState transition exists in ChildController " +
                                 $"and that happyStateName='{happyStateName}' matches exactly.");
                yield break;
            }
            yield return null;
        }

        Debug.Log("[GirlHappyReaction] Happy animation playing.");

        // --- 4. Wait for the Happy clip to near completion ---
        while (IsInHappyState())
        {
            AnimatorStateInfo info = girlAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
            if (info.normalizedTime >= doneThreshold)
                break;

            yield return null;
        }

        // --- 5. Stop emission if particles were used ---
        if (withParticles && happyParticles != null)
            happyParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);

        girlAnimator.ResetTrigger(happyTriggerName);

        Debug.Log("[GirlHappyReaction] Happy reaction complete — transitioning to IdleStanding.");
    }

    private bool IsInHappyState()
    {
        AnimatorStateInfo info = girlAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
        return info.IsName(happyStateName);
    }

    // -----------------------------------------------------------------------
    // Procedural sparkle particle system — no prefab or asset needed
    // -----------------------------------------------------------------------
    private ParticleSystem BuildDefaultParticles()
    {
        // Place particles at girl's chest height (1.2 m up from her root)
        Vector3 offset = new Vector3(0f, 1.2f, 0f);

        GameObject go = new GameObject("HappyParticles_Auto");
        go.transform.SetParent(girlTransform != null ? girlTransform : transform);
        go.transform.localPosition = offset;
        go.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // --- Main module ---
        var main = ps.main;
        main.duration = 1.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.3f, 1f),   // warm gold
            new Color(1f, 0.4f, 0.7f, 1f));    // soft pink
        main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.4f, -0.1f);  // float upward
        main.playOnAwake = false;
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // --- Emission: one-shot burst of 60 particles ---
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60, 1, 0.01f) });

        // --- Shape: sphere around the girl's chest ---
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        // --- Color over lifetime: fade to transparent ---
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // --- Size over lifetime: shrink as they fly ---
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.7f, 0.8f), new Keyframe(1f, 0f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // --- Renderer: use default particle material (sprites/default) ---
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        // Use Unity's built-in particle material if available
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit") ??
                                         Shader.Find("Legacy Shaders/Particles/Additive"));

        return ps;
    }
}
