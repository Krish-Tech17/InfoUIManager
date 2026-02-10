using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class PlacePrefabOnTopCenterWithCSV : EditorWindow
{
    private InfoiconInteractable infoIconPrefab;
    private GameObject parentForInfoTag;

    private float xOffset = 0f;
    private float yOffset = 0.1f;
    private float zOffset = 0f;

    private TextAsset csvFile;

    // CSV: Tag in S3D -> Label in VR (unique for placement)
    private Dictionary<string, string> csvTagMap = new Dictionary<string, string>();

    // CSV duplicate tracking
    private Dictionary<string, int> csvDuplicateCount = new Dictionary<string, int>();

    // Scene tracking
    private Dictionary<string, int> sceneMatchCount = new Dictionary<string, int>();
    private List<string> missingModels = new List<string>();

    // Prevent multiple icons per ModelName
    private HashSet<string> placedModelNames = new HashSet<string>();

    [MenuItem("Tools/Place Prefab on Top Center (CSV Based)")]
    public static void ShowWindow()
    {
        GetWindow<PlacePrefabOnTopCenterWithCSV>("Place Prefab CSV");
    }

    private void OnGUI()
    {
        GUILayout.Label("Info Icon Placement Tool (CSV)", EditorStyles.boldLabel);

        infoIconPrefab = (InfoiconInteractable)EditorGUILayout.ObjectField(
            "Info Icon Prefab", infoIconPrefab, typeof(InfoiconInteractable), false);

        parentForInfoTag = (GameObject)EditorGUILayout.ObjectField(
            "Info Tag Parent", parentForInfoTag, typeof(GameObject), true);

        csvFile = (TextAsset)EditorGUILayout.ObjectField(
            "CSV File", csvFile, typeof(TextAsset), false);

        GUILayout.Space(10);

        xOffset = EditorGUILayout.FloatField("X Offset", xOffset);
        yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);
        zOffset = EditorGUILayout.FloatField("Z Offset", zOffset);

        GUILayout.Space(15);

        if (GUILayout.Button("Place Info Icons (CSV Match)"))
        {
            if (infoIconPrefab == null || csvFile == null)
            {
                Debug.LogError("Assign Info Icon Prefab and CSV file.");
                return;
            }

            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("Please select a root object.");
                return;
            }

            LoadCSV();
            ProcessMatchingMeshObjects(selected);
        }
    }

    // ---------------------------------------------------------
    // CSV Loading (with duplicate detection)
    // ---------------------------------------------------------
    private void LoadCSV()
    {
        csvTagMap.Clear();
        csvDuplicateCount.Clear();

        using (StringReader reader = new StringReader(csvFile.text))
        {
            bool isHeader = true;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (isHeader)
                {
                    isHeader = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] columns = line.Split(',');

                if (columns.Length < 2)
                    continue;

                string modelName = columns[0].Trim();
                string subTaskHeading = columns[1].Trim();

                if (string.IsNullOrEmpty(modelName))
                    continue;

                // Track CSV duplicates
                if (!csvDuplicateCount.ContainsKey(modelName))
                    csvDuplicateCount[modelName] = 1;
                else
                    csvDuplicateCount[modelName]++;

                // Keep only first entry for placement
                if (!csvTagMap.ContainsKey(modelName))
                    csvTagMap.Add(modelName, subTaskHeading);
            }
        }

        Debug.Log($"CSV Loaded: {csvTagMap.Count} unique ModelNames");
    }

    // ---------------------------------------------------------
    // Main Processing
    // ---------------------------------------------------------
    private void ProcessMatchingMeshObjects(GameObject root)
    {
        sceneMatchCount.Clear();
        missingModels.Clear();
        placedModelNames.Clear();

        foreach (string key in csvTagMap.Keys)
            sceneMatchCount[key] = 0;

        // Track unique logical parents per ModelName
        Dictionary<string, HashSet<Transform>> uniqueParents =
            new Dictionary<string, HashSet<Transform>>();

        Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in allRenderers)
        {
            Transform current = renderer.transform;
            Transform matchedRoot = null;

            // Walk UP to find the OUTERMOST matching parent
            while (current != null)
            {
                if (csvTagMap.ContainsKey(current.name))
                {
                    matchedRoot = current;
                    break;
                }
                current = current.parent;
            }

            if (matchedRoot == null)
                continue;

            // Init set
            if (!uniqueParents.ContainsKey(matchedRoot.name))
                uniqueParents[matchedRoot.name] = new HashSet<Transform>();

            // Add unique logical object
            if (!uniqueParents[matchedRoot.name].Add(matchedRoot))
                continue; // already counted

            sceneMatchCount[matchedRoot.name]++;

            // Place icon only once per model name
            if (placedModelNames.Contains(matchedRoot.name))
                continue;

            placedModelNames.Add(matchedRoot.name);

            Bounds bounds = renderer.bounds;

            Vector3 topCenter = new Vector3(
                bounds.center.x + xOffset,
                bounds.max.y + yOffset,
                bounds.center.z + zOffset
            );

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(infoIconPrefab.gameObject);

            Undo.RegisterCreatedObjectUndo(instance, "Place Info Icon");
            instance.transform.position = topCenter;

            instance.transform.SetParent(
                parentForInfoTag != null ? parentForInfoTag.transform : matchedRoot,
                true);

            InfoiconInteractable info = instance.GetComponent<InfoiconInteractable>();
            if (info != null)
            {
                Undo.RecordObject(info, "Assign Sub-Task Heading");
                info.info_Content = csvTagMap[matchedRoot.name];
            }
        }

        GenerateFinalReport();
    }

    // ---------------------------------------------------------
    // Reporting
    // ---------------------------------------------------------
    private void GenerateFinalReport()
    {
        int availableCount = 0;
        int missingCount = 0;
        //int sceneRepeatedTotal = 0;
        int csvRepeatedTotal = 0;

        Debug.Log("========== CSV vs Scene Report ==========");

        foreach (var kvp in sceneMatchCount)
        {
            if (kvp.Value > 0)
            {
                availableCount++;

                int repeated = kvp.Value - 1;
                if (repeated > 0)
                {
                    //sceneRepeatedTotal += repeated;
                    Debug.Log($"✔ AVAILABLE : {kvp.Key} (Found {kvp.Value}, Scene Repeated {repeated})");
                }
                else
                {
                    Debug.Log($"✔ AVAILABLE : {kvp.Key} (Found {kvp.Value})");
                }
            }
            else
            {
                missingCount++;
                missingModels.Add(kvp.Key);
            }
        }

        // CSV repeated count
        foreach (var kvp in csvDuplicateCount)
        {
            if (kvp.Value > 1)
                csvRepeatedTotal += (kvp.Value - 1);
        }

        Debug.Log("-----------------------------------------");
        Debug.Log($"Total CSV Tags         : {csvTagMap.Count + csvRepeatedTotal}");
        Debug.Log($"CSV Unique Entries     : {csvTagMap.Count}");
        Debug.Log($"Available in Scene     : {availableCount}");
        Debug.Log($"Not Available in Scene : {missingCount}");
        Debug.Log($"CSV Repeated Tags      : {csvRepeatedTotal}");

        if (missingModels.Count > 0)
        {
            Debug.LogWarning("Missing ModelNames:");
            foreach (string name in missingModels)
                Debug.LogWarning($"{name}");
        }

        foreach (var kvp in csvDuplicateCount)
        {
            if (kvp.Value > 1)
                Debug.LogWarning($"CSV DUPLICATE : {kvp.Key} appears {kvp.Value} times");
        }
    }
}
