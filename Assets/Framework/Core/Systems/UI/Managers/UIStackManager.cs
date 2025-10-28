using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// UI栈管理器
    /// 负责管理UI的栈式显示/隐藏，支持返回功能
    /// </summary>
    public class UIStackManager
    {
        // UI栈（后进先出）
        private readonly Stack<IBaseUI> _uiStack = new Stack<IBaseUI>();
        
        // 记录UI类型到实例的映射
        private readonly Dictionary<Type, IBaseUI> _uiTypeToInstance = new Dictionary<Type, IBaseUI>();
        
        /// <summary>
        /// 推入UI到栈中
        /// </summary>
        /// <param name="ui">UI实例</param>
        /// <param name="hidePrevious">是否隐藏前一个UI（默认为false，保持显示）</param>
        public void Push(IBaseUI ui, bool hidePrevious = false)
        {
            var uiType = ui.GetType();
            
            // 如果栈中已有UI，根据配置决定是否隐藏
            if (_uiStack.Count > 0 && hidePrevious)
            {
                var previous = _uiStack.Peek();
                // 隐藏但不销毁（异步操作，异常会通过 async void 抛出）
                HandleHideAsync(previous);
                FrameworkLogger.Info($"[UIStack] 隐藏前一个UI: {previous.GetType().Name}");
            }
            
            // 如果栈中已有UI且不隐藏，禁用其Raycast（避免点击穿透）
            if (_uiStack.Count > 0 && !hidePrevious)
            {
                DisableRaycast(_uiStack.Peek());
            }
            
            // 推入栈
            _uiStack.Push(ui);
            _uiTypeToInstance[uiType] = ui;
            
            FrameworkLogger.Info($"[UIStack] 推入UI: {uiType.Name}, 栈深度: {_uiStack.Count}");
        }
        
        /// <summary>
        /// 从栈中弹出UI
        /// </summary>
        /// <returns>弹出的UI实例，如果栈为空返回null</returns>
        public IBaseUI Pop()
        {
            if (_uiStack.Count == 0)
            {
                FrameworkLogger.Warn("[UIStack] 栈为空，无法弹出");
                return null;
            }
            
            var ui = _uiStack.Pop();
            var uiType = ui.GetType();
            _uiTypeToInstance.Remove(uiType);
            
            FrameworkLogger.Info($"[UIStack] 弹出UI: {uiType.Name}, 剩余栈深度: {_uiStack.Count}");
            
            // 如果栈中还有UI，恢复其Raycast
            if (_uiStack.Count > 0)
            {
                var previous = _uiStack.Peek();
                EnableRaycast(previous);
            }
            
            return ui;
        }
        
        /// <summary>
        /// 获取栈顶UI
        /// </summary>
        public IBaseUI Peek()
        {
            return _uiStack.Count > 0 ? _uiStack.Peek() : null;
        }
        
        /// <summary>
        /// 获取UI栈的副本
        /// </summary>
        public List<IBaseUI> GetStack()
        {
            return new List<IBaseUI>(_uiStack);
        }
        
        /// <summary>
        /// 检查UI是否在栈中
        /// </summary>
        public bool Contains(Type uiType)
        {
            return _uiTypeToInstance.ContainsKey(uiType);
        }
        
        /// <summary>
        /// 清空UI栈
        /// </summary>
        public void Clear()
        {
            _uiStack.Clear();
            _uiTypeToInstance.Clear();
            FrameworkLogger.Info("[UIStack] 清空UI栈");
        }
        
        /// <summary>
        /// 获取栈深度
        /// </summary>
        public int Count => _uiStack.Count;
        
        /// <summary>
        /// 禁用UI的Raycast（防止点击穿透）
        /// </summary>
        private void DisableRaycast(IBaseUI ui)
        {
            if (ui is UIBehaviour ugui && ugui != null)
            {
                var raycaster = ugui.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = false;
                    FrameworkLogger.Info($"[UIStack] 禁用Raycast: {ui.GetType().Name}");
                }
            }
        }
        
        /// <summary>
        /// 启用UI的Raycast
        /// </summary>
        private void EnableRaycast(IBaseUI ui)
        {
            if (ui is UIBehaviour ugui && ugui != null)
            {
                var raycaster = ugui.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = true;
                    FrameworkLogger.Info($"[UIStack] 启用Raycast: {ui.GetType().Name}");
                }
            }
        }
        
        /// <summary>
        /// 处理UI隐藏异步操作，确保异常能被抛出
        /// </summary>
        private async void HandleHideAsync(IBaseUI ui)
        {
            await ui.DoHide(); // 不捕获异常，让它作为未处理异常抛出
        }
    }
}
