using System;
using System.Collections.Generic;

namespace Framework.Core
{
    /// <summary>
    /// UI层级管理器
    /// 负责管理UI的sortingOrder分配
    /// 基于UIProjectConfig的层级定义
    /// </summary>
    public class UILayerManager
    {
        // 记录每个层级的当前索引
        private readonly Dictionary<string, int> _layerCounters = new Dictionary<string, int>();
        
        // 记录UI的层级
        private readonly Dictionary<Type, int> _uiLayers = new Dictionary<Type, int>();
        
        /// <summary>
        /// 为UI分配层级
        /// </summary>
        /// <param name="uiType">UI类型</param>
        /// <param name="layerName">层级名称</param>
        /// <returns>分配的sortingOrder</returns>
        public int AllocateLayer(Type uiType, string layerName)
        {
            // 获取层级基础sortingOrder
            var baseSortingOrder = UIProjectConfigManager.GetBaseSortingOrder(layerName);
            
            // 获取该层级的计数器
            if (!_layerCounters.TryGetValue(layerName, out var counter))
            {
                counter = 0;
            }
            
            // 计算最终的sortingOrder：基础值 + 计数器
            var finalSortingOrder = baseSortingOrder + counter;
            
            // 递增计数器
            _layerCounters[layerName] = counter + 1;
            
            // 记录UI的层级
            _uiLayers[uiType] = finalSortingOrder;
            
            FrameworkLogger.Info($"[UILayerManager] 分配层级: {uiType.Name} -> {finalSortingOrder} (Layer={layerName}, Base={baseSortingOrder}, Offset={counter})");
            
            return finalSortingOrder;
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
                FrameworkLogger.Info($"[UILayerManager] 释放层级: {uiType.Name}");
            }
        }
        
        /// <summary>
        /// 清空所有层级
        /// </summary>
        public void Clear()
        {
            _uiLayers.Clear();
            _layerCounters.Clear();
            
            FrameworkLogger.Info("[UILayerManager] 清空所有层级");
        }
        
        /// <summary>
        /// 重置层级计数器
        /// </summary>
        public void ResetCounters()
        {
            _layerCounters.Clear();
            FrameworkLogger.Info("[UILayerManager] 重置层级计数器");
        }
    }
}
