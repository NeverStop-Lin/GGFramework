using System;
using System.Collections.Generic;
using Framework.Scripts;

namespace Framework.Core
{
    /// <summary>
    /// UI状态管理器
    /// 负责追踪UI的运行时状态
    /// </summary>
    public class UIStateManager
    {
        // 记录每个UI的当前状态
        private readonly Dictionary<Type, UIRuntimeState> _states = new Dictionary<Type, UIRuntimeState>();
        
        /// <summary>
        /// 设置UI状态
        /// </summary>
        public void SetState(Type uiType, UIRuntimeState state)
        {
            var oldState = GetState(uiType);
            _states[uiType] = state;
            
            FrameworkLogger.Info($"[UIState] {uiType.Name}: {oldState} -> {state}");
            
            // TODO: 触发状态变化事件
            // OnStateChanged?.Invoke(uiType, oldState, state);
        }
        
        /// <summary>
        /// 获取UI状态
        /// </summary>
        public UIRuntimeState GetState(Type uiType)
        {
            return _states.TryGetValue(uiType, out var state) ? state : UIRuntimeState.None;
        }
        
        /// <summary>
        /// 检查UI是否正在显示
        /// </summary>
        public bool IsShowing(Type uiType)
        {
            return GetState(uiType) == UIRuntimeState.Showing;
        }
        
        /// <summary>
        /// 检查UI是否已创建
        /// </summary>
        public bool IsCreated(Type uiType)
        {
            var state = GetState(uiType);
            return state != UIRuntimeState.None && state != UIRuntimeState.Destroyed;
        }
        
        /// <summary>
        /// 移除UI状态
        /// </summary>
        public void RemoveState(Type uiType)
        {
            if (_states.Remove(uiType))
            {
                FrameworkLogger.Info($"[UIState] 移除状态: {uiType.Name}");
            }
        }
        
        /// <summary>
        /// 获取所有正在显示的UI
        /// </summary>
        public List<Type> GetShowingUIs()
        {
            var showing = new List<Type>();
            foreach (var kvp in _states)
            {
                if (kvp.Value == UIRuntimeState.Showing)
                {
                    showing.Add(kvp.Key);
                }
            }
            return showing;
        }
        
        /// <summary>
        /// 清空所有状态
        /// </summary>
        public void Clear()
        {
            _states.Clear();
            FrameworkLogger.Info("[UIState] 清空所有状态");
        }
        
        /// <summary>
        /// 保存UI状态到持久化存储
        /// </summary>
        public void SaveState(Type uiType)
        {
            var state = GetState(uiType);
            var key = $"ui_state_{uiType.Name}";
            
            try
            {
                GridFramework.Storage.Save(key, state.ToString());
                
                FrameworkLogger.Info($"[UIState] 保存状态: {uiType.Name} -> {state}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UIState] 保存状态失败: {uiType.Name}, {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从持久化存储加载UI状态
        /// </summary>
        public UIRuntimeState LoadState(Type uiType)
        {
            var key = $"ui_state_{uiType.Name}";
            
            try
            {
                var data = GridFramework.Storage.Load<string>(key);
                if (!string.IsNullOrEmpty(data) && Enum.TryParse<UIRuntimeState>(data, out UIRuntimeState state))
                {
                    FrameworkLogger.Info($"[UIState] 加载状态: {uiType.Name} -> {state}");
                    return state;
                }
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UIState] 加载状态失败: {uiType.Name}, {ex.Message}");
            }
            
            return UIRuntimeState.None;
        }
    }
}
