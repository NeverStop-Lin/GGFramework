using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// UI项目配置
    /// 统一管理项目中的UI层级定义和UI实例配置
    /// </summary>
    [CreateAssetMenu(fileName = "UIProjectConfig", menuName = "Framework/UI/UI Project Config")]
    public class UIProjectConfig : ScriptableObject
    {
        [Header("Canvas设计尺寸")]
        [SerializeField]
        [Tooltip("Canvas参考分辨率宽度")]
        private int _referenceResolutionWidth = 1280;
        
        [SerializeField]
        [Tooltip("Canvas参考分辨率高度")]
        private int _referenceResolutionHeight = 720;
        
        [SerializeField]
        [Tooltip("屏幕匹配模式（0=宽度，0.5=平衡，1=高度）")]
        [Range(0f, 1f)]
        private float _matchWidthOrHeight = 1f;
        
        [Header("UI层级配置")]
        [SerializeField]
        [Tooltip("UI层级定义列表")]
        private List<UILayerDefinition> _layerDefinitions = new List<UILayerDefinition>();
        
        [SerializeField]
        [Tooltip("UI实例配置列表")]
        private List<UIInstanceConfig> _uiConfigs = new List<UIInstanceConfig>();
        
        /// <summary>
        /// Canvas参考分辨率宽度
        /// </summary>
        public int ReferenceResolutionWidth 
        { 
            get => _referenceResolutionWidth; 
            set => _referenceResolutionWidth = value; 
        }
        
        /// <summary>
        /// Canvas参考分辨率高度
        /// </summary>
        public int ReferenceResolutionHeight 
        { 
            get => _referenceResolutionHeight; 
            set => _referenceResolutionHeight = value; 
        }
        
        /// <summary>
        /// 屏幕匹配模式
        /// </summary>
        public float MatchWidthOrHeight 
        { 
            get => _matchWidthOrHeight; 
            set => _matchWidthOrHeight = value; 
        }
        
        /// <summary>
        /// 层级定义列表
        /// </summary>
        public List<UILayerDefinition> LayerDefinitions => _layerDefinitions;
        
        /// <summary>
        /// UI实例配置列表
        /// </summary>
        public List<UIInstanceConfig> UIConfigs => _uiConfigs;
        
        #region 层级管理
        
        /// <summary>
        /// 获取层级定义
        /// </summary>
        public UILayerDefinition GetLayerDefinition(string layerName)
        {
            return _layerDefinitions.FirstOrDefault(l => l.LayerName == layerName);
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
            var existing = _layerDefinitions.FirstOrDefault(l => l.LayerName == layerDef.LayerName);
            if (existing != null)
            {
                existing.BaseSortingOrder = layerDef.BaseSortingOrder;
                existing.Description = layerDef.Description;
            }
            else
            {
                _layerDefinitions.Add(layerDef.Clone());
            }
            
            SortLayers();
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// 移除层级定义
        /// </summary>
        public void RemoveLayer(string layerName)
        {
            _layerDefinitions.RemoveAll(l => l.LayerName == layerName);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// 检查层级名是否存在
        /// </summary>
        public bool HasLayer(string layerName)
        {
            return _layerDefinitions.Any(l => l.LayerName == layerName);
        }
        
        /// <summary>
        /// 按BaseSortingOrder排序层级
        /// </summary>
        private void SortLayers()
        {
            _layerDefinitions.Sort((a, b) => a.BaseSortingOrder.CompareTo(b.BaseSortingOrder));
        }
        
        #endregion
        
        #region UI配置管理
        
        /// <summary>
        /// 获取UI实例配置
        /// </summary>
        public UIInstanceConfig GetUIConfig(string uiName)
        {
            return _uiConfigs.FirstOrDefault(c => c.UIName == uiName);
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
            var existing = _uiConfigs.FirstOrDefault(c => c.UIName == config.UIName);
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
                _uiConfigs.Add(config.Clone());
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// 移除UI配置
        /// </summary>
        public void RemoveUIConfig(string uiName)
        {
            _uiConfigs.RemoveAll(c => c.UIName == uiName);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// 获取所有预加载的UI
        /// </summary>
        public List<UIInstanceConfig> GetPreloadUIConfigs()
        {
            return _uiConfigs.Where(c => c.Preload).ToList();
        }
        
        /// <summary>
        /// 获取指定层级的所有UI
        /// </summary>
        public List<UIInstanceConfig> GetUIConfigsByLayer(string layerName)
        {
            return _uiConfigs.Where(c => c.LayerName == layerName).ToList();
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        public void CreateDefaultLayers()
        {
            if (_layerDefinitions.Count > 0)
            {
                return;
            }
            
            _layerDefinitions.Add(new UILayerDefinition
            {
                LayerName = "Main",
                BaseSortingOrder = 0,
                Description = "主界面层级，用于全屏UI"
            });
            
            _layerDefinitions.Add(new UILayerDefinition
            {
                LayerName = "Popup",
                BaseSortingOrder = 1000,
                Description = "弹窗层级，用于弹出式UI"
            });
            
            _layerDefinitions.Add(new UILayerDefinition
            {
                LayerName = "Top",
                BaseSortingOrder = 2000,
                Description = "顶层层级，用于始终显示在最上层的UI"
            });
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        #endregion
    }
}

