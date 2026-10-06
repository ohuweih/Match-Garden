using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Asset GUIDs survive renames/moves; duplicated assets receive their own GUID.
[InitializeOnLoad]
public static class LevelIdentity
{
    static LevelIdentity() { EditorApplication.delayCall += RefreshAll; }

    public static void RefreshAll()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:LevelDefinition"))
            Ensure(AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid)));
    }

    public static void Ensure(LevelDefinition level)
    {
        if (level == null) return;
        string path = AssetDatabase.GetAssetPath(level);
        if (string.IsNullOrEmpty(path)) return;
        string id = AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(id) || level.LevelId == id) return;
        var serialized = new SerializedObject(level);
        serialized.FindProperty("levelId").stringValue = id;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(level);
        AssetDatabase.SaveAssetIfDirty(level);
    }
}

public sealed class LevelIdentityImporter : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        EditorApplication.delayCall += () =>
        {
            foreach (string path in imported)
                if (path.EndsWith(".asset")) LevelIdentity.Ensure(AssetDatabase.LoadAssetAtPath<LevelDefinition>(path));
            foreach (string path in moved)
                if (path.EndsWith(".asset")) LevelIdentity.Ensure(AssetDatabase.LoadAssetAtPath<LevelDefinition>(path));
        };
    }
}

public sealed class LevelIdentityBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) { LevelIdentity.RefreshAll(); }
}

[CustomEditor(typeof(LevelCatalog))]
public sealed class LevelCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        foreach (var error in ((LevelCatalog)target).Validate()) EditorGUILayout.HelpBox(error, MessageType.Error);
    }
}
