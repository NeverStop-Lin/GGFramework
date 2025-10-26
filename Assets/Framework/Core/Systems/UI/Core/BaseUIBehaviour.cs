using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Framework.Core
{
    /// <summary>
    /// MonoBehaviour版本的BaseUI
    /// 提供Pipeline管道、生命周期管理、Attachment机制
    /// </summary>
    public abstract class BaseUIBehaviour : MonoBehaviour, IBaseUI
    {
        #region 依赖注入
        
        [Inject]
        protected IUI Center;
        
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
            // 初始化Pipeline（原本在Initialize中）
            InitializePipeline();
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
        
        #region 子类重写方法
        
        /// <summary>
        /// 子类可以重写此方法来添加自定义Attachment
        /// </summary>
        protected virtual void OnAttachmentInitialize(List<UIAttachment> attachments) { }
        
        /// <summary>
        /// UI创建时调用
        /// </summary>
        protected virtual void OnCreate(params object[] args) { }
        
        /// <summary>
        /// UI显示时调用
        /// </summary>
        protected virtual void OnShow(params object[] args) { }
        
        /// <summary>
        /// UI就绪时调用
        /// </summary>
        protected virtual void OnReady(params object[] args) { }
        
        /// <summary>
        /// UI隐藏时调用
        /// </summary>
        protected virtual void OnHide(params object[] args) { }
        
        /// <summary>
        /// UI销毁时调用
        /// </summary>
        protected virtual void OnDestroy(params object[] args) { }
        
        #endregion
        
        #region Attachment适配器
        
        /// <summary>
        /// 将BaseUIBehaviour适配为UIAttachment
        /// 用于将子类的OnCreate/OnShow等方法注入Pipeline
        /// </summary>
        private class SelfAttachmentAdapter : UIAttachment
        {
            private readonly BaseUIBehaviour _ui;
            
            public SelfAttachmentAdapter(BaseUIBehaviour ui)
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
        
        #region 层级管理（抽象方法，由子类实现）
        
        /// <summary>
        /// 获取UI的层级索引
        /// </summary>
        public abstract int GetIndex();
        
        /// <summary>
        /// 设置UI的层级索引
        /// </summary>
        public abstract void SetIndex(int i);
        
        #endregion
    }
}

