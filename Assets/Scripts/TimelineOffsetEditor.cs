#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelineOffsetEditor : EditorWindow
{
    private GameObject selectedCharacter;
    private TimelineAsset selectedTimeline;
    private TimelineClip selectedClip;

    private static Vector3? previewPosition = null;
    private Vector3 currentClipOffset = Vector3.zero;

    private static bool isActive = false;
    private bool applyToAllClips = true;

    [MenuItem("Tools/Timeline Clip Offset Tool")]
    public static void ShowWindow()
    {
        GetWindow<TimelineOffsetEditor>("Clip Offset Tool");

        if (!isActive)
        {
            SceneView.duringSceneGui += OnSceneGUI;
            isActive = true;
        }
    }

    private void OnDisable()
    {
        if (isActive)
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            isActive = false;
        }

        previewPosition = null;
        SceneView.RepaintAll();
    }

    void OnGUI()
    {
        GUILayout.Label("Modify Clip Transform Offset", EditorStyles.boldLabel);

        selectedCharacter = (GameObject)EditorGUILayout.ObjectField("Character", selectedCharacter, typeof(GameObject), true);
        selectedTimeline = (TimelineAsset)EditorGUILayout.ObjectField("Timeline Asset", selectedTimeline, typeof(TimelineAsset), false);

        EditorGUILayout.Space();
        applyToAllClips = EditorGUILayout.ToggleLeft("Apply offset to all clips", applyToAllClips);

        if (selectedTimeline != null && !applyToAllClips)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Select a Clip to Modify:");

            foreach (var track in selectedTimeline.GetOutputTracks())
            {
                if (track is AnimationTrack animationTrack)
                {
                    foreach (var clip in animationTrack.GetClips())
                    {
                        if (GUILayout.Button($"Clip: {clip.displayName}"))
                        {
                            selectedClip = clip;
                            UpdateCurrentOffsetDisplay();
                        }
                    }
                }
            }
        }

        if ((selectedClip != null && !applyToAllClips) || applyToAllClips)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Hold SHIFT and click in Scene view to apply the offset.\nA circle shows the click location.", MessageType.Info);

            EditorGUILayout.LabelField("Current Clip Offset:", EditorStyles.boldLabel);
            EditorGUILayout.Vector3Field("Position", currentClipOffset);
        }
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // Capture input to make handles interactive every frame
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            previewPosition = hit.point;
        }
        else
        {
            previewPosition = null;
        }

        if (previewPosition.HasValue)
        {
            Handles.color = new Color(1f, 0.5f, 0f, 0.7f);
            Handles.DrawSolidDisc(previewPosition.Value, Vector3.up, 0.3f);
        }

        if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
        {
            if (previewPosition.HasValue)
            {
                var window = GetWindow<TimelineOffsetEditor>();
                window.ApplyOffset(previewPosition.Value);
                previewPosition = null;
                SceneView.RepaintAll();
                e.Use();
            }
        }

        sceneView.Repaint(); // Ensures smooth updates
    }

    private void ApplyOffset(Vector3 newPosition)
    {
        if (selectedTimeline == null) return;

        Undo.RecordObject(selectedTimeline, "Apply Offset");

        if (applyToAllClips)
        {
            foreach (var track in selectedTimeline.GetOutputTracks())
            {
                if (track is AnimationTrack animationTrack)
                {
                    foreach (var clip in animationTrack.GetClips())
                    {
                        if (clip.asset is AnimationPlayableAsset animAsset)
                        {
                            animAsset.position = newPosition;
                        }
                    }
                }
            }

            currentClipOffset = newPosition;
            Debug.Log($"Applied offset to all clips: {newPosition}");
        }
        else
        {
            if (selectedClip != null && selectedClip.asset is AnimationPlayableAsset animAsset)
            {
                animAsset.position = newPosition;
                currentClipOffset = newPosition;
                Debug.Log($"Applied offset to selected clip: {newPosition}");
            }
            else
            {
                Debug.LogWarning("No clip selected or not an AnimationPlayableAsset.");
            }
        }

        EditorUtility.SetDirty(selectedTimeline);
    }

    private void UpdateCurrentOffsetDisplay()
    {
        if (selectedClip != null && selectedClip.asset is AnimationPlayableAsset animAsset)
        {
            currentClipOffset = animAsset.position;
        }
    }
}
#endif