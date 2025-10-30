using UnityEngine;

namespace Framework.Core.Attributes
{
    /// <summary>
    /// 自定义标签特性，用于在 Inspector 中显示中文标签，鼠标悬停显示英文字段名
    /// </summary>
    public class LabelAttribute : PropertyAttribute
    {
        public string Label { get; private set; }

        public LabelAttribute(string label)
        {
            Label = label;
        }
    }
}

