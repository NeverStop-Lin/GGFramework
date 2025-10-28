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
    /// </summary>
    public static partial class UIProjectConfigData
    {
        #region Canvas 设计尺寸

        /// <summary>
        /// Canvas参考分辨率宽度
        /// </summary>
        public static int ReferenceResolutionWidth => 1280;

        /// <summary>
        /// Canvas参考分辨率高度
        /// </summary>
        public static int ReferenceResolutionHeight => 720;

        /// <summary>
        /// 屏幕匹配模式
        /// </summary>
        public static float MatchWidthOrHeight => 1f;

        #endregion

        #region 层级定义

        /// <summary>
        /// 获取所有层级定义
        /// </summary>
        public static List<UILayerDefinition> GetLayerDefinitions()
        {
            return new List<UILayerDefinition>
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

        #endregion

        #region UI配置

        /// <summary>
        /// 获取所有UI实例配置
        /// </summary>
        public static List<UIInstanceConfig> GetUIConfigs()
        {
            return new List<UIInstanceConfig>
            {
                new UIInstanceConfig
                {
                    UIName = "GameUI",
                    ResourcePath = "UI/GameUI",
                    LayerName = "Main",
                    CacheStrategy = UICacheStrategy.SmartCache,
                    Preload = false,
                    InstanceStrategy = UIInstanceStrategy.Singleton,
                    LogicScriptPath = "Assets/Game/Scripts/UI/GameUI.cs"
                },
                new UIInstanceConfig
                {
                    UIName = "MainMenuUI",
                    ResourcePath = "UI/MainMenuUI",
                    LayerName = "Main",
                    CacheStrategy = UICacheStrategy.SmartCache,
                    Preload = false,
                    InstanceStrategy = UIInstanceStrategy.Singleton,
                    LogicScriptPath = "Assets/Game/Scripts/UI/MainMenuUI.cs"
                },
                new UIInstanceConfig
                {
                    UIName = "GameOverUI",
                    ResourcePath = "UI/GameOverUI",
                    LayerName = "Main",
                    CacheStrategy = UICacheStrategy.SmartCache,
                    Preload = false,
                    InstanceStrategy = UIInstanceStrategy.Singleton,
                    LogicScriptPath = "Assets/Game/Scripts/UI/GameOverUI.cs"
                },
                new UIInstanceConfig
                {
                    UIName = "GameHUD",
                    ResourcePath = "UI/GameHUD",
                    LayerName = "Main",
                    CacheStrategy = UICacheStrategy.SmartCache,
                    Preload = false,
                    InstanceStrategy = UIInstanceStrategy.Singleton,
                    LogicScriptPath = "Assets/Game/Scripts/UI/GameHUD.cs"
                },
                new UIInstanceConfig
                {
                    UIName = "UI_001",
                    ResourcePath = "UI\\UI_001",
                    LayerName = "Popup",
                    CacheStrategy = UICacheStrategy.SmartCache,
                    Preload = false,
                    InstanceStrategy = UIInstanceStrategy.Singleton,
                    LogicScriptPath = "Assets/Game/Scripts/UI/UI_001.cs"
                },
            };
        }

        #endregion
    }
}
