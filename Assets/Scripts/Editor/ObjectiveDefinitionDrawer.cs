using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ObjectiveDefinition))]
public sealed class ObjectiveDefinitionDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
        var type = (ObjectiveType)property.FindPropertyRelative("Type").intValue;
        int rows = type == ObjectiveType.ClearColor ? 4 : 3;
        return rows * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float step = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            row.y += step;
            var type = property.FindPropertyRelative("Type");
            EditorGUI.PropertyField(row, type);
            row.y += step;
            if ((ObjectiveType)type.intValue == ObjectiveType.ClearFrost)
                EditorGUI.LabelField(row, "Target", "All frost + frost machines (automatic)");
            else if ((ObjectiveType)type.intValue == ObjectiveType.DestroyFrostMachines)
                EditorGUI.LabelField(row, "Target", "All placed frost machines (automatic)");
            else
                EditorGUI.PropertyField(row, property.FindPropertyRelative("Target"));
            if ((ObjectiveType)type.intValue == ObjectiveType.ClearColor)
            {
                row.y += step;
                EditorGUI.PropertyField(row, property.FindPropertyRelative("Color"));
            }
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }
}
