using System;
using System.Threading.Tasks;

namespace Framework.Core
{
    public class UIAttachment
    {

        PipelineContext _curContext;

        protected BaseUI Target
        {
            get { return (BaseUI)_curContext.Data["target"]; }
        }

        /// <summary>
        /// ����UI�����¼�
        /// </summary>
        /// <param name="context">�ܵ�������</param>
        /// <param name="next">����ִ�йܵ���ί��</param>
        /// <returns>����</returns>
        public async Task OnCreate(PipelineContext context, Func<Task> next)
        {
            _curContext = context;
            await OnBeforeCreate(context);
            await next();
            await OnAfterCreate(context);
        }

        /// <summary>
        /// ��������д�Ĵ����¼������߼�
        /// </summary>
        protected virtual Task OnBeforeCreate(PipelineContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// ��������д�Ĵ������¼������߼�
        /// </summary>
        protected virtual Task OnAfterCreate(PipelineContext context) { return Task.CompletedTask; }

        /// <summary>
        /// ����UI��ʾ�¼�
        /// </summary>
        /// <param name="context">�ܵ�������</param>
        /// <param name="next">����ִ�йܵ���ί��</param>
        /// <returns>����</returns>
        public async Task OnShow(PipelineContext context, Func<Task> next)
        {
            _curContext = context;
            await OnBeforeShow(context);
            await next();
            await OnAfterShow(context);
        }

        /// <summary>
        /// ��������д����ʾ�¼������߼�
        /// </summary>
        protected virtual Task OnBeforeShow(PipelineContext context) { return Task.CompletedTask; }

        /// <summary>
        /// ��������д����ʾ���¼������߼�
        /// </summary>
        protected virtual Task OnAfterShow(PipelineContext context) { return Task.CompletedTask; }

        /// <summary>
        /// ����UI׼������¼�
        /// </summary>
        /// <param name="context">�ܵ�������</param>
        /// <param name="next">����ִ�йܵ���ί��</param>
        /// <returns>����</returns>
        public async Task OnReady(PipelineContext context, Func<Task> next)
        {
            _curContext = context;
            await OnBeforeReady(context);
            await next();
            await OnAfterReady(context);
        }

        /// <summary>
        /// ��������д��׼������¼������߼�
        /// </summary>
        protected virtual Task OnBeforeReady(PipelineContext context) { return Task.CompletedTask; }

        /// <summary>
        /// ��������д��׼����ɺ��¼������߼�
        /// </summary>
        protected virtual Task OnAfterReady(PipelineContext context) { return Task.CompletedTask; }

        /// <summary>
        /// ����UI�����¼�
        /// </summary>
        /// <param name="context">�ܵ�������</param>
        /// <param name="next">����ִ�йܵ���ί��</param>
        /// <returns>����</returns>
        public async Task OnHide(PipelineContext context, Func<Task> next)
        {
            _curContext = context;
            await OnBeforeHide(context);
            await next();
            await OnAfterHide(context);
        }

        /// <summary>
        /// ��������д�������¼������߼�
        /// </summary>
        protected virtual Task OnBeforeHide(PipelineContext context) { return Task.CompletedTask; }

        /// <summary>
        /// ��������д�����غ��¼������߼�
        /// </summary>
        protected virtual Task OnAfterHide(PipelineContext context) { return Task.CompletedTask; }

        /// <summary>
        /// ����UI�����¼�
        /// </summary>
        /// <param name="context">�ܵ�������</param>
        /// <param name="next">����ִ�йܵ���ί��</param>
        /// <returns>����</returns>
        public async Task OnDestroy(PipelineContext context, Func<Task> next)
        {
            _curContext = context;
            await OnBeforeDestroy(context);
            await next();
            await OnAfterDestroy(context);
        }

        /// <summary>
        /// ��������д�������¼������߼�
        /// </summary>
        protected virtual Task OnBeforeDestroy(PipelineContext context)
        {
            _curContext = context;
            return Task.CompletedTask;
        }

        /// <summary>
        /// ��������д�����ٺ��¼������߼�
        /// </summary>
        protected virtual Task OnAfterDestroy(PipelineContext context)
        {
            return Task.CompletedTask;
        }
    }
}