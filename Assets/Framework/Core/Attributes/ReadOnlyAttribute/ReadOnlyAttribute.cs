using UnityEngine;

namespace Framework.Core.Attributes
{
    /// <summary>
    /// 只读特性，用于在 Inspector 中显示但禁止编辑
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
        /// <summary>
        /// 创建只读特性
        /// </summary>
        public ReadOnlyAttribute()
        {
        }
    }
}

