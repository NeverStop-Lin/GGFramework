using System;
using System.Collections.Generic;

namespace Framework.Core
{
    /// <summary>
    /// UI层级管理器
    /// 负责自动分配UI的sortingOrder，避免层级冲突
    /// </summary>
    public class UILayerManager
    {
        // 层级区间定义
        private const int MAIN_LAYER_START = 0;
        private const int MAIN_LAYER_END = 99;
        private const int POPUP_LAYER_START = 100;
        private const int POPUP_LAYER_END = 199;
        private const int TOP_LAYER_START = 200;
        private const int TOP_LAYER_END = 299;
        
        // 记录每个层级区间当前分配到的索引
        private int _mainLayerIndex = MAIN_LAYER_START;
        private int _popupLayerIndex = POPUP_LAYER_START;
        private int _topLayerIndex = TOP_LAYER_START;
        
        // 记录UI的层级
        private readonly Dictionary<Type, int> _uiLayers = new Dictionary<Type, int>();
        
        /// <summary>
        /// 为UI分配层级
        /// </summary>
        /// <param name="uiType">UI类型</param>
        /// <param name="layerType">层级类型</param>
        /// <returns>分配的sortingOrder</returns>
        public int AllocateLayer(Type uiType, UIType layerType)
        {
            int layer;
            
            switch (layerType)
            {
                case UIType.Main:
                    layer = _mainLayerIndex++;
                    if (_mainLayerIndex > MAIN_LAYER_END)
                    {
                        FrameworkLogger.Warn($"[UILayer] Main层级已满，重置为 {MAIN_LAYER_START}");
                        _mainLayerIndex = MAIN_LAYER_START;
                        layer = _mainLayerIndex++;
                    }
                    break;
                    
                case UIType.Popup:
                    layer = _popupLayerIndex++;
                    if (_popupLayerIndex > POPUP_LAYER_END)
                    {
                        FrameworkLogger.Warn($"[UILayer] Popup层级已满，重置为 {POPUP_LAYER_START}");
                        _popupLayerIndex = POPUP_LAYER_START;
                        layer = _popupLayerIndex++;
                    }
                    break;
                    
                case UIType.Top:
                    layer = _topLayerIndex++;
                    if (_topLayerIndex > TOP_LAYER_END)
                    {
                        FrameworkLogger.Warn($"[UILayer] Top层级已满，重置为 {TOP_LAYER_START}");
                        _topLayerIndex = TOP_LAYER_START;
                        layer = _topLayerIndex++;
                    }
                    break;
                    
                case UIType.Effect:
                default:
                    // Effect使用Popup层
                    layer = _popupLayerIndex++;
                    break;
            }
            
            _uiLayers[uiType] = layer;
            
            FrameworkLogger.Info($"[UILayer] 分配层级: {uiType.Name} -> {layer} ({layerType})");
            
            return layer;
        }
        
        /// <summary>
        /// 获取UI的层级
        /// </summary>
        public int GetLayer(Type uiType)
        {
            return _uiLayers.TryGetValue(uiType, out var layer) ? layer : 0;
        }
        
        /// <summary>
        /// 释放UI的层级
        /// </summary>
        public void ReleaseLayer(Type uiType)
        {
            if (_uiLayers.Remove(uiType))
            {
                FrameworkLogger.Info($"[UILayer] 释放层级: {uiType.Name}");
            }
        }
        
        /// <summary>
        /// 清空所有层级
        /// </summary>
        public void Clear()
        {
            _uiLayers.Clear();
            _mainLayerIndex = MAIN_LAYER_START;
            _popupLayerIndex = POPUP_LAYER_START;
            _topLayerIndex = TOP_LAYER_START;
            
            FrameworkLogger.Info("[UILayer] 清空所有层级");
        }
    }
}
