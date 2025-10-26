using System;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// UI实例配置
    /// 定义单个UI的具体配置信息
    /// </summary>
    [Serializable]
    public class UIInstanceConfig
    {
        /// <summary>
        /// UI名称（类名）
        /// </summary>
        [Tooltip("UI类名")]
        public string UIName;
        
        /// <summary>
        /// 资源路径（相对于Resources或Addressables）
        /// </summary>
        [Tooltip("UI预制体的资源路径")]
        public string ResourcePath;
        
        /// <summary>
        /// 使用的层级名称
        /// </summary>
        [Tooltip("该UI所属的层级名称")]
        public string LayerName;
        
        /// <summary>
        /// 缓存策略
        /// </summary>
        [Tooltip("UI的缓存策略")]
        public UICacheStrategy CacheStrategy = UICacheStrategy.AlwaysCache;
        
        /// <summary>
        /// 是否预加载
        /// </summary>
        [Tooltip("是否在游戏启动时预加载")]
        public bool Preload = false;
        
        /// <summary>
        /// 是否使用遮罩
        /// </summary>
        [Tooltip("是否在UI下方显示遮罩")]
        public bool UseMask = false;

        /// <summary>
        /// 克隆配置
        /// </summary>
        public UIInstanceConfig Clone()
        {
            return new UIInstanceConfig
            {
                UIName = this.UIName,
                ResourcePath = this.ResourcePath,
                LayerName = this.LayerName,
                CacheStrategy = this.CacheStrategy,
                Preload = this.Preload,
                UseMask = this.UseMask
            };
        }
    }
}

