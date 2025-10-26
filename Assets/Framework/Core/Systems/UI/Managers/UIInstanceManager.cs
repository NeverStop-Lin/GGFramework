using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Core
{
    /// <summary>
    /// UI实例管理器
    /// 负责UI实例的创建、缓存和销毁
    /// </summary>
    public class UIInstanceManager
    {
        // UI实例缓存
        private readonly Dictionary<Type, IBaseUI> _instances = new Dictionary<Type, IBaseUI>();
        
        // LRU缓存队列（用于限制缓存数量）
        private readonly LinkedList<Type> _lruList = new LinkedList<Type>();
        
        // 最大缓存数量（LRU策略时使用）
        private const int MAX_CACHE_SIZE = 20;
        
        /// <summary>
        /// 获取UI实例
        /// </summary>
        public IBaseUI GetInstance(Type uiType)
        {
            if (_instances.TryGetValue(uiType, out var instance))
            {
                // 更新LRU
                UpdateLRU(uiType);
                return instance;
            }
            return null;
        }
        
        /// <summary>
        /// 添加UI实例
        /// </summary>
        public void AddInstance(Type uiType, IBaseUI instance)
        {
            _instances[uiType] = instance;
            UpdateLRU(uiType);
            
            FrameworkLogger.Info($"[UIInstance] 添加实例: {uiType.Name}, 总数: {_instances.Count}");
        }
        
        /// <summary>
        /// 移除UI实例
        /// </summary>
        public void RemoveInstance(Type uiType)
        {
            if (_instances.Remove(uiType))
            {
                RemoveFromLRU(uiType);
                FrameworkLogger.Info($"[UIInstance] 移除实例: {uiType.Name}, 剩余: {_instances.Count}");
            }
        }
        
        /// <summary>
        /// 检查是否有缓存实例
        /// </summary>
        public bool HasInstance(Type uiType)
        {
            return _instances.ContainsKey(uiType);
        }
        
        /// <summary>
        /// 获取所有UI实例
        /// </summary>
        public List<IBaseUI> GetAllInstances()
        {
            return _instances.Values.ToList();
        }
        
        /// <summary>
        /// 获取实例数量
        /// </summary>
        public int GetCount()
        {
            return _instances.Count;
        }
        
        /// <summary>
        /// 清空所有实例（不销毁，只清除缓存）
        /// </summary>
        public void Clear()
        {
            _instances.Clear();
            _lruList.Clear();
            FrameworkLogger.Info("[UIInstance] 清空所有实例缓存");
        }
        
        /// <summary>
        /// 检查并执行LRU淘汰
        /// </summary>
        public void CheckLRU()
        {
            if (_instances.Count <= MAX_CACHE_SIZE)
            {
                return;
            }
            
            // 淘汰最少使用的UI
            while (_instances.Count > MAX_CACHE_SIZE && _lruList.Count > 0)
            {
                var lruType = _lruList.Last.Value;
                _lruList.RemoveLast();
                
                if (_instances.TryGetValue(lruType, out var ui))
                {
                    // 销毁UI
                    _ = ui.DoDestroy();
                    _instances.Remove(lruType);
                    
                    FrameworkLogger.Info($"[UIInstance] LRU淘汰: {lruType.Name}");
                }
            }
        }
        
        /// <summary>
        /// 更新LRU队列
        /// </summary>
        private void UpdateLRU(Type uiType)
        {
            // 移除旧位置
            var node = _lruList.Find(uiType);
            if (node != null)
            {
                _lruList.Remove(node);
            }
            
            // 添加到最前面（表示最近使用）
            _lruList.AddFirst(uiType);
        }
        
        /// <summary>
        /// 从LRU队列中移除
        /// </summary>
        private void RemoveFromLRU(Type uiType)
        {
            var node = _lruList.Find(uiType);
            if (node != null)
            {
                _lruList.Remove(node);
            }
        }
        
        /// <summary>
        /// 获取内存占用估算（MB）
        /// </summary>
        public float GetMemoryUsage()
        {
            // 简单估算：每个UI实例约1MB
            // 实际应该通过Profiler API获取
            return _instances.Count * 1.0f;
        }
    }
}
