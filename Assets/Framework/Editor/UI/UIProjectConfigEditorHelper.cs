#if UNITY_EDITOR
using Framework.Core;
using UnityEngine;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI项目配置编辑器辅助类
    /// 提供编辑器专用的配置读写功能
    /// </summary>
    public static class UIProjectConfigEditorHelper
    {
        /// <summary>
        /// 获取配置对象（用于编辑）
        /// </summary>
        public static UIProjectConfig GetConfig()
        {
            var config = new UIProjectConfig
            {
                ReferenceResolutionWidth = UIProjectConfigData.ReferenceResolutionWidth,
                ReferenceResolutionHeight = UIProjectConfigData.ReferenceResolutionHeight,
                MatchWidthOrHeight = UIProjectConfigData.MatchWidthOrHeight,
                LayerDefinitions = UIProjectConfigData.GetLayerDefinitions(),
                UIConfigs = UIProjectConfigData.GetUIConfigs()
            };
            return config;
        }
        
        /// <summary>
        /// 保存配置（触发代码生成）
        /// </summary>
        public static void SaveConfig(UIProjectConfig config)
        {
            var settings = UIManagerSettings.Instance;
            if (settings == null)
            {
                Debug.LogError("[UIProjectConfigEditorHelper] UIManagerSettings 不存在");
                return;
            }
            
            if (string.IsNullOrEmpty(settings.ConfigCodeFilePath))
            {
                Debug.LogError("[UIProjectConfigEditorHelper] 配置代码文件路径为空，请先创建配置文件");
                return;
            }
            
            // 固定使用 Framework.Core 命名空间
            UIProjectConfigCodeGenerator.GenerateCode(config, settings.ConfigCodeFilePath, "Framework.Core");
            
            // 重新加载运行时配置
            UIProjectConfigManager.Reload();
        }
        
        /// <summary>
        /// 创建配置代码文件
        /// </summary>
        public static void CreateConfigCodeFile(string filePath)
        {
            // 创建默认配置
            var defaultConfig = new UIProjectConfig();
            defaultConfig.CreateDefaultLayers();
            
            // 生成代码（固定命名空间 Framework.Core）
            UIProjectConfigCodeGenerator.GenerateCode(defaultConfig, filePath, "Framework.Core");
            
            // 更新设置
            var settings = UIManagerSettings.Instance;
            if (settings != null)
            {
                settings.ConfigCodeFilePath = filePath;
                settings.Save();
            }
            
            Debug.Log($"[UIProjectConfigEditorHelper] 配置代码文件已创建: {filePath}");
        }
        
        /// <summary>
        /// 检查配置代码文件是否存在
        /// </summary>
        public static bool ConfigCodeFileExists()
        {
            var settings = UIManagerSettings.Instance;
            if (settings == null || string.IsNullOrEmpty(settings.ConfigCodeFilePath))
                return false;
                
            return System.IO.File.Exists(settings.ConfigCodeFilePath);
        }
        
        /// <summary>
        /// 获取配置代码文件路径
        /// </summary>
        public static string GetConfigCodeFilePath()
        {
            var settings = UIManagerSettings.Instance;
            return settings?.ConfigCodeFilePath ?? "";
        }
    }
}
#endif

