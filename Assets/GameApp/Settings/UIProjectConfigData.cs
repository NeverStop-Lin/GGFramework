// ========================================
// 自动生成的UI项目配置数据
// 警告: 请勿手动修改此文件，所有修改将在重新生成时丢失
// ========================================

using System.Collections.Generic;
using Framework.Core;

namespace Framework.Core
{
    /// <summary>
    /// UI项目配置数据（自动生成）
    /// 通过 partial 方法实现配置注入
    /// </summary>
    public static partial class UIProjectConfigData
    {
        /// <summary>
        /// 提供外部分辨率配置
        /// </summary>
        static partial void GetResolutionExternal(ref int width, ref int height, ref float match)
        {
            width = 1280;
            height = 720;
            match = 1f;
        }

        /// <summary>
        /// 提供外部层级定义配置
        /// </summary>
        static partial void GetLayerDefinitionsExternal(ref List<UILayerDefinition> layers)
        {
            layers = new List<UILayerDefinition>
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
                },
            };
        }

        /// <summary>
        /// 提供外部UI实例配置
        /// </summary>
        static partial void GetUIConfigsExternal(ref List<UIInstanceConfig> configs)
        {
            configs = new List<UIInstanceConfig>
            {
            };
        }
    }
}
