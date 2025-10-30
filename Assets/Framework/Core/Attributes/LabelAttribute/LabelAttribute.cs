using UnityEngine;

namespace Framework.Core.Attributes
{
    /// <summary>
    /// 自定义标签特性，用于在 Inspector 中显示中文标签
    /// </summary>
    public class LabelAttribute : PropertyAttribute
    {
        public string Label { get; private set; }
        public string Help { get; private set; }
        public bool ShowBilingual { get; private set; }

        /// <summary>
        /// 创建标签特性
        /// </summary>
        /// <param name="label">中文标签</param>
        /// <param name="help">帮助文本（可选）</param>
        /// <param name="showBilingual">显示模式：true=中文在上，false=英文在上</param>
        public LabelAttribute(string label, string help = null, bool showBilingual = false)
        {
            Label = label;
            Help = help;
            ShowBilingual = showBilingual;
        }
    }
}

