using System;

namespace Framework.Core
{
    /// <summary>
    /// UI配置类
    /// 定义UI的各种属性和行为
    /// </summary>
    [Serializable]
    public class UIConfig
    {
        /// <summary>
        /// 资源路径（相对于Resources或Addressables）
        /// 例如："UI/MainMenu" 或 "UI/Popup/Reward"
        /// </summary>
        public string ResourcePath;
        
        /// <summary>
        /// UI类型（Main/Popup/Effect/Top）
        /// </summary>
        public UIType UIType = UIType.Main;
        
        /// <summary>
        /// 对齐方式
        /// </summary>
        public UIAlignType AlignType = UIAlignType.Center;
        
        /// <summary>
        /// 缓存策略
        /// </summary>
        public UICacheStrategy CacheStrategy = UICacheStrategy.AlwaysCache;
        
        /// <summary>
        /// 层级（sortingOrder基础值，实际值由UILayerManager自动分配）
        /// </summary>
        public int Layer = 0;
        
        /// <summary>
        /// 是否使用遮罩（通常用于Popup）
        /// </summary>
        public bool UseMask = false;
        
        /// <summary>
        /// 遮罩颜色（半透明黑色）
        /// </summary>
        public UnityEngine.Color MaskColor = new UnityEngine.Color(0, 0, 0, 0.5f);
        
        /// <summary>
        /// 是否使用动画
        /// </summary>
        public bool UseAnimation = false;
        
        /// <summary>
        /// 动画类型
        /// </summary>
        public UIAnimationType AnimationType = UIAnimationType.Fade;
        
        /// <summary>
        /// 动画持续时间（秒）
        /// </summary>
        public float AnimationDuration = 0.3f;
        
        /// <summary>
        /// 是否预加载
        /// </summary>
        public bool Preload = false;
        
        /// <summary>
        /// 克隆配置（用于合并）
        /// </summary>
        public UIConfig Clone()
        {
            return new UIConfig
            {
                ResourcePath = this.ResourcePath,
                UIType = this.UIType,
                AlignType = this.AlignType,
                CacheStrategy = this.CacheStrategy,
                Layer = this.Layer,
                UseMask = this.UseMask,
                MaskColor = this.MaskColor,
                UseAnimation = this.UseAnimation,
                AnimationType = this.AnimationType,
                AnimationDuration = this.AnimationDuration,
                Preload = this.Preload
            };
        }
    }
}
