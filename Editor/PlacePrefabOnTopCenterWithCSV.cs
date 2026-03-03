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

    // CSV: Tag in S3D -> Label in VR
    private Dictionary<string, string> csvTagMap = new Dictionary<string, string>();
    private Dictionary<string, int> csvDuplicateCount = new Dictionary<string, int>();

    // Scene tracking
    private Dictionary<string, int> sceneMatchCount = new Dictionary<string, int>();
    private HashSet<string> placedModelNames = new HashSet<string>();

    // Missing classification
    private List<string> meshNotAvailableModels = new List<string>();
    private List<string> notAvailableInSceneModels = new List<string>();

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
    // CSV Loading
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

                string modelName = "/" + columns[0].Trim();
                string subTaskHeading = columns[1].Trim();

                if (string.IsNullOrEmpty(modelName))
                    continue;

                if (!csvDuplicateCount.ContainsKey(modelName))
                    csvDuplicateCount[modelName] = 1;
                else
                    csvDuplicateCount[modelName]++;

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
        placedModelNames.Clear();
        meshNotAvailableModels.Clear();
        notAvailableInSceneModels.Clear();

        foreach (string key in csvTagMap.Keys)
            sceneMatchCount[key] = 0;

        Dictionary<string, HashSet<Transform>> uniqueParents =
            new Dictionary<string, HashSet<Transform>>();

        Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in allRenderers)
        {
            Transform current = renderer.transform;
            Transform matchedRoot = null;

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

            if (!uniqueParents.ContainsKey(matchedRoot.name))
                uniqueParents[matchedRoot.name] = new HashSet<Transform>();

            if (!uniqueParents[matchedRoot.name].Add(matchedRoot))
                continue;

            sceneMatchCount[matchedRoot.name]++;

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

            instance.name = csvTagMap[matchedRoot.name];

            InfoiconInteractable info = instance.GetComponent<InfoiconInteractable>();
            if (info != null)
            {
                Undo.RecordObject(info, "Assign Sub-Task Heading");
                info.info_Content = csvTagMap[matchedRoot.name];
            }
        }

        // FAST existence check
        HashSet<string> allTransformNames = new HashSet<string>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            allTransformNames.Add(t.name);

        foreach (var kvp in sceneMatchCount)
        {
            if (kvp.Value == 0)
            {
                if (allTransformNames.Contains(kvp.Key))
                    meshNotAvailableModels.Add(kvp.Key);
                else
                    notAvailableInSceneModels.Add(kvp.Key);
            }
        }

        GenerateFinalReport();
        GenerateResultCSV();
    }

    // ---------------------------------------------------------
    // Final Report
    // ---------------------------------------------------------
    private void GenerateFinalReport()
    {
        int availableCount = placedModelNames.Count;
        int meshNotAvailableCount = meshNotAvailableModels.Count;
        int notAvailableCount = notAvailableInSceneModels.Count;

        int csvRepeatedTotal = 0;
        foreach (var kvp in csvDuplicateCount)
        {
            if (kvp.Value > 1)
                csvRepeatedTotal += (kvp.Value - 1);
        }

        Debug.Log("========== CSV vs Scene Report ==========");
        Debug.Log("-----------------------------------------");
        Debug.Log($"Total CSV Tags           : {csvTagMap.Count + csvRepeatedTotal}");
        Debug.Log($"CSV Unique Entries       : {csvTagMap.Count}");
        Debug.Log($"Available in Scene       : {availableCount}");
        Debug.Log($"Mesh Not Available       : {meshNotAvailableCount}");
        Debug.Log($"Not Available in Scene   : {notAvailableCount}");
        Debug.Log($"CSV Repeated Tags        : {csvRepeatedTotal}");
        Debug.Log("-----------------------------------------");

        int validation =
            availableCount +
            meshNotAvailableCount +
            notAvailableCount;

        if (validation != csvTagMap.Count)
            Debug.LogError("⚠ Count mismatch detected!");
        else
            Debug.Log("✅ Counts perfectly matched.");
    }

    // ---------------------------------------------------------
    // Result CSV
    // ---------------------------------------------------------
    private void GenerateResultCSV()
    {
        if (csvFile == null)
            return;

        string originalPath = AssetDatabase.GetAssetPath(csvFile);
        string directory = Path.GetDirectoryName(originalPath);
        string newFilePath = Path.Combine(directory, "CSV_Result_Report.csv");

        using (StreamWriter writer = new StreamWriter(newFilePath))
        {
            writer.WriteLine("Tag in E3D,Label in VR,Status");

            foreach (var kvp in csvTagMap)
            {
                string modelName = kvp.Key;
                string label = kvp.Value;
                string status;

                if (placedModelNames.Contains(modelName))
                    status = "Available";
                else if (meshNotAvailableModels.Contains(modelName))
                    status = "Mesh Not Available";
                else
                    status = "Not Available in Scene";

                writer.WriteLine($"{modelName},{label},{status}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"✅ Result CSV Generated at: {newFilePath}");
    }
}