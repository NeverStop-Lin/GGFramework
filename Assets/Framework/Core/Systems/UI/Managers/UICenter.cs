using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zenject;

namespace Framework.Core
{
    /// <summary>
    /// UI管理中心
    /// 负责UI的创建、显示、隐藏、销毁等核心功能
    /// </summary>
    public class UICenter : IUI
    {
        #region 依赖注入
        
        [Inject]
        private DiContainer _container;
        
        #endregion
        
        #region 字段
        
        // UI状态字典
        private readonly ConcurrentDictionary<Type, UiState> _uiStates = new ConcurrentDictionary<Type, UiState>();
        
        // 四个核心管理器
        private readonly UIInstanceManager _instanceManager = new UIInstanceManager();
        private readonly UIStackManager _stackManager = new UIStackManager();
        private readonly UILayerManager _layerManager = new UILayerManager();
        private readonly UIStateManager _stateManager = new UIStateManager();
        
        #endregion
        
        #region 基础API
        
        /// <summary>
        /// 显示UI
        /// </summary>
        public UiLifeCycle<T> Show<T>(params object[] args) where T : IBaseUI
        {
            var uiKey = typeof(T);
            FrameworkLogger.Info($"[UICenter] 请求显示UI: {uiKey.Name}");

            var uiState = _uiStates.GetOrAdd(uiKey, _ => new UiState());

            // 只在需要时创建新的TCS（修复Bug #1）
            if (uiState.CreateTcs == null || uiState.CreateTcs.Task.IsCompleted)
            {
                uiState.CreateTcs = new TaskCompletionSource<object>();
            }
            if (uiState.ShowTcs == null || uiState.ShowTcs.Task.IsCompleted)
            {
                uiState.ShowTcs = new TaskCompletionSource<object>();
            }
            if (uiState.ReadyTcs == null || uiState.ReadyTcs.Task.IsCompleted)
            {
                uiState.ReadyTcs = new TaskCompletionSource<object>();
            }
            if (uiState.HideTcs == null || uiState.HideTcs.Task.IsCompleted)
            {
                uiState.HideTcs = new TaskCompletionSource<object>();
            }

            if (uiState.Ui == null)
            {
                FrameworkLogger.Info($"[UICenter] 创建新UI实例: {uiKey.Name}");
                var ui = _container.Resolve<PlaceholderFactory<Type, IBaseUI>>().Create(typeof(T));
                ui.Initialize();
                uiState.Ui = ui;
                
                // 添加到实例管理器
                _instanceManager.AddInstance(uiKey, ui);
                
                // 正确等待异步方法（修复Bug #2）
                _ = CreateAndShowAsync(ui, args, uiState, uiKey);
            }
            else
            {
                FrameworkLogger.Info($"[UICenter] 复用已有UI实例: {uiKey.Name}");
                _ = ShowOnlyAsync(uiState.Ui, args, uiState, uiKey);
            }

            return new UiLifeCycle<T>
            {
                ShowTask = uiState.ShowTcs.Task,
                HideTask = uiState.HideTcs.Task,
                Target = uiState.Ui is T ? (T)uiState.Ui : default
            };
        }

        /// <summary>
        /// 创建并显示UI（完整流程）
        /// </summary>
        private async Task CreateAndShowAsync(IBaseUI ui, object[] args, UiState uiState, Type uiType)
        {
            try
            {
                _stateManager.SetState(uiType, UIRuntimeState.Creating);
                
                await CreateAsync(ui, args, uiState, uiType);
                await ShowAsync(ui, args, uiState, uiType);
                await ReadyAsync(ui, args, uiState);
                
                _stateManager.SetState(uiType, UIRuntimeState.Showing);
                
                FrameworkLogger.Info($"[UICenter] UI显示完成: {uiType.Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI显示异常: {ui.GetType().Name}, {ex.Message}\n{ex.StackTrace}");
                RemoveUi(ui.GetType(), ex);
            }
        }
        
        /// <summary>
        /// 只显示UI（复用已有实例）
        /// </summary>
        private async Task ShowOnlyAsync(IBaseUI ui, object[] args, UiState uiState, Type uiType)
        {
            try
            {
                await ShowAsync(ui, args, uiState, uiType);
                await ReadyAsync(ui, args, uiState);
                
                _stateManager.SetState(uiType, UIRuntimeState.Showing);
                
                FrameworkLogger.Info($"[UICenter] UI显示完成: {uiType.Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI显示异常: {ui.GetType().Name}, {ex.Message}");
                uiState.ShowTcs?.TrySetException(ex);
                uiState.ReadyTcs?.TrySetException(ex);
            }
        }

        private async Task CreateAsync(IBaseUI ui, object[] args, UiState uiState, Type uiType)
        {
            try
            {
                var result = await ui.DoCreate(args);
                uiState.CreateTcs?.TrySetResult(result);
                
                FrameworkLogger.Info($"[UICenter] UI创建成功: {uiType.Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI创建失败: {uiType.Name}, {ex.Message}");
                uiState.CreateTcs?.TrySetException(ex);
                throw;
            }
        }

        private async Task ShowAsync(IBaseUI ui, object[] args, UiState uiState, Type uiType)
        {
            try
            {
                var result = await ui.DoShow(args);
                uiState.ShowTcs?.TrySetResult(result);
                
                // 分配层级
                if (ui is UGUIBaseUIBehaviour ugui)
                {
                    var config = UIManifestManager.GetConfig(uiType);
                    var layerType = config?.UIType ?? UIType.Main;
                    var layer = _layerManager.AllocateLayer(uiType, layerType);
                    ugui.SetIndex(layer);
                }
                
                FrameworkLogger.Info($"[UICenter] UI显示成功: {uiType.Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI显示失败: {uiType.Name}, {ex.Message}");
                uiState.ShowTcs?.TrySetException(ex);
                throw;
            }
        }

        private async Task ReadyAsync(IBaseUI ui, object[] args, UiState uiState)
        {
            try
            {
                var result = await ui.DoReady(args);
                uiState.ReadyTcs?.TrySetResult(result);
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI Ready失败: {ui.GetType().Name}, {ex.Message}");
                uiState.ReadyTcs?.TrySetException(ex);
                throw;
            }
        }

        /// <summary>
        /// 隐藏UI
        /// </summary>
        public Task<object> Hide<T>(params object[] args)
        {
            return Hide(typeof(T), args);
        }

        /// <summary>
        /// 隐藏UI（通过Type）（修复Bug #4）
        /// </summary>
        public async Task<object> Hide(Type uiType, params object[] args)
        {
            FrameworkLogger.Info($"[UICenter] 请求隐藏UI: {uiType.Name}");

            if (!_uiStates.TryGetValue(uiType, out var uiState))
            {
                FrameworkLogger.Warn($"[UICenter] UI不存在: {uiType.Name}");
                return null;
            }

            if (uiState.Ui != null)
            {
                return await HideAsync(uiState.Ui, args, uiState, uiType);
            }

            FrameworkLogger.Warn($"[UICenter] UI实例为空: {uiType.Name}");
            return null;
        }

        /// <summary>
        /// 执行隐藏操作（修复Bug #3）
        /// </summary>
        private async Task<object> HideAsync(IBaseUI ui, object[] args, UiState uiState, Type uiType)
        {
            try
            {
                _stateManager.SetState(uiType, UIRuntimeState.Hidden);
                
                var result = await ui.DoHide(args);
                uiState.HideTcs?.TrySetResult(result);
                
                FrameworkLogger.Info($"[UICenter] UI隐藏成功: {uiType.Name}");
                
                // 根据缓存策略决定是否销毁
                var config = UIManifestManager.GetConfig(uiType);
                if (config?.CacheStrategy == UICacheStrategy.NeverCache)
                {
                    await DestroyUI(uiType);
                }
                
                return result; // 修复Bug #3：正确返回
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI隐藏失败: {uiType.Name}, {ex.Message}");
                uiState.HideTcs?.TrySetException(ex);
                throw;
            }
        }
        
        #endregion
        
        #region 实例管理
        
        /// <summary>
        /// 销毁指定UI
        /// </summary>
        public async Task DestroyUI<T>() where T : IBaseUI
        {
            await DestroyUI(typeof(T));
        }
        
        /// <summary>
        /// 销毁指定UI（内部方法）
        /// </summary>
        private async Task DestroyUI(Type uiType)
        {
            FrameworkLogger.Info($"[UICenter] 请求销毁UI: {uiType.Name}");
            
            if (!_uiStates.TryRemove(uiType, out var uiState))
            {
                FrameworkLogger.Warn($"[UICenter] UI不存在: {uiType.Name}");
                return;
            }
            
            if (uiState.Ui != null)
            {
                try
                {
                    _stateManager.SetState(uiType, UIRuntimeState.Destroying);
                    
                    await uiState.Ui.DoDestroy();
                    
                    // 从管理器中移除
                    _instanceManager.RemoveInstance(uiType);
                    _layerManager.ReleaseLayer(uiType);
                    _stateManager.SetState(uiType, UIRuntimeState.Destroyed);
                    
                    FrameworkLogger.Info($"[UICenter] UI销毁成功: {uiType.Name}");
                }
                catch (Exception ex)
                {
                    FrameworkLogger.Error($"[UICenter] UI销毁失败: {uiType.Name}, {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 销毁所有UI
        /// </summary>
        public async Task DestroyAllUI()
        {
            FrameworkLogger.Info($"[UICenter] 销毁所有UI，总数: {_uiStates.Count}");
            
            var uiTypes = _uiStates.Keys.ToList();
            foreach (var uiType in uiTypes)
            {
                await DestroyUI(uiType);
            }
            
            // 清空管理器
            _instanceManager.Clear();
            _layerManager.Clear();
            _stateManager.Clear();
            _stackManager.Clear();
        }
        
        /// <summary>
        /// 获取UI实例
        /// </summary>
        public T GetUI<T>() where T : IBaseUI
        {
            var uiType = typeof(T);
            var instance = _instanceManager.GetInstance(uiType);
            return instance is T ? (T)instance : default;
        }
        
        /// <summary>
        /// 检查UI是否正在显示
        /// </summary>
        public bool IsShowing<T>() where T : IBaseUI
        {
            return _stateManager.IsShowing(typeof(T));
        }
        
        /// <summary>
        /// 获取所有显示中的UI
        /// </summary>
        public List<IBaseUI> GetAllShowingUIs()
        {
            var showingTypes = _stateManager.GetShowingUIs();
            return showingTypes.Select(t => _instanceManager.GetInstance(t)).Where(ui => ui != null).ToList();
        }
        
        #endregion
        
        #region 栈管理
        
        /// <summary>
        /// 推入UI栈（显示UI）
        /// </summary>
        public UiLifeCycle<T> PushUI<T>(params object[] args) where T : IBaseUI
        {
            var lifecycle = Show<T>(args);
            
            // 添加到栈中（保持显示，只禁用Raycast）
            if (lifecycle.Target != null)
            {
                _stackManager.Push(lifecycle.Target, hidePrevious: false);
            }
            
            return lifecycle;
        }
        
        /// <summary>
        /// 弹出栈顶UI
        /// </summary>
        public async Task<IBaseUI> PopUI()
        {
            var ui = _stackManager.Pop();
            if (ui != null)
            {
                await Hide(ui.GetType());
            }
            return ui;
        }
        
        /// <summary>
        /// 获取UI栈
        /// </summary>
        public List<IBaseUI> GetUIStack()
        {
            return _stackManager.GetStack();
        }
        
        /// <summary>
        /// 清空UI栈
        /// </summary>
        public async Task ClearUIStack()
        {
            var stack = _stackManager.GetStack();
            foreach (var ui in stack)
            {
                await Hide(ui.GetType());
            }
            _stackManager.Clear();
        }
        
        #endregion
        
        #region 批量操作
        
        /// <summary>
        /// 隐藏所有UI（可排除）
        /// </summary>
        public async Task HideAll(params Type[] except)
        {
            var exceptSet = new HashSet<Type>(except ?? Array.Empty<Type>());
            var showingUIs = _stateManager.GetShowingUIs();
            
            foreach (var uiType in showingUIs)
            {
                if (!exceptSet.Contains(uiType))
                {
                    await Hide(uiType);
                }
            }
        }
        
        #endregion
        
        #region 预加载
        
        /// <summary>
        /// 预加载UI
        /// </summary>
        public async Task PreloadUI<T>() where T : IBaseUI
        {
            var uiType = typeof(T);
            FrameworkLogger.Info($"[UICenter] 预加载UI: {uiType.Name}");
            
            // 如果已经有实例，跳过
            if (_instanceManager.HasInstance(uiType))
            {
                FrameworkLogger.Info($"[UICenter] UI已存在，跳过预加载: {uiType.Name}");
                return;
            }
            
            // 创建UI实例但不显示
            var uiState = _uiStates.GetOrAdd(uiType, _ => new UiState());
            uiState.CreateTcs = new TaskCompletionSource<object>();
            
            var ui = _container.Resolve<PlaceholderFactory<Type, IBaseUI>>().Create(typeof(T));
            ui.Initialize();
            uiState.Ui = ui;
            
            _instanceManager.AddInstance(uiType, ui);
            
            try
            {
                _stateManager.SetState(uiType, UIRuntimeState.Creating);
                await ui.DoCreate();
                _stateManager.SetState(uiType, UIRuntimeState.Hidden);
                uiState.CreateTcs.TrySetResult(null);
                
                FrameworkLogger.Info($"[UICenter] UI预加载完成: {uiType.Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI预加载失败: {uiType.Name}, {ex.Message}");
                uiState.CreateTcs.TrySetException(ex);
                throw;
            }
        }
        
        #endregion
        
        #region 层级管理
        
        /// <summary>
        /// 设置UI层级
        /// </summary>
        public void SetUILayer<T>(int layer) where T : IBaseUI
        {
            var ui = GetUI<T>();
            if (ui is UGUIBaseUIBehaviour ugui)
            {
                ugui.SetIndex(layer);
                FrameworkLogger.Info($"[UICenter] 设置UI层级: {typeof(T).Name} -> {layer}");
            }
        }
        
        /// <summary>
        /// 置顶UI
        /// </summary>
        public void BringToFront<T>() where T : IBaseUI
        {
            var ui = GetUI<T>();
            if (ui is UGUIBaseUIBehaviour ugui)
            {
                var maxLayer = 999;
                ugui.SetIndex(maxLayer);
                FrameworkLogger.Info($"[UICenter] 置顶UI: {typeof(T).Name}");
            }
        }
        
        #endregion
        
        #region 状态查询
        
        /// <summary>
        /// 获取UI状态
        /// </summary>
        public UIRuntimeState GetUIState<T>() where T : IBaseUI
        {
            return _stateManager.GetState(typeof(T));
        }
        
        /// <summary>
        /// 获取UI数量
        /// </summary>
        public int GetUICount()
        {
            return _instanceManager.GetCount();
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 移除UI
        /// </summary>
        private void RemoveUi(Type uiKey, Exception exception = null)
        {
            if (!_uiStates.TryRemove(uiKey, out var uiState)) return;

            if (uiState.Ui is IDisposable disposable)
            {
                FrameworkLogger.Info($"[UICenter] 释放UI资源: {uiKey.Name}");
                disposable.Dispose();
            }

            // 统一处理所有状态
            var tcsList = new[]
            {
                uiState.CreateTcs, uiState.ShowTcs, uiState.ReadyTcs, uiState.HideTcs
            };
            foreach (var tcs in tcsList)
            {
                if (tcs == null) continue;

                if (exception != null)
                {
                    FrameworkLogger.Error($"[UICenter] UI操作异常: {uiKey.Name}, {exception.Message}");
                    tcs.TrySetException(exception);
                }
                else
                {
                    tcs.TrySetCanceled();
                }
            }
            
            // 从管理器中移除
            _instanceManager.RemoveInstance(uiKey);
            _layerManager.ReleaseLayer(uiKey);
            _stateManager.RemoveState(uiKey);
        }
        
        #endregion
        
        #region 内部类
        
        /// <summary>
        /// UI状态类
        /// </summary>
        private class UiState
        {
            public IBaseUI Ui { get; set; }
            public TaskCompletionSource<object> CreateTcs { get; set; }
            public TaskCompletionSource<object> ShowTcs { get; set; }
            public TaskCompletionSource<object> ReadyTcs { get; set; }
            public TaskCompletionSource<object> HideTcs { get; set; }
        }
        
        #endregion
    }
}