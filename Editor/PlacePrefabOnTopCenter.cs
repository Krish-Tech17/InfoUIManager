using UnityEngine;
using UnityEditor;

public class PlacePrefabOnTopCenter : EditorWindow
{
    private GameObject prefab;
    private float xOffset = 0f;
    private float yOffset = 0.1f;
    private float zOffset = 0f;
    private GameObject parentRoot;

    [MenuItem("Tools/Place Prefab on Top Center")]
    public static void ShowWindow()
    {
        GetWindow<PlacePrefabOnTopCenter>("Place Prefab");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Placement Tool", EditorStyles.boldLabel);

        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
        parentRoot = (GameObject)EditorGUILayout.ObjectField("Parent Root", parentRoot, typeof(GameObject), true);

        GUILayout.Space(10);

        xOffset = EditorGUILayout.FloatField("X Offset", xOffset);
        yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);
        zOffset = EditorGUILayout.FloatField("Z Offset", zOffset);

        GUILayout.Space(10);

        if (GUILayout.Button("Place Prefabs on Lowest Mesh Child"))
        {
            if (prefab == null)
            {
                Debug.LogError("Please assign a prefab.");
                return;
            }

            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("Please select a root object.");
                return;
            }

            ProcessLowestMeshChild(selected);
        }
    }

    private void ProcessLowestMeshChild(GameObject obj)
    {
        // Find all children recursively that have a Renderer
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            // Only consider the Renderer’s root object (lowest child with mesh)
            GameObject meshObj = r.gameObject;

            // Check if it is a “lowest mesh child” (i.e., no child has a Renderer)
            bool hasChildRenderer = false;
            foreach (Transform t in meshObj.transform)
            {
                if (t.GetComponentInChildren<Renderer>() != null)
                {
                    hasChildRenderer = true;
                    break;
                }
            }
            if (hasChildRenderer) continue; // Skip, not the lowest

            // Compute bounds
            Renderer[] meshRenderers = meshObj.GetComponentsInChildren<Renderer>();
            Bounds bounds = meshRenderers[0].bounds;
            foreach (Renderer mr in meshRenderers)
                bounds.Encapsulate(mr.bounds);

            Vector3 topCenter = new Vector3(
                bounds.center.x + xOffset,
                bounds.max.y + yOffset,
                bounds.center.z + zOffset
            );

            // Instantiate prefab
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Place Prefab");
            instance.transform.position = topCenter;

            // Parent prefab under parentRoot if assigned, otherwise under meshObj
            if (parentRoot != null)
                instance.transform.SetParent(parentRoot.transform, true);
            else
                instance.transform.SetParent(meshObj.transform, true);

            // Assign info_Content
            InfoiconInteractable info = instance.GetComponent<InfoiconInteractable>();
            if (info != null)
            {
                Undo.RecordObject(info, "Assign Info Content");
                info.info_Content = meshObj.name;
                PrefabUtility.RecordPrefabInstancePropertyModifications(info);
                EditorUtility.SetDirty(info);
            }

            Debug.Log("Placed prefab on: " + meshObj.name);
        }
    }
}
