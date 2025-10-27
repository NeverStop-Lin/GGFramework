using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Core
{
    /// <summary>
    /// UI项目配置
    /// 统一管理项目中的UI层级定义和UI实例配置
    /// </summary>
    [Serializable]
    public class UIProjectConfig
    {
        /// <summary>
        /// Canvas参考分辨率宽度
        /// </summary>
        public int ReferenceResolutionWidth { get; set; } = 1280;
        
        /// <summary>
        /// Canvas参考分辨率高度
        /// </summary>
        public int ReferenceResolutionHeight { get; set; } = 720;
        
        /// <summary>
        /// 屏幕匹配模式
        /// </summary>
        public float MatchWidthOrHeight { get; set; } = 1f;
        
        /// <summary>
        /// 层级定义列表
        /// </summary>
        public List<UILayerDefinition> LayerDefinitions { get; set; } = new List<UILayerDefinition>();
        
        /// <summary>
        /// UI实例配置列表
        /// </summary>
        public List<UIInstanceConfig> UIConfigs { get; set; } = new List<UIInstanceConfig>();
        
        #region 层级管理
        
        /// <summary>
        /// 获取层级定义
        /// </summary>
        public UILayerDefinition GetLayerDefinition(string layerName)
        {
            return LayerDefinitions.FirstOrDefault(l => l.LayerName == layerName);
        }
        
        /// <summary>
        /// 获取基础排序值
        /// </summary>
        public int GetBaseSortingOrder(string layerName)
        {
            var layer = GetLayerDefinition(layerName);
            return layer?.BaseSortingOrder ?? 0;
        }
        
        /// <summary>
        /// 添加或更新层级定义
        /// </summary>
        public void AddOrUpdateLayer(UILayerDefinition layerDef)
        {
            var existing = LayerDefinitions.FirstOrDefault(l => l.LayerName == layerDef.LayerName);
            if (existing != null)
            {
                existing.BaseSortingOrder = layerDef.BaseSortingOrder;
                existing.Description = layerDef.Description;
            }
            else
            {
                LayerDefinitions.Add(layerDef.Clone());
            }
            
            SortLayers();
        }
        
        /// <summary>
        /// 移除层级定义
        /// </summary>
        public void RemoveLayer(string layerName)
        {
            LayerDefinitions.RemoveAll(l => l.LayerName == layerName);
        }
        
        /// <summary>
        /// 检查层级名是否存在
        /// </summary>
        public bool HasLayer(string layerName)
        {
            return LayerDefinitions.Any(l => l.LayerName == layerName);
        }
        
        /// <summary>
        /// 按BaseSortingOrder排序层级
        /// </summary>
        private void SortLayers()
        {
            LayerDefinitions.Sort((a, b) => a.BaseSortingOrder.CompareTo(b.BaseSortingOrder));
        }
        
        #endregion
        
        #region UI配置管理
        
        /// <summary>
        /// 获取UI实例配置
        /// </summary>
        public UIInstanceConfig GetUIConfig(string uiName)
        {
            return UIConfigs.FirstOrDefault(c => c.UIName == uiName);
        }
        
        /// <summary>
        /// 获取UI实例配置（通过类型）
        /// </summary>
        public UIInstanceConfig GetUIConfig(Type uiType)
        {
            return GetUIConfig(uiType.Name);
        }
        
        /// <summary>
        /// 添加或更新UI配置
        /// </summary>
        public void AddOrUpdateUIConfig(UIInstanceConfig config)
        {
            var existing = UIConfigs.FirstOrDefault(c => c.UIName == config.UIName);
            if (existing != null)
            {
                existing.ResourcePath = config.ResourcePath;
                existing.LayerName = config.LayerName;
                existing.CacheStrategy = config.CacheStrategy;
                existing.Preload = config.Preload;
                existing.UseMask = config.UseMask;
            }
            else
            {
                UIConfigs.Add(config.Clone());
            }
        }
        
        /// <summary>
        /// 移除UI配置
        /// </summary>
        public void RemoveUIConfig(string uiName)
        {
            UIConfigs.RemoveAll(c => c.UIName == uiName);
        }
        
        /// <summary>
        /// 获取所有预加载的UI
        /// </summary>
        public List<UIInstanceConfig> GetPreloadUIConfigs()
        {
            return UIConfigs.Where(c => c.Preload).ToList();
        }
        
        /// <summary>
        /// 获取指定层级的所有UI
        /// </summary>
        public List<UIInstanceConfig> GetUIConfigsByLayer(string layerName)
        {
            return UIConfigs.Where(c => c.LayerName == layerName).ToList();
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        public void CreateDefaultLayers()
        {
            if (LayerDefinitions.Count > 0)
            {
                return;
            }
            
            LayerDefinitions.Add(new UILayerDefinition
            {
                LayerName = "Main",
                BaseSortingOrder = 0,
                Description = "主界面层级，用于全屏UI"
            });
            
            LayerDefinitions.Add(new UILayerDefinition
            {
                LayerName = "Popup",
                BaseSortingOrder = 1000,
                Description = "弹窗层级，用于弹出式UI"
            });
            
            LayerDefinitions.Add(new UILayerDefinition
            {
                LayerName = "Top",
                BaseSortingOrder = 2000,
                Description = "顶层层级，用于始终显示在最上层的UI"
            });
        }
        
        #endregion
    }
}

