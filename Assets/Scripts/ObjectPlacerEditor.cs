using UnityEditor;
using UnityEngine;

// version 3.0

public class ObjectPlacerEditor : EditorWindow
{
    private GameObject selectedObject;
    private GameObject previewInstance;
    private bool isPlacing = false;

    [MenuItem("Tools/Object Placer")]
    public static void ShowWindow()
    {
        GetWindow<ObjectPlacerEditor>("Object Placer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select an object to place", EditorStyles.boldLabel);
        selectedObject = (GameObject)EditorGUILayout.ObjectField("Object to Place", selectedObject, typeof(GameObject), true);

        if (!isPlacing)
        {
            if (GUILayout.Button("Start Placing"))
            {
                if (selectedObject != null)
                {
                    isPlacing = true;
                    CreatePreviewInstance();
                    SceneView.duringSceneGui += OnSceneGUI;
                }
                else
                {
                    Debug.LogWarning("No object selected to place!");
                }
            }
        }
        else
        {
            if (GUILayout.Button("Stop Placing"))
            {
                StopPlacing();
            }
        }
    }

    private void CreatePreviewInstance()
    {
        if (previewInstance != null)
            DestroyImmediate(previewInstance);

        previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedObject);
        if (previewInstance == null)
            previewInstance = Instantiate(selectedObject);

        previewInstance.hideFlags = HideFlags.HideAndDontSave;
        previewInstance.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Disable unwanted physics
        foreach (var comp in previewInstance.GetComponentsInChildren<Collider>())
            comp.enabled = false;

        foreach (var comp in previewInstance.GetComponentsInChildren<Rigidbody>())
            DestroyImmediate(comp);

        // Clone materials for transparency
        foreach (var renderer in previewInstance.GetComponentsInChildren<Renderer>())
        {
            Material[] originalMats = renderer.sharedMaterials;
            Material[] transparentMats = new Material[originalMats.Length];

            for (int i = 0; i < originalMats.Length; i++)
            {
                Material original = originalMats[i];
                if (original != null)
                {
                    Material previewMat = new Material(original);
                    previewMat.shader = Shader.Find("Universal Render Pipeline/Lit");

                    // Set for transparency
                    previewMat.SetFloat("_Surface", 1); // Transparent
                    previewMat.SetFloat("_Blend", 0);   // Alpha blend
                    previewMat.SetFloat("_ZWrite", 0);
                    previewMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    previewMat.renderQueue = 3000;

                    Color color = previewMat.HasProperty("_BaseColor") ? previewMat.GetColor("_BaseColor") : Color.white;
                    color.a = 0.5f;
                    previewMat.SetColor("_BaseColor", color);

                    transparentMats[i] = previewMat;
                }
            }

            renderer.materials = transparentMats; // Safe: doesn't affect shared materials
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPlacing || previewInstance == null)
            return;

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            previewInstance.transform.position = hit.point;
        }
        else
        {
            previewInstance.transform.position = ray.origin + ray.direction * 5f;
        }

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            PlaceObject(previewInstance.transform.position);
            e.Use();
        }

        if ((e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) || e.commandName == "Cancel")
        {
            StopPlacing();
            e.Use();
        }

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 400, 30));
        GUI.color = Color.yellow;
        GUILayout.Label("Placing mode – Click to place, ESC to cancel", EditorStyles.boldLabel);
        GUI.color = Color.white;
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void PlaceObject(Vector3 position)
    {
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(selectedObject);
        if (newObj == null)
            newObj = Instantiate(selectedObject);

        Undo.RegisterCreatedObjectUndo(newObj, "Place Object");
        newObj.transform.position = position;
        Selection.activeGameObject = newObj;
    }

    private void StopPlacing()
    {
        isPlacing = false;
        SceneView.duringSceneGui -= OnSceneGUI;

        if (previewInstance != null)
            DestroyImmediate(previewInstance);
    }

    private void OnDisable()
    {
        StopPlacing();
    }
}