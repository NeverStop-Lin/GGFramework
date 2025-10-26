using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
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
        
        [Inject]
        private IResource _resource;
        
        #endregion
        
        #region 字段
        
        // UI状态字典（使用UIInstanceKey作为键，支持多实例）
        private readonly ConcurrentDictionary<UIInstanceKey, UiState> _uiStates = new ConcurrentDictionary<UIInstanceKey, UiState>();
        
        // 四个核心管理器
        private readonly UIInstanceManager _instanceManager = new UIInstanceManager();
        private readonly UIStackManager _stackManager = new UIStackManager();
        private readonly UILayerManager _layerManager = new UILayerManager();
        private readonly UIStateManager _stateManager = new UIStateManager();
        
        // 层级计数器（用于同层级UI的sortingOrder递增）
        private readonly Dictionary<string, int> _layerCounters = new Dictionary<string, int>();
        
        // 自动实例ID计数器（每个UI类型独立计数）
        private readonly Dictionary<Type, int> _autoInstanceCounters = new Dictionary<Type, int>();
        
        // 自动实例ID前缀（避免与手动ID冲突）
        private const string AUTO_INSTANCE_PREFIX = "__auto__";
        
        #endregion
        
        #region 基础API
        
        /// <summary>
        /// 显示UI
        /// - 单例模式：复用同一实例，刷新层级
        /// - 多实例模式：自动生成实例ID，创建新实例
        /// </summary>
        public UiLifeCycle<T> Show<T>(params object[] args) where T : IBaseUI
        {
            var uiType = typeof(T);
            var config = UIProjectConfigManager.GetUIInstanceConfig(uiType);
            
            // 多实例模式：自动生成实例ID
            if (config?.InstanceStrategy == UIInstanceStrategy.Multiple)
            {
                var autoInstanceId = GenerateAutoInstanceId(uiType);
                return Show<T>(autoInstanceId, args);
            }
            
            // 单例模式：使用null作为instanceId
            return Show<T>(null, args);
        }
        
        /// <summary>
        /// 显示UI（手动指定实例ID）
        /// 注意：单例模式会忽略instanceId参数
        /// </summary>
        /// <param name="instanceId">实例ID，null表示使用默认实例</param>
        /// <param name="args">传递给UI的参数</param>
        public UiLifeCycle<T> Show<T>(string instanceId, params object[] args) where T : IBaseUI
        {
            var uiType = typeof(T);
            var config = UIProjectConfigManager.GetUIInstanceConfig(uiType);
            
            // 单例模式下，忽略instanceId，并刷新层级
            if (config?.InstanceStrategy == UIInstanceStrategy.Singleton)
            {
                instanceId = null;
            }
            
            var uiKey = new UIInstanceKey(uiType, instanceId);
            FrameworkLogger.Info($"[UICenter] 请求显示UI: {uiKey}");

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
                FrameworkLogger.Info($"[UICenter] 创建新UI实例: {uiKey}");
                var ui = _container.Resolve<PlaceholderFactory<Type, IBaseUI>>().Create(uiType);
                ui.Initialize();
                uiState.Ui = ui;
                
                // 设置实例ID（用于多实例模式下UI自己调用Hide）
                if (ui is UIBehaviour uiBehaviour)
                {
                    uiBehaviour.InstanceId = uiKey.InstanceId;
                }
                
                // 添加到实例管理器
                _instanceManager.AddInstance(uiKey.UIType, ui);
                
                // 正确等待异步方法（修复Bug #2）
                _ = CreateAndShowAsync(ui, args, uiState, uiKey);
            }
            else
            {
                FrameworkLogger.Info($"[UICenter] 复用已有UI实例: {uiKey}");
                
                // 单例模式：刷新层级（置顶）
                if (config?.InstanceStrategy == UIInstanceStrategy.Singleton && uiState.Ui is UIBehaviour ugui)
                {
                    BringUIToFront(ugui);
                    FrameworkLogger.Info($"[UICenter] 单例UI刷新层级: {uiKey}");
                }
                
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
        private async Task CreateAndShowAsync(IBaseUI ui, object[] args, UiState uiState, UIInstanceKey uiKey)
        {
            try
            {
                _stateManager.SetState(uiKey.UIType, UIRuntimeState.Creating);
                
                await CreateAsync(ui, args, uiState, uiKey);
                await ShowAsync(ui, args, uiState, uiKey);
                await ShowAnimAsync(ui, args, uiState, uiKey);
                
                _stateManager.SetState(uiKey.UIType, UIRuntimeState.Showing);
                
                FrameworkLogger.Info($"[UICenter] UI显示完成: {uiKey}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI显示异常: {uiKey}, {ex.Message}\n{ex.StackTrace}");
                RemoveUi(uiKey, ex);
            }
        }
        
        /// <summary>
        /// 只显示UI（复用已有实例）
        /// </summary>
        private async Task ShowOnlyAsync(IBaseUI ui, object[] args, UiState uiState, UIInstanceKey uiKey)
        {
            try
            {
                await ShowAsync(ui, args, uiState, uiKey);
                await ShowAnimAsync(ui, args, uiState, uiKey);
                
                _stateManager.SetState(uiKey.UIType, UIRuntimeState.Showing);
                
                FrameworkLogger.Info($"[UICenter] UI显示完成: {uiKey}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI显示异常: {uiKey}, {ex.Message}");
                uiState.ShowTcs?.TrySetException(ex);
            }
        }

        private async Task CreateAsync(IBaseUI ui, object[] args, UiState uiState, UIInstanceKey uiKey)
        {
            try
            {
                var result = await ui.DoCreate(args);
                uiState.CreateTcs?.TrySetResult(result);
                
                // 发送创建事件
                EventBus.Emit(GlobalEventType.UI, GlobalEventType.UIEvent.Create, uiKey.ToString());
                
                FrameworkLogger.Info($"[UICenter] UI创建成功: {uiKey}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI创建失败: {uiKey}, {ex.Message}");
                uiState.CreateTcs?.TrySetException(ex);
                throw;
            }
        }

        private async Task ShowAsync(IBaseUI ui, object[] args, UiState uiState, UIInstanceKey uiKey)
        {
            try
            {
                var result = await ui.DoShow(args);
                uiState.ShowTcs?.TrySetResult(result);
                
                // 分配层级（使用增强的排序逻辑）
                if (ui is UIBehaviour ugui)
                {
                    AssignSortingOrder(ugui);
                }
                
                // 发送显示事件
                EventBus.Emit(GlobalEventType.UI, GlobalEventType.UIEvent.Show, uiKey.ToString());
                
                FrameworkLogger.Info($"[UICenter] UI显示成功: {uiKey}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI显示失败: {uiKey}, {ex.Message}");
                uiState.ShowTcs?.TrySetException(ex);
                throw;
            }
        }

        private async Task ShowAnimAsync(IBaseUI ui, object[] args, UiState uiState, UIInstanceKey uiKey)
        {
            try
            {
                var result = await ui.DoShowAnim(args);
                uiState.ReadyTcs?.TrySetResult(result);
                
                // 发送动画完成事件（复用Ready事件）
                EventBus.Emit(GlobalEventType.UI, GlobalEventType.UIEvent.Ready, uiKey.ToString());
                
                FrameworkLogger.Info($"[UICenter] UI显示动画完成: {uiKey}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI显示动画失败: {uiKey}, {ex.Message}");
                uiState.ReadyTcs?.TrySetException(ex);
                throw;
            }
        }

        /// <summary>
        /// 隐藏UI（单例或默认实例）
        /// </summary>
        Task<object> IUI.Hide<T>(params object[] args)
        {
            return Hide<T>(null, args);
        }
        
        /// <summary>
        /// 隐藏UI（单例或默认实例）- 公共实现
        /// </summary>
        public Task<object> Hide<T>(params object[] args) where T : IBaseUI
        {
            return Hide<T>(null, args);
        }
        
        /// <summary>
        /// 隐藏UI（支持多实例）
        /// </summary>
        public Task<object> Hide<T>(string instanceId, params object[] args) where T : IBaseUI
        {
            var uiKey = new UIInstanceKey(typeof(T), instanceId);
            return Hide(uiKey, args);
        }

        /// <summary>
        /// 隐藏UI（通过Type）
        /// </summary>
        public Task<object> Hide(Type uiType, params object[] args)
        {
            return Hide(new UIInstanceKey(uiType, null), args);
        }
        
        /// <summary>
        /// 隐藏UI（通过Type和实例ID）
        /// </summary>
        public Task<object> Hide(Type uiType, string instanceId, params object[] args)
        {
            return Hide(new UIInstanceKey(uiType, instanceId), args);
        }

        /// <summary>
        /// 隐藏UI（通过UIInstanceKey）
        /// </summary>
        private async Task<object> Hide(UIInstanceKey uiKey, params object[] args)
        {
            FrameworkLogger.Info($"[UICenter] 请求隐藏UI: {uiKey}");

            if (!_uiStates.TryGetValue(uiKey, out var uiState))
            {
                FrameworkLogger.Warn($"[UICenter] UI不存在: {uiKey}");
                return null;
            }

            if (uiState.Ui != null)
            {
                return await HideAsync(uiState.Ui, args, uiState, uiKey);
            }

            FrameworkLogger.Warn($"[UICenter] UI实例为空: {uiKey}");
            return null;
        }

        /// <summary>
        /// 执行隐藏操作
        /// </summary>
        private async Task<object> HideAsync(IBaseUI ui, object[] args, UiState uiState, UIInstanceKey uiKey)
        {
            try
            {
                _stateManager.SetState(uiKey.UIType, UIRuntimeState.Hidden);
                
                // 调用 OnHide
                await ui.DoHide(args);
                
                // 播放隐藏动画
                await ui.DoHideAnim(args);
                
                // 设置结果
                uiState.HideTcs?.TrySetResult(null);
                
                // 发送隐藏事件
                EventBus.Emit(GlobalEventType.UI, GlobalEventType.UIEvent.Hide, uiKey.ToString());
                
                FrameworkLogger.Info($"[UICenter] UI隐藏成功: {uiKey}");
                
                // 根据缓存策略决定是否销毁
                var config = UIProjectConfigManager.GetUIInstanceConfig(uiKey.UIType);
                if (config?.CacheStrategy == UICacheStrategy.NeverCache)
                {
                    await DestroyUI(uiKey);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI隐藏失败: {uiKey}, {ex.Message}");
                uiState.HideTcs?.TrySetException(ex);
                throw;
            }
        }
        
        #endregion
        
        #region 实例管理
        
        /// <summary>
        /// 销毁指定UI（单例或默认实例）
        /// </summary>
        public async Task DestroyUI<T>() where T : IBaseUI
        {
            await DestroyUI<T>(null);
        }
        
        /// <summary>
        /// 销毁指定UI（支持多实例）
        /// </summary>
        public async Task DestroyUI<T>(string instanceId) where T : IBaseUI
        {
            var uiKey = new UIInstanceKey(typeof(T), instanceId);
            await DestroyUI(uiKey);
        }
        
        /// <summary>
        /// 销毁指定UI（内部方法）
        /// </summary>
        private async Task DestroyUI(UIInstanceKey uiKey)
        {
            FrameworkLogger.Info($"[UICenter] 请求销毁UI: {uiKey}");
            
            if (!_uiStates.TryRemove(uiKey, out var uiState))
            {
                FrameworkLogger.Warn($"[UICenter] UI不存在: {uiKey}");
                return;
            }
            
            if (uiState.Ui != null)
            {
                try
                {
                    _stateManager.SetState(uiKey.UIType, UIRuntimeState.Destroying);
                    
                    await uiState.Ui.DoDestroy();
                    
                    // 发送销毁事件
                    EventBus.Emit(GlobalEventType.UI, GlobalEventType.UIEvent.Destroy, uiKey.ToString());
                    
                    // 从管理器中移除
                    _instanceManager.RemoveInstance(uiKey.UIType);
                    _layerManager.ReleaseLayer(uiKey.UIType);
                    _stateManager.SetState(uiKey.UIType, UIRuntimeState.Destroyed);
                    
                    FrameworkLogger.Info($"[UICenter] UI销毁成功: {uiKey}");
                }
                catch (Exception ex)
                {
                    FrameworkLogger.Error($"[UICenter] UI销毁失败: {uiKey}, {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 销毁指定类型的所有实例
        /// </summary>
        public async Task DestroyAllInstancesOf<T>() where T : IBaseUI
        {
            var uiType = typeof(T);
            FrameworkLogger.Info($"[UICenter] 销毁所有实例: {uiType.Name}");
            
            // 查找该类型的所有实例
            var keysToDestroy = _uiStates.Keys.Where(key => key.UIType == uiType).ToList();
            
            if (keysToDestroy.Count == 0)
            {
                FrameworkLogger.Warn($"[UICenter] 没有找到任何实例: {uiType.Name}");
                return;
            }
            
            FrameworkLogger.Info($"[UICenter] 找到 {keysToDestroy.Count} 个实例，开始销毁");
            
            // 销毁所有实例
            foreach (var key in keysToDestroy)
            {
                await DestroyUI(key);
            }
            
            FrameworkLogger.Info($"[UICenter] {uiType.Name} 所有实例销毁完成");
        }
        
        /// <summary>
        /// 销毁所有UI
        /// </summary>
        public async Task DestroyAllUI()
        {
            FrameworkLogger.Info($"[UICenter] 销毁所有UI，总数: {_uiStates.Count}");
            
            var uiKeys = _uiStates.Keys.ToList();
            foreach (var uiKey in uiKeys)
            {
                await DestroyUI(uiKey);
            }
            
            // 清空管理器
            _instanceManager.Clear();
            _layerManager.Clear();
            _stateManager.Clear();
            _stackManager.Clear();
            _autoInstanceCounters.Clear();
        }
        
        /// <summary>
        /// 获取UI实例（单例或第一个实例）
        /// </summary>
        public T GetUI<T>() where T : IBaseUI
        {
            var uiType = typeof(T);
            
            // 尝试获取单例实例
            var singletonKey = new UIInstanceKey(uiType, null);
            if (_uiStates.TryGetValue(singletonKey, out var state) && state.Ui is T)
            {
                return (T)state.Ui;
            }
            
            // 如果不是单例，返回第一个找到的实例
            var firstInstance = _uiStates.Keys
                .Where(k => k.UIType == uiType)
                .Select(k => _uiStates.TryGetValue(k, out var s) ? s.Ui : null)
                .FirstOrDefault(ui => ui is T);
            
            return firstInstance is T ? (T)firstInstance : default;
        }
        
        /// <summary>
        /// 获取UI实例（支持多实例）
        /// </summary>
        public T GetUI<T>(string instanceId) where T : IBaseUI
        {
            var uiKey = new UIInstanceKey(typeof(T), instanceId);
            if (_uiStates.TryGetValue(uiKey, out var state))
            {
                return state.Ui is T ? (T)state.Ui : default;
            }
            return default;
        }
        
        /// <summary>
        /// 检查UI是否正在显示（单例或是否有任何实例显示）
        /// </summary>
        public bool IsShowing<T>() where T : IBaseUI
        {
            var uiType = typeof(T);
            
            // 检查是否有任何该类型的实例在显示
            return _uiStates.Keys
                .Where(k => k.UIType == uiType)
                .Any(k => _stateManager.IsShowing(k.UIType));
        }
        
        /// <summary>
        /// 检查UI实例是否正在显示（支持多实例）
        /// </summary>
        public bool IsShowing<T>(string instanceId) where T : IBaseUI
        {
            var uiKey = new UIInstanceKey(typeof(T), instanceId);
            return _uiStates.ContainsKey(uiKey) && _stateManager.IsShowing(uiKey.UIType);
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
        /// 注意：只加载Prefab资源到内存，不实例化GameObject，不执行业务逻辑
        /// 适用于单例和多实例UI
        /// </summary>
        public async Task PreloadUI<T>() where T : IBaseUI
        {
            await Task.CompletedTask; // 保持异步签名
            
            var uiType = typeof(T);
            var config = UIProjectConfigManager.GetUIInstanceConfig(uiType);
            
            if (config == null || string.IsNullOrEmpty(config.ResourcePath))
            {
                FrameworkLogger.Warn($"[UICenter] 预加载失败：未找到UI配置或资源路径: {uiType.Name}");
                return;
            }
            
            try
            {
                FrameworkLogger.Info($"[UICenter] 预加载UI资源: {uiType.Name} <- {config.ResourcePath}");
                
                // 使用框架的资源系统加载Prefab（不实例化）
                var prefab = await _resource.LoadAsync<GameObject>(config.ResourcePath);
                
                if (prefab == null)
                {
                    FrameworkLogger.Error($"[UICenter] 预加载失败：无法加载Prefab: {config.ResourcePath}");
                    return;
                }
                
                FrameworkLogger.Info($"[UICenter] UI资源预加载完成: {uiType.Name}");
                FrameworkLogger.Info($"[UICenter] Prefab已缓存到框架资源系统，后续实例化时将直接使用");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UICenter] UI资源预加载失败: {uiType.Name}, {ex.Message}");
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
            if (ui is UIBehaviour ugui)
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
            if (ui is UIBehaviour ugui)
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
        /// 分配sortingOrder（从SortUIAttachment迁移）
        /// </summary>
        private void AssignSortingOrder(UIBehaviour ui)
        {
            var layerName = ui.LayerName ?? "Main";
            var baseSortingOrder = UIProjectConfigManager.GetBaseSortingOrder(layerName);
            
            // 获取该层级当前的计数器
            if (!_layerCounters.TryGetValue(layerName, out var counter))
            {
                counter = 0;
            }
            
            // 同层级UI按打开顺序递增sortingOrder
            var finalSortingOrder = baseSortingOrder + counter;
            ui.SetIndex(finalSortingOrder);
            
            // 递增计数器
            _layerCounters[layerName] = counter + 1;
            
            FrameworkLogger.Info($"[UICenter] 分配sortingOrder: {ui.GetType().Name}, Layer={layerName}, BaseSortingOrder={baseSortingOrder}, FinalSortingOrder={finalSortingOrder}");
        }
        
        /// <summary>
        /// 将UI置顶（刷新层级）
        /// </summary>
        private void BringUIToFront(UIBehaviour ui)
        {
            var layerName = ui.LayerName ?? "Main";
            var baseSortingOrder = UIProjectConfigManager.GetBaseSortingOrder(layerName);
            
            // 获取该层级当前的计数器
            if (!_layerCounters.TryGetValue(layerName, out var counter))
            {
                counter = 0;
            }
            
            // 使用最新的sortingOrder置顶
            var finalSortingOrder = baseSortingOrder + counter;
            ui.SetIndex(finalSortingOrder);
            
            // 递增计数器
            _layerCounters[layerName] = counter + 1;
            
            FrameworkLogger.Info($"[UICenter] 刷新UI层级（置顶）: {ui.GetType().Name}, Layer={layerName}, FinalSortingOrder={finalSortingOrder}");
        }
        
        /// <summary>
        /// 生成自动实例ID
        /// </summary>
        private string GenerateAutoInstanceId(Type uiType)
        {
            if (!_autoInstanceCounters.TryGetValue(uiType, out var counter))
            {
                counter = 0;
            }
            
            counter++;
            _autoInstanceCounters[uiType] = counter;
            
            var instanceId = $"{AUTO_INSTANCE_PREFIX}{counter}";
            FrameworkLogger.Info($"[UICenter] 生成自动实例ID: {uiType.Name} -> {instanceId}");
            
            return instanceId;
        }
        
        /// <summary>
        /// 移除UI
        /// </summary>
        private void RemoveUi(UIInstanceKey uiKey, Exception exception = null)
        {
            if (!_uiStates.TryRemove(uiKey, out var uiState)) return;

            if (uiState.Ui is IDisposable disposable)
            {
                FrameworkLogger.Info($"[UICenter] 释放UI资源: {uiKey}");
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
                    FrameworkLogger.Error($"[UICenter] UI操作异常: {uiKey}, {exception.Message}");
                    tcs.TrySetException(exception);
                }
                else
                {
                    tcs.TrySetCanceled();
                }
            }
            
            // 从管理器中移除
            _instanceManager.RemoveInstance(uiKey.UIType);
            _layerManager.ReleaseLayer(uiKey.UIType);
            _stateManager.RemoveState(uiKey.UIType);
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