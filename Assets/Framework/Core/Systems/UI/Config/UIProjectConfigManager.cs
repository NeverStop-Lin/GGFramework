using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Core
{
    /// <summary>
    /// UI项目配置管理器
    /// 从生成的代码中读取UI配置数据
    /// </summary>
    public static class UIProjectConfigManager
    {
        private static bool _initialized = false;
        private static List<UILayerDefinition> _layerDefinitions;
        private static List<UIInstanceConfig> _uiConfigs;
        private static Dictionary<string, UIInstanceConfig> _uiConfigCache;
        
        /// <summary>
        /// 初始化配置数据（懒加载）
        /// </summary>
        private static void Initialize()
        {
            if (_initialized)
                return;
                
            _layerDefinitions = UIProjectConfigData.GetLayerDefinitions();
            _uiConfigs = UIProjectConfigData.GetUIConfigs();
            _uiConfigCache = _uiConfigs.ToDictionary(c => c.UIName);
            _initialized = true;
        }
        
        /// <summary>
        /// 获取Canvas参考分辨率宽度
        /// </summary>
        public static int GetReferenceResolutionWidth()
        {
            return UIProjectConfigData.ReferenceResolutionWidth;
        }
        
        /// <summary>
        /// 获取Canvas参考分辨率高度
        /// </summary>
        public static int GetReferenceResolutionHeight()
        {
            return UIProjectConfigData.ReferenceResolutionHeight;
        }
        
        /// <summary>
        /// 获取屏幕匹配模式
        /// </summary>
        public static float GetMatchWidthOrHeight()
        {
            return UIProjectConfigData.MatchWidthOrHeight;
        }
        
        /// <summary>
        /// 获取层级定义
        /// </summary>
        public static UILayerDefinition GetLayerDefinition(string layerName)
        {
            Initialize();
            return _layerDefinitions.FirstOrDefault(l => l.LayerName == layerName);
        }
        
        /// <summary>
        /// 获取基础排序值
        /// </summary>
        public static int GetBaseSortingOrder(string layerName)
        {
            Initialize();
            var layer = _layerDefinitions.FirstOrDefault(l => l.LayerName == layerName);
            
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
            Initialize();
            return _uiConfigCache.TryGetValue(uiName, out var config) ? config : null;
        }
        
        /// <summary>
        /// 获取UI实例配置（通过类型）
        /// </summary>
        public static UIInstanceConfig GetUIInstanceConfig(Type uiType)
        {
            return GetUIInstanceConfig(uiType.Name);
        }
        
        /// <summary>
        /// 获取所有UI配置
        /// </summary>
        public static List<UIInstanceConfig> GetAllUIConfigs()
        {
            Initialize();
            return _uiConfigs;
        }
        
        /// <summary>
        /// 获取所有层级定义
        /// </summary>
        public static List<UILayerDefinition> GetAllLayerDefinitions()
        {
            Initialize();
            return _layerDefinitions;
        }
        
        /// <summary>
        /// 重新加载配置
        /// </summary>
        public static void Reload()
        {
            _initialized = false;
            _layerDefinitions = null;
            _uiConfigs = null;
            _uiConfigCache = null;
        }
        
    }
}


