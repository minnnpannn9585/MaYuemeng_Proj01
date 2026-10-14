using UnityEditor;
using UnityEngine;

public sealed class FeedbackPreviewWindow : EditorWindow
{
    InteractionCue cue = InteractionCue.Collect;
    bool sound = true;
    bool particles = true;
    float intensity = 1f;
    Vector2 scroll;

    [MenuItem("Tools/Forest/Feedback Preview")]
    static void Open() { GetWindow<FeedbackPreviewWindow>("Feedback Preview"); }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Forest · Interaction Feedback", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enter Play mode. Effects appear beside the player; the game scene is not modified. " +
            "Select InteractionFeedback in the Hierarchy to adjust master volume and effect scale.", MessageType.Info);
        sound = EditorGUILayout.Toggle("Sound", sound);
        particles = EditorGUILayout.Toggle("Particles", particles);
        intensity = EditorGUILayout.Slider("Intensity", intensity, 0.35f, 1.6f);
        cue = (InteractionCue)EditorGUILayout.EnumPopup("Cue", cue);
        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Play selected cue", GUILayout.Height(30))) Preview(cue);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (InteractionCue item in System.Enum.GetValues(typeof(InteractionCue)))
                if (GUILayout.Button(item.ToString())) Preview(item);
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("Show generated audio"))
        {
            var folder = AssetDatabase.LoadAssetAtPath<Object>("Assets/Resources/Feedback/Audio");
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }

    void Preview(InteractionCue item)
    {
        var player = FindFirstObjectByType<NewBehaviourScript>();
        var camera = Camera.main;
        Vector3 position = player != null ? player.transform.position + Vector3.right * 1.5f :
            camera != null ? new Vector3(camera.transform.position.x, camera.transform.position.y, 0f) : Vector3.zero;
        if ((item == InteractionCue.Jump || item == InteractionCue.Land || item == InteractionCue.PotImpact) &&
            player != null && player.groundCheck != null) position.y = player.groundCheck.position.y;
        InteractionFeedback.Play(item, position, sound, particles, intensity);
    }
}
