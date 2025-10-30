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
        private const float VerticalSpacing = 6f;            // 属性块底部额外留白
        private const float SeparatorHeight = 1f;            // 分隔线高度
        private static readonly Color SeparatorColor = new Color(0f, 0f, 0f, 0.08f);

        private void DrawBilingualProperty(Rect position, SerializedProperty property, GUIContent label, LabelAttribute labelAttribute)
        {
            // 计算标签区域和属性区域
            float labelWidth = EditorGUIUtility.labelWidth;
            float topPadding = 2f;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            
            // 获取真正的字段名（英文）
            string fieldName = property.name;
            
            // 创建加粗样式（第一行标签）
            GUIStyle boldStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = EditorStyles.label.fontSize
            };
            
            // 创建小字体样式 - 更小的字体和更淡的颜色
            GUIStyle smallStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Normal,
                fontSize = Mathf.Max(8, EditorStyles.label.fontSize - 3),
                wordWrap = false  // 不换行
            };
            
            // 设置固定的淡色（禁用所有状态的颜色变化）
            Color fadedColor = EditorGUIUtility.isProSkin 
                ? new Color(0.7f, 0.7f, 0.7f, 0.6f)  // 深色主题
                : new Color(0.4f, 0.4f, 0.4f, 0.6f); // 浅色主题
            
            smallStyle.normal.textColor = fadedColor;
            smallStyle.hover.textColor = fadedColor;   // 禁用悬停颜色
            smallStyle.active.textColor = fadedColor;  // 禁用激活颜色
            smallStyle.focused.textColor = fadedColor; // 禁用焦点颜色
            
            // 根据 ShowBilingual 决定第一行显示中文还是英文
            string firstLineText = labelAttribute.ShowBilingual ? labelAttribute.Label : fieldName;
            string secondLineText = labelAttribute.ShowBilingual ? fieldName : labelAttribute.Label;
            
            // 第一行：标签（左侧）+ 输入框（右侧）
            Rect firstLineLabelRect = new Rect(position.x, position.y + topPadding, labelWidth, lineHeight);
            GUI.Label(firstLineLabelRect, firstLineText, boldStyle);
            
            Rect propertyRect = new Rect(position.x + labelWidth, position.y + topPadding, position.width - labelWidth, lineHeight);
            EditorGUI.PropertyField(propertyRect, property, GUIContent.none, false);
            
            // 第二行：另一个文本 + Help（占据整个宽度，单行不换行）
            float secondLineY = position.y + topPadding + lineHeight;
            Rect secondLineRect = new Rect(position.x, secondLineY, position.width, lineHeight);
            
            if (!string.IsNullOrEmpty(labelAttribute.Help))
            {
                // 组合文本：第二行文本 | 帮助文本
                string combinedText = secondLineText + " | " + labelAttribute.Help;
                GUI.Label(secondLineRect, combinedText, smallStyle);
            }
            else
            {
                // 没有帮助文本，只显示第二行文本
                GUI.Label(secondLineRect, secondLineText, smallStyle);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LabelAttribute labelAttribute = (LabelAttribute)attribute;

            // 为绘制区域预留底部分隔线空间
            Rect contentRect = new Rect(position.x, position.y, position.width, position.height - SeparatorHeight);

            // 使用双语布局（支持位置互换）
            DrawBilingualProperty(contentRect, property, label, labelAttribute);

            // 底部分隔线
            Rect sep = new Rect(position.x, position.y + position.height - SeparatorHeight, position.width, SeparatorHeight);
            EditorGUI.DrawRect(sep, SeparatorColor);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 固定两行高度：第一行（标签+输入框） + 第二行（小字）
            float twoLinesHeight = EditorGUIUtility.singleLineHeight * 2;
            float height = twoLinesHeight + 4f;

            // 统一在底部增加留白，避免属性块过于紧凑
            height += VerticalSpacing;
            return height;
        }
    }
}

