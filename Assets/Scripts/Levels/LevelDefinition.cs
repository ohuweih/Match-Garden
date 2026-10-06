using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Match 3/Level Definition")]
public sealed class LevelDefinition : ScriptableObject
{
    [SerializeField, HideInInspector] private string levelId;
    public string LevelId => levelId;

    [SerializeField] private LevelSettings settings = new LevelSettings();

    public LevelSettings CreateSettings() => settings == null ? new LevelSettings() : settings.Copy();

    // Used by the scene-to-asset editor command. Own all nested data after capture.
    public void SetSettings(LevelSettings source)
    {
        if (source == null) throw new System.ArgumentNullException(nameof(source));
        settings = source.Copy();
    }
}
