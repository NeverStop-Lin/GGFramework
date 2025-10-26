using System;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// UI项目配置管理器
    /// 负责加载和访问UIProjectConfig配置
    /// </summary>
    public static class UIProjectConfigManager
    {
        private static UIProjectConfig _config;
        private static string _configPath = "Config/UIProjectConfig";
        
        /// <summary>
        /// 获取UI项目配置
        /// </summary>
        public static UIProjectConfig GetConfig()
        {
            if (_config == null)
            {
                _config = Resources.Load<UIProjectConfig>(_configPath);
                
#if UNITY_EDITOR
                if (_config == null)
                {
                    FrameworkLogger.Warn($"[UIProjectConfig] 未找到UI项目配置文件，路径: Resources/{_configPath}");
                    FrameworkLogger.Warn($"[UIProjectConfig] 请通过 Tools/Framework/UI Manager 创建配置文件");
                }
#endif
            }
            
            return _config;
        }
        
        /// <summary>
        /// 获取层级定义
        /// </summary>
        public static UILayerDefinition GetLayerDefinition(string layerName)
        {
            var config = GetConfig();
            return config?.GetLayerDefinition(layerName);
        }
        
        /// <summary>
        /// 获取基础排序值
        /// </summary>
        public static int GetBaseSortingOrder(string layerName)
        {
            var config = GetConfig();
            if (config == null)
            {
                FrameworkLogger.Warn($"[UIProjectConfig] 配置文件不存在，返回默认sortingOrder=0");
                return 0;
            }
            
            var layer = config.GetLayerDefinition(layerName);
            if (layer == null)
            {
                FrameworkLogger.Warn($"[UIProjectConfig] 未找到层级定义: {layerName}，返回默认sortingOrder=0");
                return 0;
            }
            
            return layer.BaseSortingOrder;
        }
        
        /// <summary>
        /// 获取UI实例配置
        /// </summary>
        public static UIInstanceConfig GetUIInstanceConfig(string uiName)
        {
            var config = GetConfig();
            return config?.GetUIConfig(uiName);
        }
        
        /// <summary>
        /// 获取UI实例配置（通过类型）
        /// </summary>
        public static UIInstanceConfig GetUIInstanceConfig(Type uiType)
        {
            return GetUIInstanceConfig(uiType.Name);
        }
        
        
        /// <summary>
        /// 设置配置文件路径（Editor使用）
        /// </summary>
        public static void SetConfigPath(string path)
        {
            _configPath = path;
            _config = null; // 重新加载
        }
        
        /// <summary>
        /// 重新加载配置
        /// </summary>
        public static void Reload()
        {
            _config = null;
            GetConfig();
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 获取当前配置路径（Editor使用）
        /// </summary>
        public static string GetConfigPath()
        {
            return _configPath;
        }
        
        /// <summary>
        /// 设置配置实例（Editor使用）
        /// </summary>
        public static void SetConfig(UIProjectConfig config)
        {
            _config = config;
        }
#endif
    }
}

