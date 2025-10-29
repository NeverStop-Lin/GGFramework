using System.Collections.Generic;

namespace Framework.Core
{
    /// <summary>
    /// UI项目配置数据
    /// 框架提供默认实现，外部生成文件可通过 partial 方法注入自定义配置
    /// </summary>
    public static partial class UIProjectConfigData
    {
        #region Partial 方法声明（供外部实现）
        
        /// <summary>
        /// 外部提供的分辨率配置
        /// </summary>
        static partial void GetResolutionExternal(ref int width, ref int height, ref float match);
        
        /// <summary>
        /// 外部提供的层级定义配置
        /// </summary>
        static partial void GetLayerDefinitionsExternal(ref List<UILayerDefinition> layers);
        
        /// <summary>
        /// 外部提供的UI实例配置
        /// </summary>
        static partial void GetUIConfigsExternal(ref List<UIInstanceConfig> configs);
        
        #endregion
        
        #region Canvas 设计尺寸
        
        /// <summary>
        /// Canvas参考分辨率宽度
        /// </summary>
        public static int ReferenceResolutionWidth
        {
            get
            {
                int width = 1280;  // 默认值
                int height = 720;
                float match = 1f;
                GetResolutionExternal(ref width, ref height, ref match);
                return width;
            }
        }
        
        /// <summary>
        /// Canvas参考分辨率高度
        /// </summary>
        public static int ReferenceResolutionHeight
        {
            get
            {
                int width = 1280;
                int height = 720;  // 默认值
                float match = 1f;
                GetResolutionExternal(ref width, ref height, ref match);
                return height;
            }
        }
        
        /// <summary>
        /// 屏幕匹配模式
        /// </summary>
        public static float MatchWidthOrHeight
        {
            get
            {
                int width = 1280;
                int height = 720;
                float match = 1f;  // 默认值
                GetResolutionExternal(ref width, ref height, ref match);
                return match;
            }
        }
        
        #endregion
        
        #region 层级定义
        
        /// <summary>
        /// 获取所有层级定义
        /// </summary>
        public static List<UILayerDefinition> GetLayerDefinitions()
        {
            // 默认层级定义
            var layers = new List<UILayerDefinition>
            {
                new UILayerDefinition
                {
                    LayerName = "Main",
                    BaseSortingOrder = 0,
                    Description = "主界面层级，用于全屏UI"
                },
                new UILayerDefinition
                {
                    LayerName = "Popup",
                    BaseSortingOrder = 1000,
                    Description = "弹窗层级，用于弹出式UI"
                },
                new UILayerDefinition
                {
                    LayerName = "Top",
                    BaseSortingOrder = 2000,
                    Description = "顶层层级，用于始终显示在最上层的UI"
                },
                new UILayerDefinition
                {
                    LayerName = "Frame",
                    BaseSortingOrder = 9900,
                    Description = "框架级别"
                }
            };
            
            // 允许外部扩展/覆盖
            GetLayerDefinitionsExternal(ref layers);
            
            return layers;
        }
        
        #endregion
        
        #region UI配置
        
        /// <summary>
        /// 获取所有UI实例配置
        /// </summary>
        public static List<UIInstanceConfig> GetUIConfigs()
        {
            // 默认为空列表
            var configs = new List<UIInstanceConfig>();
            
            // 允许外部扩展/覆盖
            GetUIConfigsExternal(ref configs);
            
            return configs;
        }
        
        #endregion
    }
}

