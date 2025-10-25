using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using Zenject;

namespace Framework.Core
{
    public abstract class BaseUI : UIAttachment, IBaseUI
    {

        [Inject]
        protected IUI Center;

        private readonly List<UIAttachment> _attachments = new List<UIAttachment>();
        private readonly Dictionary<UIState, AsyncPipeline> _uiPipelines = new Dictionary<UIState, AsyncPipeline>();
        protected UIState uiState = UIState.None;

        // ״̬��������ӳ���
        readonly Dictionary<UIState, PipelineContext> _pipelineContext = new Dictionary<UIState, PipelineContext>();

        public readonly ActionUiAttachment Action = new ActionUiAttachment();

        public UIAlignType AlignType = UIAlignType.Center;
        public UIType UIType = UIType.Main;

        public object[] GetParams(UIState state)
        {
            return _pipelineContext.TryGetValue(state, out var context)
                ? context.Data.TryGetValue("params", out var value) && value is object[] args
                    ? args
                    : Array.Empty<object>()
                : Array.Empty<object>();
        }

        public void Initialize()
        {


            // ����Ĭ�ϵĸ���
            _attachments.Add(Action);
            _attachments.Add(new EmitLifeCycleUIAttachment());
            _attachments.Add(new SortUIAttachment());
            // ������������
            OnAttachmentInitialize(_attachments);
            // ��������
            _attachments.Add(this);
            ResetPipeline();
        }

        protected void ResetPipeline()
        {
            // ״̬-�м��ӳ������
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

                // ���Ӹ����м��
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

        #region center �ӿ�

        public async Task<object> DoCreate(params object[] args)
        {
            return await ExecuteStatePipelineAsync(UIState.Create, args);
        }
        public async Task<object> DoShow(params object[] args)
        {
            return await ExecuteStatePipelineAsync(UIState.Show, args);
        }

        public async Task<object> DoReady(params object[] args)
        {
            return await ExecuteStatePipelineAsync(UIState.Ready, args);
        }

        public async Task<object> DoHide(params object[] args)
        {
            return await ExecuteStatePipelineAsync(UIState.Hide, args);
        }
        public async Task<object> DoDestroy(params object[] args)
        {
            return await ExecuteStatePipelineAsync(UIState.Destroy, args);
        }

        #endregion

        #region ��������

        public Task<object> Hide(params object[] args) { return Center.Hide(this.GetType(), args); }

        #endregion


        #region ˽�и�������

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

        protected void SetResult(params object[] args)
        {
            if (_pipelineContext.TryGetValue(uiState, out var context))
            {
                context.Result = args;
            }
        }

        private static object[] GetParameters(PipelineContext context)
        {
            return context.Data.TryGetValue("params", out var value) && value is object[] args
                ? args
                : Array.Empty<object>();
        }

        #endregion

        #region ����д���������ڷ���

        protected virtual void OnAttachmentInitialize(List<UIAttachment> attachments) { }
        protected virtual void OnCreate(params object[] args) { }
        protected virtual void OnShow(params object[] args) { }
        protected virtual void OnReady(params object[] args) { }
        protected virtual void OnHide(params object[] args) { }
        protected virtual void OnDestroy(params object[] args) { }

        #endregion

        #region �������� & ״̬�Ĵ��� @ ��������

        /// <summary>
        /// ����ʾUI֮ǰִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnBeforeShow(PipelineContext context)
        {
            uiState = UIState.Show;
            OnShow(GetParameters(context));
            return base.OnBeforeShow(context);
        }
        /// <summary>
        /// �ڴ���UI֮ǰִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnBeforeCreate(PipelineContext context)
        {
            uiState = UIState.Create;
            OnCreate(GetParameters(context));
            return base.OnBeforeCreate(context);
        }
        /// <summary>
        /// ������UI֮ǰִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnBeforeHide(PipelineContext context)
        {
            uiState = UIState.Hide;
            OnHide(GetParameters(context));
            return base.OnBeforeHide(context);
        }
        /// <summary>
        /// ������UI֮ǰִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnBeforeDestroy(PipelineContext context)
        {
            uiState = UIState.Destroy;
            OnDestroy(GetParameters(context));
            return base.OnBeforeDestroy(context);
        }

        /// <summary>
        /// ��UI׼����֮ǰִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnBeforeReady(PipelineContext context)
        {
            uiState = UIState.Ready;
            OnReady(GetParameters(context));
            return base.OnBeforeReady(context);
        }

        #endregion

        public abstract int GetIndex();
        public abstract void SetIndex(int i);

    }
}