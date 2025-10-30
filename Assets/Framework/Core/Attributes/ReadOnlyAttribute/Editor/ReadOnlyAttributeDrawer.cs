using UnityEditor;
using UnityEngine;

namespace Framework.Core.Attributes
{
    /// <summary>
    /// ReadOnly 特性的自定义绘制器
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 保存当前的 GUI 启用状态
            bool previousEnabled = GUI.enabled;
            
            // 禁用 GUI 编辑
            GUI.enabled = false;
            
            // 绘制属性（支持嵌套属性）
            EditorGUI.PropertyField(position, property, label, true);
            
            // 恢复 GUI 启用状态
            GUI.enabled = previousEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 返回属性的实际高度（支持嵌套属性）
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}

