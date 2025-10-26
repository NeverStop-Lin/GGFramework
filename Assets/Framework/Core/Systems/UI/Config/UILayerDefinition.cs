using System;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// UI层级定义
    /// 定义一个UI层级的名称和基础排序值
    /// </summary>
    [Serializable]
    public class UILayerDefinition
    {
        /// <summary>
        /// 层级名称（如"Main"、"Popup"、"Top"）
        /// </summary>
        [Tooltip("层级名称，用于标识不同的UI层级")]
        public string LayerName;
        
        /// <summary>
        /// 基础排序值
        /// 该层级的UI将从此值开始分配sortingOrder
        /// </summary>
        [Tooltip("该层级的基础sortingOrder值，同层级UI会在此基础上递增")]
        public int BaseSortingOrder;
        
        /// <summary>
        /// 层级说明
        /// </summary>
        [Tooltip("层级说明，用于描述该层级的用途")]
        [TextArea(2, 4)]
        public string Description;

        /// <summary>
        /// 克隆层级定义
        /// </summary>
        public UILayerDefinition Clone()
        {
            return new UILayerDefinition
            {
                LayerName = this.LayerName,
                BaseSortingOrder = this.BaseSortingOrder,
                Description = this.Description
            };
        }
    }
}

