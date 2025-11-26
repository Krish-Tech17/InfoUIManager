using UnityEngine;
using UnityEditor;

public class PlacePrefabOnTopCenter : EditorWindow
{
    private InfoiconInteractable InfoIconPrefab;
    private float xOffset = 0f;
    private float yOffset = 0.1f;
    private float zOffset = 0f;
    private GameObject parentForInfoTag;

    [MenuItem("Tools/Place Prefab on Top Center")]
    public static void ShowWindow()
    {
        GetWindow<PlacePrefabOnTopCenter>("Place Prefab");
    }

    private void OnGUI()
    {
        GUILayout.Label("Info Icon Placement Tool", EditorStyles.boldLabel);

        InfoIconPrefab = (InfoiconInteractable)EditorGUILayout.ObjectField("Info Icon Prefab", InfoIconPrefab, typeof(InfoiconInteractable), false);
        parentForInfoTag = (GameObject)EditorGUILayout.ObjectField("Info Tag Parent", parentForInfoTag, typeof(GameObject), true);

        GUILayout.Space(10);

        xOffset = EditorGUILayout.FloatField("X Offset", xOffset);
        yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);
        zOffset = EditorGUILayout.FloatField("Z Offset", zOffset);

        GUILayout.Space(10);

        if (GUILayout.Button("Place Info Icons on Lowest Mesh Child"))
        {
            if (InfoIconPrefab == null)
            {
                Debug.LogError("Please assign InfoIconPrefab.");
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
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            GameObject meshObj = r.gameObject;

            bool hasChildRenderer = false;
            foreach (Transform t in meshObj.transform)
            {
                if (t.GetComponentInChildren<Renderer>() != null)
                {
                    hasChildRenderer = true;
                    break;
                }
            }
            if (hasChildRenderer) continue;

            Renderer[] meshRenderers = meshObj.GetComponentsInChildren<Renderer>();


            if (meshRenderers == null || meshRenderers.Length == 0)
            {
                Debug.LogWarning("No Renderer found in " + meshObj.name);
                return; // skip this object
            }

            Bounds bounds = meshRenderers[0].bounds;
            foreach (Renderer mr in meshRenderers)
            {
                if (mr != null)
                    bounds.Encapsulate(mr.bounds);
            }

            Vector3 topCenter = new Vector3(
                bounds.center.x + xOffset,
                bounds.max.y + yOffset,
                bounds.center.z + zOffset
            );

            // Instantiate Info Icon Prefab
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(InfoIconPrefab.gameObject);
            Undo.RegisterCreatedObjectUndo(instance, "Place Info Icon");
            instance.transform.position = topCenter;

            // Set parent
            if (parentForInfoTag != null)
                instance.transform.SetParent(parentForInfoTag.transform, true);
            else
                instance.transform.SetParent(meshObj.transform, true);

            // Update info_Content
            InfoiconInteractable info = instance.GetComponent<InfoiconInteractable>();
            if (info != null)
            {
                Undo.RecordObject(info, "Assign Info Content");
                info.info_Content = meshObj.name;
                PrefabUtility.RecordPrefabInstancePropertyModifications(info);
                EditorUtility.SetDirty(info);
            }

            Debug.Log("Placed Info Icon on: " + meshObj.name);
        }
    }
}
