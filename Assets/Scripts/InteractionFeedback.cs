using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Preserve existing enum values and Play call sites.
public enum InteractionCue
{
    Jump, Land, Climb, Pickup, Drop, PotImpact, Bounce, Collect, TimeTravel,
    Trap, Respawn, BatFlee, Reveal, Bloom, LaserHit, LaserSpark, Denied, Portal
}

public sealed class InteractionFeedback : MonoBehaviour
{
    const int PoolLimit = 56;
    const int VoiceLimit = 12;
    static InteractionFeedback instance;
    [SerializeField, Range(0f, 1f)] float masterVolume = 0.75f;
    [SerializeField, Range(0.25f, 1.5f)] float effectScale = 1f;
    readonly List<ParticleSystem> pool = new List<ParticleSystem>();
    // Cosmetic variation must not alter gameplay's UnityEngine.Random sequence.
    readonly System.Random random = new System.Random(7139);
    readonly AudioSource[] voices = new AudioSource[VoiceLimit];
    readonly Material[] materials = new Material[7];
    AudioClip[][] clips;
    float[] nextSoundTime;
    int[] lastVariant;
    int poolCursor;
    Gradient fade;
    AnimationCurve shrink;
    AnimationCurve expand;
    enum Shape { Dust, Star, Ring, Glow, Leaf, Streak, Mote }
    static readonly Color Cream = new Color(1f, 0.91f, 0.64f);
    static readonly Color Mint = new Color(0.64f, 0.94f, 0.76f);
    static readonly Color Rose = new Color(1f, 0.65f, 0.76f);
    static readonly Color Cyan = new Color(0.57f, 0.9f, 1f);
    static readonly Color Lilac = new Color(0.76f, 0.65f, 1f);
    static readonly Color Dust = new Color(0.77f, 0.75f, 0.57f, 0.55f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Warmup() { EnsureInstance(); }

    public static void Play(InteractionCue cue, Vector3 position, bool playSound = true,
        bool playParticles = true, float intensity = 1f)
    {
        if ((int)cue < 0 || (int)cue > (int)InteractionCue.Portal) return;
        if (Application.isPlaying && Time.timeScale <= 0f) return;
        EnsureInstance().PlayCue(cue, position, playSound, playParticles, Mathf.Clamp(intensity, 0.35f, 1.6f));
    }

    static InteractionFeedback EnsureInstance()
    {
        if (instance != null) return instance;
        instance = FindFirstObjectByType<InteractionFeedback>();
        if (instance != null) return instance;
        return new GameObject(nameof(InteractionFeedback)).AddComponent<InteractionFeedback>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        int count = Enum.GetValues(typeof(InteractionCue)).Length;
        clips = new AudioClip[count][];
        nextSoundTime = new float[count];
        lastVariant = new int[count];
        for (int i = 0; i < count; i++)
        {
            var variants = new List<AudioClip>();
            for (int v = 1; v <= 3; v++)
            {
                var clip = Resources.Load<AudioClip>("Feedback/Audio/" + (InteractionCue)i + "_" + v);
                if (clip != null) variants.Add(clip);
            }
            clips[i] = variants.ToArray();
            lastVariant[i] = -1;
            if (clips[i].Length == 0 && (InteractionCue)i != InteractionCue.LaserSpark)
                Debug.LogWarning("Feedback audio missing: " + (InteractionCue)i, this);
        }
        for (int i = 0; i < VoiceLimit; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0f;
            voices[i].dopplerLevel = 0f;
            voices[i].reverbZoneMix = 0f;
        }
        // Resource reference ensures the custom shader is included in player builds.
        Shader shader = Resources.Load<Shader>("Feedback/FeedbackParticle");
        if (shader != null)
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = new Material(shader) { name = "Feedback " + (Shape)i };
                materials[i].SetFloat("_Shape", i);
            }
        else Debug.LogError("Feedback/FeedbackParticle shader missing.", this);
        fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.07f),
                new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) });
        shrink = new AnimationCurve(new Keyframe(0f, 0.65f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0.12f));
        expand = AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1.35f);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        foreach (var system in pool) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Array.Clear(nextSoundTime, 0, nextSoundTime.Length);
        // Let the portal audio tail finish across the scene boundary.
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
        foreach (var material in materials)
            if (material != null)
            {
                if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
            }
        // Imported clips are shared Resources assets and must not be destroyed here.
    }

    void PlayCue(InteractionCue cue, Vector3 p, bool sound, bool particles, float strength)
    {
        if (sound) PlayAudio(cue, p, strength);
        if (!particles || materials[0] == null) return;
        float s = strength * effectScale;
        switch (cue)
        {
            case InteractionCue.Jump:
            case InteractionCue.Land:
            case InteractionCue.PotImpact:
                Ground(p, s * (cue == InteractionCue.Jump ? 0.65f : cue == InteractionCue.PotImpact ? 1.15f : 1f));
                break;
            case InteractionCue.Climb:
                Scatter(p, Shape.Dust, 4, Dust, Cream, 0.22f, 0.1f, 0.3f, 0.2f, 0.2f, s, true);
                break;
            case InteractionCue.Pickup:
                Halo(p, Cream, 0.7f * s, 0.3f);
                Scatter(p, Shape.Star, 6, Cream, Mint, 0.16f, 0.7f, 0.42f, 0.3f, -0.08f, s);
                break;
            case InteractionCue.Drop:
                Scatter(p, Shape.Dust, 5, Dust, Cream, 0.18f, 0.35f, 0.25f, 0.12f, 0.15f, s, true);
                break;
            case InteractionCue.Bounce:
            case InteractionCue.Bloom:
                Halo(p, Mint, 1.25f * s, 0.35f);
                Ring(p, Mint, 1.45f * s, 0.48f, cue == InteractionCue.Bounce ? 0.3f : 0.7f);
                Scatter(p, Shape.Leaf, 11, Mint, Rose, 0.22f, 1.5f, 0.85f, 0.15f, 0.14f, s, true);
                Scatter(p, Shape.Star, 7, Cream, Rose, 0.11f, 0.85f, 0.65f, 0.25f, -0.08f, s);
                break;
            case InteractionCue.Collect:
                Halo(p, Cream, 1.5f * s, 0.28f);
                Ring(p, Cream, 1.6f * s, 0.48f);
                Scatter(p, Shape.Star, 8, Cream, new Color(0.85f, 1f, 0.88f), 0.2f, 1.8f, 0.8f, 0.25f, -0.06f, s);
                Scatter(p, Shape.Mote, 12, Mint, Cream, 0.07f, 1.9f, 0.65f, 0.1f, 0.06f, s);
                break;
            case InteractionCue.TimeTravel:
            case InteractionCue.Portal:
                Halo(p, Lilac, 2.1f * s, 0.6f);
                Ring(p, Cyan, 2.4f * s, 0.85f, 1.15f);
                Ring(p, Lilac, 1.7f * s, 0.6f, 0.82f);
                Scatter(p, Shape.Star, 15, Cyan, Lilac, 0.16f, 1.6f, 1f, 0.35f, -0.03f, s);
                Scatter(p, Shape.Streak, 9, Cyan, Color.white, 0.28f, 2f, 0.45f, 0.3f, 0f, s);
                break;
            case InteractionCue.Trap:
                Halo(p, Rose, 1.1f * s, 0.2f);
                Scatter(p, Shape.Streak, 9, Rose, Cream, 0.25f, 2f, 0.28f, 0.08f, 0.25f, s);
                break;
            case InteractionCue.Respawn:
                Halo(p, Mint, 1.5f * s, 0.45f);
                Ring(p, Cyan, 1.5f * s, 0.6f);
                Scatter(p, Shape.Star, 10, Mint, Color.white, 0.15f, 0.65f, 0.8f, 0.4f, -0.15f, s);
                break;
            case InteractionCue.BatFlee:
                Scatter(p, Shape.Leaf, 7, new Color(0.42f, 0.36f, 0.5f, 0.7f), Lilac,
                    0.18f, 1.3f, 0.55f, 0.2f, 0.18f, s);
                break;
            case InteractionCue.Reveal:
                Halo(p, Mint, 1.6f * s, 0.55f);
                Scatter(p, Shape.Star, 12, Mint, Cream, 0.14f, 0.6f, 0.85f, 0.65f, -0.12f, s, true);
                break;
            case InteractionCue.LaserHit:
            case InteractionCue.LaserSpark:
                bool hit = cue == InteractionCue.LaserHit;
                Halo(p, Cream, (hit ? 0.55f : 0.28f) * s, 0.15f);
                Scatter(p, Shape.Streak, hit ? 7 : 3, Cream, new Color(1f, 0.55f, 0.28f),
                    0.16f, 1.8f, 0.22f, 0.025f, 0.6f, s);
                break;
            case InteractionCue.Denied:
                Ring(p, new Color(0.9f, 0.6f, 0.53f, 0.55f), 0.65f * s, 0.25f);
                break;
        }
    }

    void PlayAudio(InteractionCue cue, Vector3 position, float strength)
    {
        int index = (int)cue;
        if (clips[index].Length == 0 || Time.unscaledTime < nextSoundTime[index]) return;
        var camera = Camera.main;
        float distanceGain = 1f;
        if (camera != null && (cue == InteractionCue.LaserHit || cue == InteractionCue.BatFlee || cue == InteractionCue.Reveal))
        {
            float radius = camera.orthographic ? camera.orthographicSize * Mathf.Max(1f, camera.aspect) : 10f;
            float distance = Vector2.Distance(position, camera.transform.position);
            distanceGain = 1f - Mathf.InverseLerp(radius, radius * 2f, distance);
            if (distanceGain <= 0f) return;
        }
        float gap = cue == InteractionCue.LaserHit ? 0.35f : cue == InteractionCue.TimeTravel ? 0.25f : 0.075f;
        nextSoundTime[index] = Time.unscaledTime + gap;
        int priority = cue == InteractionCue.Climb || cue == InteractionCue.LaserHit ? 160 : 64;
        AudioSource voice = null;
        foreach (var candidate in voices) if (!candidate.isPlaying) { voice = candidate; break; }
        if (voice == null)
        {
            foreach (var candidate in voices)
                if (candidate.priority > priority && (voice == null || candidate.priority > voice.priority)) voice = candidate;
            if (voice == null) return;
        }
        int variant = random.Next(clips[index].Length);
        if (clips[index].Length > 1 && variant == lastVariant[index]) variant = (variant + 1) % clips[index].Length;
        lastVariant[index] = variant;
        voice.Stop();
        voice.clip = clips[index][variant];
        voice.priority = priority;
        bool musical = cue == InteractionCue.Collect || cue == InteractionCue.TimeTravel || cue == InteractionCue.Bloom
            || cue == InteractionCue.Reveal || cue == InteractionCue.Portal;
        voice.pitch = musical ? 1f : Range(0.97f, 1.03f);
        voice.panStereo = 0f;
        if (camera != null && cue != InteractionCue.TimeTravel && cue != InteractionCue.Portal && cue != InteractionCue.Respawn)
            voice.panStereo = Mathf.Clamp((position.x - camera.transform.position.x) / 15f, -0.35f, 0.35f);
        float level = cue == InteractionCue.Climb ? 0.19f : cue == InteractionCue.LaserHit ? 0.24f :
            cue == InteractionCue.TimeTravel || cue == InteractionCue.PotImpact || cue == InteractionCue.Jump ? 0.22f :
            cue == InteractionCue.Land ? 0.16f :
            cue == InteractionCue.Drop ? 0.32f : 0.48f;
        voice.volume = masterVolume * level * Mathf.Sqrt(strength) * distanceGain;
        voice.Play();
    }

    void Ground(Vector3 p, float s)
    {
        Scatter(p + Vector3.up * 0.04f, Shape.Dust, 9, Dust, new Color(0.92f, 0.87f, 0.69f, 0.45f),
            0.36f, 1.15f, 0.48f, 0.16f, 0.18f, s, true);
        Scatter(p + Vector3.up * 0.06f, Shape.Leaf, 3, new Color(0.54f, 0.8f, 0.64f, 0.7f), Cream,
            0.12f, 0.85f, 0.4f, 0.12f, 0.3f, s, true);
        Ring(p, new Color(0.9f, 0.86f, 0.64f, 0.3f), 1.15f * s, 0.28f, 0.18f);
    }

    void Halo(Vector3 p, Color color, float size, float life)
    {
        color.a *= 0.22f;
        Single(p, Shape.Glow, color, size, life, 1f);
    }

    void Ring(Vector3 p, Color color, float size, float life, float aspect = 1f)
    {
        color.a *= 0.7f;
        Single(p, Shape.Ring, color, size, life, aspect);
    }

    void Single(Vector3 p, Shape shape, Color color, float size, float life, float aspect)
    {
        var ps = Rent(shape, p, 0f, true);
        ps.Emit(new ParticleSystem.EmitParams {
            position = p, velocity = Vector3.zero, startColor = color, startLifetime = life,
            startSize3D = new Vector3(size, size * aspect, size), randomSeed = (uint)random.Next(1, int.MaxValue)
        }, 1);
        ps.Play();
    }

    void Scatter(Vector3 p, Shape shape, int count, Color a, Color b, float size, float speed,
        float life, float radius, float gravity, float strength, bool upward = false)
    {
        var ps = Rent(shape, p, gravity, shape == Shape.Dust);
        for (int i = 0; i < count; i++)
        {
            // Stratified angles keep small bursts balanced without obvious identical spacing.
            float angle = (i + Range(0.15f, 0.85f)) / count * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            if (upward) dir.y = Mathf.Abs(dir.y) * 0.65f + 0.15f;
            float scale = size * Range(0.65f, 1.25f) * strength;
            ps.Emit(new ParticleSystem.EmitParams {
                position = p + new Vector3(dir.x, upward ? Range(0f, 0.3f) : dir.y, 0f) * radius,
                velocity = dir * speed * Range(0.35f, 1f) * strength,
                startLifetime = life * Range(0.7f, 1.2f), startColor = Color.Lerp(a, b, Range(0f, 1f)),
                startSize3D = new Vector3(scale, scale * (shape == Shape.Streak ? 0.18f : shape == Shape.Dust ? 0.65f : 1f), scale),
                rotation = shape == Shape.Streak ? -angle * Mathf.Rad2Deg : Range(-180f, 180f),
                randomSeed = (uint)random.Next(1, int.MaxValue)
            }, 1);
        }
        ps.Play();
    }

    ParticleSystem Rent(Shape shape, Vector3 position, float gravity, bool grows)
    {
        ParticleSystem ps = null;
        foreach (var candidate in pool) if (!candidate.IsAlive(true)) { ps = candidate; break; }
        if (ps == null && pool.Count < PoolLimit)
        {
            var host = new GameObject("Feedback layer");
            host.transform.SetParent(transform, false);
            ps = host.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)random.Next(1, int.MaxValue);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 2f;
            main.startSize3D = true;
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = ps.emission; emission.enabled = false;
            var shapeModule = ps.shape; shapeModule.enabled = false;
            var color = ps.colorOverLifetime; color.enabled = true; color.color = fade;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 2f;
            pool.Add(ps);
        }
        if (ps == null) { ps = pool[poolCursor]; poolCursor = (poolCursor + 1) % pool.Count; }
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.transform.SetPositionAndRotation(position, Quaternion.identity);
        var settings = ps.main; settings.gravityModifier = gravity;
        var sizeModule = ps.sizeOverLifetime;
        sizeModule.enabled = true;
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, grows ? expand : shrink);
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = shape == Shape.Leaf || shape == Shape.Star;
        rotation.z = shape == Shape.Leaf ? 2.2f : 0.45f;
        var render = ps.GetComponent<ParticleSystemRenderer>();
        render.sharedMaterial = materials[(int)shape];
        render.sortingOrder = 50 + (shape == Shape.Glow || shape == Shape.Dust ? 0 : 1);
        return ps;
    }

    float Range(float min, float max) { return min + (float)random.NextDouble() * (max - min); }
}
