using Framework.CoreTools;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CustomSprite))] // 注意这里要对应类�?
[CanEditMultipleObjects]
public class CustomSpriteEditor : Editor
{
    private SerializedProperty currentOptionProp;

    void OnEnable()
    {
        // 必须与类中的字段名完全一�?
        currentOptionProp = serializedObject.FindProperty("currentOption");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 只显示我们的自定义选项
        EditorGUILayout.PropertyField(currentOptionProp);

        // 添加一个刷新按钮（可选）
        if (GUILayout.Button("Apply Option"))
        {
            foreach (CustomSprite sprite in targets)
            {
                sprite.ApplyOptionEffects();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}