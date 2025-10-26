using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.Scripts;
using UnityEngine;
using Zenject;

namespace Framework.Core
{
    /// <summary>
    /// UI基类（MonoBehaviour + UGUI）
    /// 提供Pipeline管道、生命周期管理、Attachment机制、UGUI组件绑定
    /// </summary>
    public abstract class UIBehaviour : MonoBehaviour, IBaseUI
    {
        #region 依赖注入
        
        [Inject]
        protected IUI Center;
        
        #endregion
        
        #region UGUI组件
        
        /// <summary>
        /// UI的Canvas组件
        /// </summary>
        protected Canvas Canvas;
        
        /// <summary>
        /// UI的RectTransform组件
        /// </summary>
        protected RectTransform RectTransform;
        
        /// <summary>
        /// UI配置
        /// </summary>
        private UIConfig _config;
        
        #endregion
        
        #region Pipeline系统
        
        private readonly List<UIAttachment> _attachments = new List<UIAttachment>();
        private readonly Dictionary<UIState, AsyncPipeline> _uiPipelines = new Dictionary<UIState, AsyncPipeline>();
        protected UIState uiState = UIState.None;
        
        // 状态参数上下文映射
        private readonly Dictionary<UIState, PipelineContext> _pipelineContext = new Dictionary<UIState, PipelineContext>();
        
        public readonly ActionUiAttachment Action = new ActionUiAttachment();
        
        /// <summary>
        /// UI对齐类型（实现IBaseUI接口）
        /// </summary>
        public UIAlignType AlignType { get; set; } = UIAlignType.Center;
        
        /// <summary>
        /// UI类型（实现IBaseUI接口）
        /// </summary>
        public UIType UIType { get; set; } = UIType.Main;
        
        #endregion
        
        #region Unity生命周期
        
        /// <summary>
        /// Unity Awake钩子（MonoBehaviour创建时）
        /// 子类重写时必须调用base.Awake()
        /// </summary>
        protected virtual void Awake()
        {
            // 初始化Pipeline
            InitializePipeline();
            
            // 获取UGUI组件
            Canvas = GetComponent<Canvas>();
            if (Canvas == null)
            {
                Canvas = GetComponentInChildren<Canvas>();
            }
            
            RectTransform = GetComponent<RectTransform>();
            
            // 获取配置
            _config = GetFinalConfig();
            
            // 绑定组件（自动生成的代码会重写此方法）
            BindComponents();
        }
        
        /// <summary>
        /// Unity OnEnable钩子（GameObject激活时）
        /// </summary>
        protected virtual void OnEnable()
        {
            // 注册事件
            RegisterEvents();
        }
        
        /// <summary>
        /// Unity OnDisable钩子（GameObject禁用时）
        /// </summary>
        protected virtual void OnDisable()
        {
            // 注销事件
            UnregisterEvents();
        }
        
        /// <summary>
        /// Unity OnDestroy钩子（GameObject销毁时）
        /// 自动触发Destroy Pipeline
        /// 使用new关键字明确隐藏MonoBehaviour.OnDestroy
        /// </summary>
        protected virtual new void OnDestroy()
        {
            // 如果是通过DoDestroy销毁的，不要重复执行
            if (uiState == UIState.Destroy)
            {
                return;
            }
            
            // Unity直接销毁GameObject时，触发Destroy Pipeline
            _ = DoDestroy();
        }
        
        #endregion
        
        #region IBaseUI接口实现
        
        /// <summary>
        /// 初始化UI
        /// </summary>
        public void Initialize()
        {
            // MonoBehaviour版本中，大部分初始化在Awake中完成
            // UICenter调用此方法时，确保Pipeline已初始化
            if (_uiPipelines.Count == 0)
            {
                InitializePipeline();
            }
        }
        
        /// <summary>
        /// 执行Create Pipeline
        /// </summary>
        public async Task<object> DoCreate(params object[] args)
        {
            return await ExecuteStatePipelineAsync(UIState.Create, args);
        }
        
        /// <summary>
        /// 执行Show Pipeline
        /// </summary>
        public async Task<object> DoShow(params object[] args)
        {
            // 激活GameObject
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            
            return await ExecuteStatePipelineAsync(UIState.Show, args);
        }
        
        /// <summary>
        /// 执行Ready Pipeline
        /// </summary>
        public async Task<object> DoReady(params object[] args)
        {
            return await ExecuteStatePipelineAsync(UIState.Ready, args);
        }
        
        /// <summary>
        /// 执行Hide Pipeline
        /// </summary>
        public async Task<object> DoHide(params object[] args)
        {
            var result = await ExecuteStatePipelineAsync(UIState.Hide, args);
            
            // 禁用GameObject
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
            
            return result;
        }
        
        /// <summary>
        /// 执行Destroy Pipeline
        /// </summary>
        public async Task<object> DoDestroy(params object[] args)
        {
            var result = await ExecuteStatePipelineAsync(UIState.Destroy, args);
            
            // 销毁GameObject
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
            
            return result;
        }
        
        #endregion
        
        #region 便捷方法
        
        /// <summary>
        /// 隐藏当前UI
        /// </summary>
        public Task<object> Hide(params object[] args)
        {
            return Center.Hide(this.GetType(), args);
        }
        
        /// <summary>
        /// 获取指定状态的参数
        /// </summary>
        public object[] GetParams(UIState state)
        {
            return _pipelineContext.TryGetValue(state, out var context)
                ? context.Data.TryGetValue("params", out var value) && value is object[] args
                    ? args
                    : Array.Empty<object>()
                : Array.Empty<object>();
        }
        
        #endregion
        
        #region Pipeline初始化
        
        /// <summary>
        /// 初始化Pipeline系统
        /// </summary>
        private void InitializePipeline()
        {
            // 添加默认的附件
            _attachments.Add(Action);
            _attachments.Add(new EmitLifeCycleUIAttachment());
            _attachments.Add(new SortUIAttachment());
            
            // 让子类添加自定义附件
            OnAttachmentInitialize(_attachments);
            
            // 将自己也作为附件
            _attachments.Add(new SelfAttachmentAdapter(this));
            
            // 重置Pipeline
            ResetPipeline();
        }
        
        /// <summary>
        /// 重置Pipeline
        /// </summary>
        protected void ResetPipeline()
        {
            // 状态-中间件映射配置
            var stateConfigurations
                = new (UIState State, Func<UIAttachment, Func<PipelineContext, Func<Task>, Task>> MiddlewareGetter)[]
                {
                    (UIState.Create, a => a.OnCreate),
                    (UIState.Show, a => a.OnShow),
                    (UIState.Ready, a => a.OnReady),
                    (UIState.Hide, a => a.OnHide),
                    (UIState.Destroy, a => a.OnDestroy)
                };
            
            foreach (var config in stateConfigurations)
            {
                var pipeline = new AsyncPipeline();
                _uiPipelines[config.State] = pipeline;
                
                // 添加各个中间件
                foreach (var attachment in _attachments)
                {
                    var middleware = config.MiddlewareGetter(attachment);
                    if (middleware != null)
                    {
                        pipeline.AddMiddleware(middleware);
                    }
                }
            }
        }
        
        #endregion
        
        #region Pipeline执行
        
        /// <summary>
        /// 执行状态Pipeline
        /// </summary>
        private async Task<object> ExecuteStatePipelineAsync(UIState state, object[] args)
        {
            var context = new PipelineContext
            {
                Data =
                {
                    ["params"] = args,
                    ["target"] = this
                }
            };
            
            _pipelineContext[state] = context;
            
            if (_uiPipelines.TryGetValue(state, out var pipeline))
            {
                await pipeline.ExecuteAsync(context);
            }
            
            return context.Result;
        }
        
        /// <summary>
        /// 设置Pipeline结果
        /// </summary>
        protected void SetResult(params object[] args)
        {
            if (_pipelineContext.TryGetValue(uiState, out var context))
            {
                context.Result = args;
            }
        }
        
        /// <summary>
        /// 从Context获取参数
        /// </summary>
        private static object[] GetParameters(PipelineContext context)
        {
            return context.Data.TryGetValue("params", out var value) && value is object[] args
                ? args
                : Array.Empty<object>();
        }
        
        #endregion
        
        #region 配置系统
        
        /// <summary>
        /// 创建UI配置（子类重写以提供自定义配置）
        /// </summary>
        protected virtual UIConfig CreateUIConfig()
        {
            return new UIConfig
            {
                UIType = UIType.Main,
                AlignType = UIAlignType.Center,
                CacheStrategy = UICacheStrategy.AlwaysCache
            };
        }
        
        /// <summary>
        /// 获取最终配置（合并代码配置和Manifest配置）
        /// </summary>
        private UIConfig GetFinalConfig()
        {
            // 代码配置
            var codeConfig = CreateUIConfig();
            
            // 从UIManifest获取运行时配置
            var manifestConfig = UIManifestManager.GetConfig(GetType());
            
            // 融合（运行时配置优先）
            return UIConfigMerger.Merge(codeConfig, manifestConfig);
        }
        
        #endregion
        
        #region 子类重写方法
        
        /// <summary>
        /// 子类可以重写此方法来添加自定义Attachment
        /// </summary>
        protected virtual void OnAttachmentInitialize(List<UIAttachment> attachments) { }
        
        /// <summary>
        /// UI创建时调用
        /// </summary>
        protected virtual void OnCreate(params object[] args)
        {
            try
            {
                // MonoBehaviour版本中，GameObject已经存在，不需要加载Prefab
                
                // 根据配置初始化Attachments
                InitializeAttachmentsByConfig();
                
                FrameworkLogger.Info($"[UI] UI创建成功: {GetType().Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UI] UI创建失败: {GetType().Name}, {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        /// <summary>
        /// UI显示时调用
        /// </summary>
        protected virtual void OnShow(params object[] args)
        {
            FrameworkLogger.Info($"[UI] UI显示: {GetType().Name}");
        }
        
        /// <summary>
        /// UI就绪时调用
        /// </summary>
        protected virtual void OnReady(params object[] args) { }
        
        /// <summary>
        /// UI隐藏时调用
        /// </summary>
        protected virtual void OnHide(params object[] args)
        {
            FrameworkLogger.Info($"[UI] UI隐藏: {GetType().Name}");
        }
        
        /// <summary>
        /// UI销毁时调用（Pipeline回调）
        /// </summary>
        protected virtual void OnDestroy(params object[] args)
        {
            // 清理资源
            Canvas = null;
            RectTransform = null;
            
            FrameworkLogger.Info($"[UI] UI销毁: {GetType().Name}");
        }
        
        /// <summary>
        /// 根据配置初始化Attachments
        /// </summary>
        private void InitializeAttachmentsByConfig()
        {
            if (_config == null) return;
            
            var attachments = new List<UIAttachment>();
            
            // 根据配置添加Attachment
            if (_config.UseMask)
            {
                // TODO: 添加MaskUIAttachment
            }
            
            if (_config.UseAnimation && _config.AnimationType != UIAnimationType.None && _config.AnimationType != UIAnimationType.Custom)
            {
                // TODO: 根据AnimationType创建对应的动画Attachment
            }
            
            // 让子类添加自定义Attachment
            OnAttachmentInitialize(attachments);
        }
        
        #endregion
        
        #region 组件绑定（子类重写）
        
        /// <summary>
        /// 绑定组件（自动生成的代码会重写此方法）
        /// </summary>
        protected virtual void BindComponents()
        {
            // 默认不执行任何操作
            // 自动生成的Binding代码会重写此方法
        }
        
        #endregion
        
        #region 事件管理（子类重写）
        
        /// <summary>
        /// 注册事件（自动生成的代码会重写此方法）
        /// </summary>
        protected virtual void RegisterEvents()
        {
            // 默认不执行任何操作
            // 自动生成的Binding代码会重写此方法
        }
        
        /// <summary>
        /// 注销事件（自动生成的代码会重写此方法）
        /// </summary>
        protected virtual void UnregisterEvents()
        {
            // 默认不执行任何操作
            // 自动生成的Binding代码会重写此方法
        }
        
        #endregion
        
        #region 组件查找
        
        /// <summary>
        /// 查找组件（记录完整路径，精确查找）
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="path">相对于当前GameObject的路径</param>
        /// <returns>找到的组件</returns>
        /// <exception cref="Exception">找不到节点或组件时抛出异常</exception>
        protected T FindComponent<T>(string path) where T : Component
        {
            // 查找节点
            var trans = transform.Find(path);
            if (trans == null)
            {
                throw new Exception($"[UI] 找不到节点: {path} in {GetType().Name}");
            }
            
            // 获取组件
            var component = trans.GetComponent<T>();
            if (component == null)
            {
                throw new Exception($"[UI] 找不到组件: {typeof(T).Name} at {path} in {GetType().Name}");
            }
            
            return component;
        }
        
        /// <summary>
        /// 尝试查找组件（不抛异常，找不到返回null）
        /// </summary>
        protected T TryFindComponent<T>(string path) where T : Component
        {
            var trans = transform.Find(path);
            return trans?.GetComponent<T>();
        }
        
        #endregion
        
        #region Attachment适配器
        
        /// <summary>
        /// 将UIBehaviour适配为UIAttachment
        /// 用于将子类的OnCreate/OnShow等方法注入Pipeline
        /// </summary>
        private class SelfAttachmentAdapter : UIAttachment
        {
            private readonly UIBehaviour _ui;
            
            public SelfAttachmentAdapter(UIBehaviour ui)
            {
                _ui = ui;
            }
            
            protected override Task OnBeforeCreate(PipelineContext context)
            {
                _ui.uiState = UIState.Create;
                _ui.OnCreate(GetParameters(context));
                return Task.CompletedTask;
            }
            
            protected override Task OnBeforeShow(PipelineContext context)
            {
                _ui.uiState = UIState.Show;
                _ui.OnShow(GetParameters(context));
                return Task.CompletedTask;
            }
            
            protected override Task OnBeforeReady(PipelineContext context)
            {
                _ui.uiState = UIState.Ready;
                _ui.OnReady(GetParameters(context));
                return Task.CompletedTask;
            }
            
            protected override Task OnBeforeHide(PipelineContext context)
            {
                _ui.uiState = UIState.Hide;
                _ui.OnHide(GetParameters(context));
                return Task.CompletedTask;
            }
            
            protected override Task OnBeforeDestroy(PipelineContext context)
            {
                _ui.uiState = UIState.Destroy;
                _ui.OnDestroy(GetParameters(context));
                return Task.CompletedTask;
            }
            
            private static object[] GetParameters(PipelineContext context)
            {
                return context.Data.TryGetValue("params", out var value) && value is object[] args
                    ? args
                    : Array.Empty<object>();
            }
        }
        
        #endregion
        
        #region 层级管理
        
        /// <summary>
        /// 获取UI的层级索引（基于Canvas的sortingOrder）
        /// </summary>
        public virtual int GetIndex()
        {
            return Canvas != null ? Canvas.sortingOrder : 0;
        }
        
        /// <summary>
        /// 设置UI的层级索引（通过修改Canvas的sortingOrder）
        /// </summary>
        public virtual void SetIndex(int i)
        {
            if (Canvas != null)
            {
                Canvas.sortingOrder = i;
            }
        }
        
        #endregion
    }
}


