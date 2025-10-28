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
        
        // 智能缓存统计数据
        private readonly Dictionary<Type, int> _showCounts = new Dictionary<Type, int>();              // 显示次数
        private readonly Dictionary<Type, DateTime> _lastShowTime = new Dictionary<Type, DateTime>();  // 最后显示时间
        
        // 最大缓存数量（智能缓存上限）
        private const int MAX_CACHE_SIZE = 10;
        
        /// <summary>
        /// 获取UI实例
        /// </summary>
        public IBaseUI GetInstance(Type uiType)
        {
            if (_instances.TryGetValue(uiType, out var instance))
            {
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
            
            // 初始化统计数据
            if (!_showCounts.ContainsKey(uiType))
            {
                _showCounts[uiType] = 0;
            }
            
            FrameworkLogger.Info($"[UIInstance] 添加实例: {uiType.Name}, 总数: {_instances.Count}");
        }
        
        /// <summary>
        /// 移除UI实例
        /// </summary>
        public void RemoveInstance(Type uiType)
        {
            if (_instances.Remove(uiType))
            {
                // 清除统计数据
                _showCounts.Remove(uiType);
                _lastShowTime.Remove(uiType);
                
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
            _showCounts.Clear();
            _lastShowTime.Clear();
            FrameworkLogger.Info("[UIInstance] 清空所有实例缓存");
        }
        
        /// <summary>
        /// 记录UI显示（增加显示次数，更新最后显示时间）
        /// </summary>
        public void RecordShow(Type uiType)
        {
            // 增加显示次数
            if (_showCounts.ContainsKey(uiType))
            {
                _showCounts[uiType]++;
            }
            else
            {
                _showCounts[uiType] = 1;
            }
            
            // 更新最后显示时间
            _lastShowTime[uiType] = DateTime.Now;
            
            var count = _showCounts[uiType];
            FrameworkLogger.Info($"[UIInstance] 记录显示: {uiType.Name}, 次数: {count}");
        }
        
        /// <summary>
        /// 获取UI显示次数
        /// </summary>
        public int GetShowCount(Type uiType)
        {
            return _showCounts.TryGetValue(uiType, out var count) ? count : 0;
        }
        
        /// <summary>
        /// 获取UI最后显示时间
        /// </summary>
        public DateTime GetLastShowTime(Type uiType)
        {
            return _lastShowTime.TryGetValue(uiType, out var time) ? time : DateTime.MinValue;
        }
        
        /// <summary>
        /// 检查并执行智能淘汰（基于显示次数和时间）
        /// 注意：上限只针对智能缓存的UI，永久缓存不占用配额
        /// </summary>
        public void CheckSmartEviction(Type justShownUIType)
        {
            // 筛选智能缓存的UI（只统计和淘汰 SmartCache 策略的UI）
            var smartCacheUIs = new List<(Type uiType, IBaseUI ui, int showCount, DateTime lastShowTime)>();
            
            foreach (var kvp in _instances)
            {
                var uiType = kvp.Key;
                var ui = kvp.Value;
                
                // 获取该UI的配置
                var config = UIProjectConfigManager.GetUIInstanceConfig(uiType);
                
                // 只统计智能缓存策略的UI
                if (config?.CacheStrategy == UICacheStrategy.SmartCache)
                {
                    var showCount = GetShowCount(uiType);
                    var lastShowTime = GetLastShowTime(uiType);
                    smartCacheUIs.Add((uiType, ui, showCount, lastShowTime));
                }
            }
            
            // 检查智能缓存的UI数量是否超过上限
            if (smartCacheUIs.Count <= MAX_CACHE_SIZE)
            {
                return;
            }
            
            FrameworkLogger.Info($"[UIInstance] 智能缓存UI数量({smartCacheUIs.Count})超过上限({MAX_CACHE_SIZE})，开始淘汰");
            
            if (smartCacheUIs.Count == 0)
            {
                return;
            }
            
            // 按淘汰优先级排序
            // 1. 显示次数少的优先淘汰
            // 2. 次数相同时，最久未显示的优先淘汰
            var sorted = smartCacheUIs
                .OrderBy(x => x.showCount)           // 按显示次数升序
                .ThenBy(x => x.lastShowTime)         // 按最后显示时间升序
                .ToList();
            
            // 淘汰直到智能缓存UI满足上限
            var evictCount = 0;
            while (smartCacheUIs.Count - evictCount > MAX_CACHE_SIZE && sorted.Count > 0)
            {
                var toEvict = sorted[0];
                sorted.RemoveAt(0);
                
                // 销毁UI（异步操作，异常会通过 async void 抛出）
                HandleDestroyAsync(toEvict.ui, toEvict.uiType);
                _instances.Remove(toEvict.uiType);
                _showCounts.Remove(toEvict.uiType);
                _lastShowTime.Remove(toEvict.uiType);
                
                evictCount++;
                FrameworkLogger.Info($"[UIInstance] 智能淘汰: {toEvict.uiType.Name} (显示{toEvict.showCount}次, 最后显示: {toEvict.lastShowTime:yyyy-MM-dd HH:mm:ss})");
            }
            
            FrameworkLogger.Info($"[UIInstance] 智能淘汰完成: 淘汰{evictCount}个, 智能缓存剩余{smartCacheUIs.Count - evictCount}个, 总缓存{_instances.Count}个");
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
        
        /// <summary>
        /// 处理UI销毁异步操作，确保异常能被抛出
        /// </summary>
        private async void HandleDestroyAsync(IBaseUI ui, System.Type uiType)
        {
            await ui.DoDestroy(); // 不捕获异常，让它作为未处理异常抛出
        }
    }
}
