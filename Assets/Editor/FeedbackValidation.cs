using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Optional batch smoke check: Unity -batchmode -projectPath ... -executeMethod FeedbackValidation.RunBatch
[InitializeOnLoad]
public static class FeedbackValidation
{
    const string Running = "Forest.FeedbackValidation";
    static FeedbackValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Running, false))
            {
                SessionState.SetBool(Running, false);
                var host = new GameObject("Feedback validation");
                Object.DontDestroyOnLoad(host);
                host.AddComponent<FeedbackValidationRunner>();
            }
        };
    }

    public static void RunBatch()
    {
        Directory.CreateDirectory("Logs/FeedbackQA");
        var shader = Resources.Load<Shader>("Feedback/FeedbackParticle");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Feedback shader failed import.");
        foreach (InteractionCue cue in Enum.GetValues(typeof(InteractionCue)))
        {
            if (cue == InteractionCue.LaserSpark) continue; // Deliberately silent sustained contact.
            var clip = Resources.Load<AudioClip>("Feedback/Audio/" + cue + "_1");
            if (clip == null || clip.channels != 2 || clip.frequency != 48000)
                throw new Exception("Missing or invalid audio: " + cue);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/1.unity");
        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }

    public static void DiagnoseThirdScene()
    {
        Directory.CreateDirectory("Logs/FeedbackQA");
        EditorSceneManager.OpenScene("Assets/Scenes/3.unity");
        SessionState.SetBool("Forest.DiagnoseScene", true);
        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class FeedbackValidationRunner : MonoBehaviour
{
    int errors;
    float startedAt;
    void OnEnable() { startedAt = Time.realtimeSinceStartup; Application.logMessageReceived += Log; }
    void OnDisable() { Application.logMessageReceived -= Log; }
    void Update()
    {
        if (Time.realtimeSinceStartup - startedAt > 120f)
        {
            File.WriteAllText("Logs/FeedbackQA/result.txt", "FAIL: validation timed out\n");
            EditorApplication.Exit(1);
        }
    }
    void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }

    IEnumerator Start()
    {
        yield return null;
        if (SessionState.GetBool("Forest.DiagnoseScene", false))
        {
            SessionState.SetBool("Forest.DiagnoseScene", false);
            yield return new WaitForSeconds(0.7f);
            var diagnosticCamera = Camera.main;
            var planes = GeometryUtility.CalculateFrustumPlanes(diagnosticCamera);
            var report = new System.Text.StringBuilder();
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                    report.AppendLine(renderer.name + " enabled=" + renderer.enabled + " color=" + renderer.color +
                        " bounds=" + renderer.bounds + " material=" + renderer.sharedMaterial + " sprite=" + renderer.sprite);
            File.WriteAllText("Logs/FeedbackQA/scene-3-renderers.txt", report.ToString());
            Capture(diagnosticCamera, "scene-3-direct");
            ScreenCapture.CaptureScreenshot("Logs/FeedbackQA/scene-3-screen.png");
            yield return new WaitForSeconds(0.2f);
            EditorApplication.Exit(0);
            yield break;
        }
        for (int scene = 1; scene <= 3; scene++)
        {
            if (scene > 1) { SceneManager.LoadScene(scene.ToString()); yield return null; yield return null; }
            yield return new WaitForSeconds(0.6f);
            var player = Object.FindFirstObjectByType<NewBehaviourScript>();
            Require(player != null, "Player missing");
            if (player != null)
            {
                Require(player.GetComponent<HiddenPlatformReveal>() == null, "Glow area hid the player");
                bool visible = false;
                foreach (var sprite in player.GetComponentsInChildren<SpriteRenderer>())
                    visible |= sprite.enabled && sprite.color.a > 0f;
                Require(visible, "Player has no visible sprite");
            }
            foreach (var hidden in Object.FindObjectsByType<HiddenPlatformReveal>(FindObjectsSortMode.None))
                Require(hidden.gameObject.layer == LayerMask.NameToLayer("HiddenPlatformReveal"), "Unexpected auto-hidden object: " + hidden.name);
            Vector3 origin = player != null ? player.transform.position : Vector3.zero;
            // Freeze game motion for deterministic screenshots, without pausing particle simulation.
            foreach (var body in Object.FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None)) body.simulated = false;
            if (player != null) player.enabled = false;
            foreach (var feedback in Object.FindObjectsByType<InteractionFeedback>(FindObjectsSortMode.None))
                foreach (var ps in feedback.GetComponentsInChildren<ParticleSystem>()) ps.Clear();
            var randomState = UnityEngine.Random.state;
            foreach (InteractionCue cue in Enum.GetValues(typeof(InteractionCue)))
                InteractionFeedback.Play(cue, origin, false, true);
            Require(JsonUtility.ToJson(randomState) == JsonUtility.ToJson(UnityEngine.Random.state), "VFX altered gameplay random state");
            var manager = Object.FindFirstObjectByType<InteractionFeedback>();
            Require(manager != null, "Feedback manager missing");
            Require(Object.FindObjectsByType<InteractionFeedback>(FindObjectsSortMode.None).Length == 1, "Duplicate feedback manager");
            var systems = manager.GetComponentsInChildren<ParticleSystem>();
            Require(systems.Length > 0 && systems.Length <= 56, "Particle pool out of bounds");
            foreach (var ps in systems)
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                Require(renderer.sharedMaterial != null && renderer.sharedMaterial.shader.isSupported, "Unsupported material");
            }
            for (int i = 0; i < 100; i++) InteractionFeedback.Play(InteractionCue.Collect, origin, false);
            Require(manager.GetComponentsInChildren<ParticleSystem>().Length <= 56, "Pool grew under load");
            foreach (var ps in manager.GetComponentsInChildren<ParticleSystem>())
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Actual in-engine stills: the foreground assets and camera come from each game scene.
            var camera = Camera.main;
            if (camera != null)
            {
                yield return null;
                Debug.Log("Feedback QA view: scene=" + scene + " player=" + origin + " camera=" + camera.transform.position);
                Capture(camera, "scene-" + scene + "-baseline");
                InteractionCue[] gallery = { InteractionCue.Land, InteractionCue.Bloom, InteractionCue.Collect, InteractionCue.TimeTravel };
                foreach (var cue in gallery)
                {
                    Vector3 p = origin + Vector3.right * 1.2f;
                    if (cue == InteractionCue.Land && player != null && player.groundCheck != null) p.y = player.groundCheck.position.y;
                    InteractionFeedback.Play(cue, p, false);
                    foreach (var ps in manager.GetComponentsInChildren<ParticleSystem>()) ps.Simulate(0.16f, false, false, true);
                    Capture(camera, "scene-" + scene + "-" + cue);
                    foreach (var ps in manager.GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            // A second collider callback in the same frame must not collect a diamond twice.
            var diamond = Object.FindFirstObjectByType<Diamond>();
            var playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
            if (diamond != null && playerCollider != null)
            {
                foreach (var ps in manager.GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                diamond.SendMessage("OnTriggerEnter2D", playerCollider);
                int first = ParticleCount(manager);
                diamond.SendMessage("OnTriggerEnter2D", playerCollider);
                Require(first > 0 && ParticleCount(manager) == first, "Diamond feedback duplicated or bypassed");
            }
            foreach (var voice in manager.GetComponents<AudioSource>()) voice.Stop();
            InteractionFeedback.Play(InteractionCue.Jump, origin, true, false);
            Require(PlayingVoices(manager) == 1, "Audio cue did not play");
            InteractionFeedback.Play(InteractionCue.Jump, origin, true, false);
            Require(PlayingVoices(manager) == 1, "Audio cooldown did not suppress repeated cue");
            foreach (var voice in manager.GetComponents<AudioSource>()) voice.Stop();
            yield return new WaitForSeconds(0.08f);
            InteractionFeedback.Play(InteractionCue.LaserHit, origin + Vector3.right * 500f, true, false);
            Require(PlayingVoices(manager) == 0, "Distant laser is audible");
            var state = UnityEngine.Random.state;
            InteractionFeedback.Play(InteractionCue.Jump, origin, true, false);
            Require(JsonUtility.ToJson(state) == JsonUtility.ToJson(UnityEngine.Random.state), "Audio altered gameplay random state");
            foreach (var voice in manager.GetComponents<AudioSource>()) voice.Stop();
            // Validate expiration after the longest cue, also after forced reuse.
            InteractionFeedback.Play(InteractionCue.TimeTravel, origin, false);
            foreach (var ps in manager.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Simulate(3f, false, false, true);
                Require(ps.particleCount == 0, "Particles did not expire");
            }
            Debug.Log("Feedback QA: scene " + scene + " passed pool, resources, randomness, expiration and capture checks.");
        }
        File.WriteAllText("Logs/FeedbackQA/result.txt", errors == 0 ? "PASS: three scenes, 18 cues, bounded pool, visual/audio random isolation, expiration, shader support, diamond deduplication, audio playback/cooldown/distance, glow registration and visible player.\n" : "FAIL: " + errors + " errors\n");
        EditorApplication.Exit(errors == 0 ? 0 : 1);
    }

    int ParticleCount(InteractionFeedback manager)
    {
        int count = 0;
        foreach (var ps in manager.GetComponentsInChildren<ParticleSystem>()) count += ps.particleCount;
        return count;
    }

    int PlayingVoices(InteractionFeedback manager)
    {
        int count = 0;
        foreach (var voice in manager.GetComponents<AudioSource>()) if (voice.isPlaying) count++;
        return count;
    }

    void Require(bool condition, string message)
    {
        if (!condition) Debug.LogError("Feedback QA: " + message);
    }

    void Capture(Camera camera, string name)
    {
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes("Logs/FeedbackQA/" + name + ".png", texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            Destroy(texture);
        }
    }
}
