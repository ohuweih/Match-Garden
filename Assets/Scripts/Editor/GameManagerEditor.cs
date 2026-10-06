using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public sealed class GameManagerEditor : Editor
{
    private bool showSceneSettings;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("boardView"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("inputController"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("levelCatalog"));
        bool hasCatalog = serializedObject.FindProperty("levelCatalog").objectReferenceValue != null;
        using (new EditorGUI.DisabledScope(hasCatalog))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("levelDefinition"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fitCameraToBoard"));
        bool hasLevel = hasCatalog || serializedObject.FindProperty("levelDefinition").objectReferenceValue != null;
        if (hasLevel)
        {
            EditorGUILayout.HelpBox(hasCatalog
                ? "Campaign mode loads the first unfinished level and saves each win. Clear Level Catalog for single-level testing."
                : "The assigned level supplies gameplay rules when Play Mode starts. Camera and animation settings remain on the scene components.", MessageType.Info);
            showSceneSettings = EditorGUILayout.Foldout(showSceneSettings, "Saved scene settings (fallback)", true);
        }
        if (!hasLevel || showSceneSettings)
            using (new EditorGUI.DisabledScope(hasLevel))
                DrawPropertiesExcluding(serializedObject, "m_Script", "boardView", "inputController", "levelDefinition", "levelCatalog", "fitCameraToBoard");
        serializedObject.ApplyModifiedProperties();

        using (new EditorGUI.DisabledScope(Application.isPlaying || targets.Length != 1))
        {
            if (GUILayout.Button("Create Level From Scene Settings")) CreateFromScene();
            if (GUILayout.Button("Reset All Campaign Progress")) ResetProgress();
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Campaign progress files");
        EditorGUILayout.SelectableLabel(
            System.IO.Path.Combine(GameManager.ProgressDirectory, GameManager.GetCampaignSaveFileName(RefillMode.Classic)) + "\n" +
            System.IO.Path.Combine(GameManager.ProgressDirectory, GameManager.GetCampaignSaveFileName(RefillMode.Normal)) + "\n" +
            System.IO.Path.Combine(GameManager.ProgressDirectory, GameManager.GetCampaignSaveFileName(RefillMode.Chaos)),
            GUILayout.Height(54));
        if (GUILayout.Button("Open Save Folder"))
        {
            System.IO.Directory.CreateDirectory(GameManager.ProgressDirectory);
            EditorUtility.RevealInFinder(GameManager.ProgressDirectory);
        }
    }

    private static void ResetProgress()
    {
        if (!EditorUtility.DisplayDialog("Reset All Campaign Progress?",
            "Delete Classic, Normal, and Chaos completed-level progress, banked specials, and backups?",
            "Reset All", "Cancel")) return;

        bool success = true;
        foreach (RefillMode mode in new[] { RefillMode.Classic, RefillMode.Normal, RefillMode.Chaos })
        {
            var store = new ProgressFileStore(
                GameManager.ProgressDirectory,
                GameManager.GetCampaignSaveFileName(mode));

            if (!store.TryReset(out var error))
            {
                success = false;
                Debug.LogError($"[Save] Could not reset {mode} campaign: {error}");
            }
        }

        if (success) Debug.Log("[Save] Classic, Normal, and Chaos campaign progress reset.");
    }

    private void CreateFromScene()
    {
        var manager = (GameManager)target;
        var settings = manager.CaptureSceneSettings();
        var errors = LevelBuilder.Validate(settings);
        if (errors.Count > 0)
        {
            Debug.LogError("Cannot create level:\n" + string.Join("\n", errors), manager);
            return;
        }
        string path = EditorUtility.SaveFilePanelInProject("Save Level Definition", "NewLevel", "asset",
            "Choose where to save the current scene's gameplay settings.");
        if (string.IsNullOrEmpty(path)) return;
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        var asset = CreateInstance<LevelDefinition>();
        asset.SetSettings(settings);
        AssetDatabase.CreateAsset(asset, path);
        LevelIdentity.Ensure(asset);
        AssetDatabase.SaveAssets();
        serializedObject.Update();
        serializedObject.FindProperty("levelDefinition").objectReferenceValue = asset;
        serializedObject.ApplyModifiedProperties();
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"[Level] Created and assigned {path}. Save the scene to keep the assignment.", manager);
    }
}

[CustomEditor(typeof(LevelDefinition))]
public sealed class LevelDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var settings = serializedObject.FindProperty("settings");
        var iterator = settings.Copy();
        var end = settings.GetEndProperty();
        if (iterator.NextVisible(true))
            while (!SerializedProperty.EqualContents(iterator, end))
            {
                EditorGUILayout.PropertyField(iterator, true);
                if (!iterator.NextVisible(false)) break;
            }
        serializedObject.ApplyModifiedProperties();
        var level = (LevelDefinition)target;
        foreach (var error in LevelBuilder.Validate(level.CreateSettings()))
            EditorGUILayout.HelpBox(error, MessageType.Error);
        if (GUILayout.Button("Validate Level"))
        {
            var snapshot = level.CreateSettings();
            var errors = LevelBuilder.Validate(snapshot);
            if (errors.Count > 0) Debug.LogError(string.Join("\n", errors), level);
            else
            {
                int warnings = 0;
                LevelBuilder.Build(snapshot, message => { warnings++; Debug.LogWarning(message, level); });
                Debug.Log($"[Level] {level.name}: validation complete, {warnings} placement warning(s).", level);
            }
        }
    }
}
