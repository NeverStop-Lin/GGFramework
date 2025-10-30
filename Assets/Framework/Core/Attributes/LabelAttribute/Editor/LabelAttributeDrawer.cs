using UnityEditor;
using UnityEngine;

namespace Framework.Core.Attributes
{
    /// <summary>
    /// Label 特性的自定义绘制器
    /// </summary>
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LabelAttribute labelAttribute = (LabelAttribute)attribute;
            
            // 创建新的 GUIContent，显示中文标签，Tooltip 显示原始字段名
            GUIContent customLabel = new GUIContent(labelAttribute.Label, label.text);
            
            // 使用默认的属性绘制
            EditorGUI.PropertyField(position, property, customLabel, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}

