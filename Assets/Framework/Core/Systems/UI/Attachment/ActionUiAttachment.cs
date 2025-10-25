using System;
using System.Threading.Tasks;

namespace Framework.Core
{
    /// <summary>
    /// ��ʾһ������UI�����ĸ������ṩ�ڲ�ͬ�������ڽ׶�ִ���Զ��������������
    /// </summary>
    public class ActionUiAttachment : UIAttachment
    {
        #region Fields

        /// <summary>
        /// �ڴ���UI֮ǰִ�еĲ������ϡ�
        /// </summary>
        public Actions<object> BeforeCreateAction { get; } = new Actions<object>();

        /// <summary>
        /// �ڴ���UI֮��ִ�еĲ������ϡ�
        /// </summary>
        public Actions<object> AfterCreateAction { get; } = new Actions<object>();

        /// <summary>
        /// ����ʾUI֮ǰִ�еĲ������ϡ�
        /// </summary>
        public Actions<object> BeforeShowAction { get; } = new Actions<object>();

        /// <summary>
        /// ����ʾUI֮��ִ�еĲ������ϡ�
        /// </summary>
        public Actions<object> AfterShowAction { get; } = new Actions<object>();

        /// <summary>
        /// ������UI֮ǰִ�еĲ������ϡ�
        /// </summary>
        public Actions<object> BeforeHideAction { get; } = new Actions<object>();

        /// <summary>
        /// ������UI֮��ִ�еĲ������ϡ�
        /// </summary>
        public Actions<object> AfterHideAction { get; } = new Actions<object>();

        /// <summary>
        /// ������UI֮ǰִ�еĲ������ϡ�
        /// </summary>
        public Actions<object> BeforeDestroyAction { get; } = new Actions<object>();

        /// <summary>
        /// ������UI֮��ִ�еĲ������ϡ�
        /// </summary>
        public Actions<object> AfterDestroyAction { get; } = new Actions<object>();

        /// <summary>
        /// ��UI׼����֮ǰִ�еĲ������ϡ�
        /// </summary>
        public Actions<object> BeforeReadyAction { get; } = new Actions<object>();

        /// <summary>
        /// ��UI׼����֮��ִ�еĲ������ϡ�
        /// </summary>
        public Actions<object> AfterReadyAction { get; } = new Actions<object>();

        #endregion

        #region Overrides

        /// <summary>
        /// ����ʾUI֮ǰִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnBeforeShow(PipelineContext context)
        {
            BeforeShowAction.Invoke(context.Data["params"]);
            return base.OnBeforeShow(context);
        }

        /// <summary>
        /// ����ʾUI֮��ִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnAfterShow(PipelineContext context)
        {
            AfterShowAction.Invoke(context.Data["params"]);
            return base.OnAfterShow(context);
        }

        /// <summary>
        /// �ڴ���UI֮ǰִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnBeforeCreate(PipelineContext context)
        {
            BeforeCreateAction.Invoke(context.Data["params"]);
            return base.OnBeforeCreate(context);
        }

        /// <summary>
        /// �ڴ���UI֮��ִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnAfterCreate(PipelineContext context)
        {
            AfterCreateAction.Invoke(context.Data["params"]);

            return base.OnAfterCreate(context);
        }

        /// <summary>
        /// ������UI֮ǰִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnBeforeHide(PipelineContext context)
        {
            BeforeHideAction.Invoke(context.Data["params"]);

            return base.OnBeforeHide(context);
        }

        /// <summary>
        /// ������UI֮��ִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnAfterHide(PipelineContext context)
        {
            AfterHideAction.Invoke(context.Data["params"]);

            return base.OnAfterHide(context);
        }

        /// <summary>
        /// ������UI֮ǰִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnBeforeDestroy(PipelineContext context)
        {
            BeforeDestroyAction.Invoke(context.Data["params"]);


            return base.OnBeforeDestroy(context);
        }

        /// <summary>
        /// ������UI֮��ִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnAfterDestroy(PipelineContext context)
        {
            AfterDestroyAction.Invoke(context.Data["params"]);


            return base.OnAfterDestroy(context);
        }

        /// <summary>
        /// ��UI׼����֮ǰִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnBeforeReady(PipelineContext context)
        {
            BeforeReadyAction.Invoke(context.Data["params"]);

            return base.OnBeforeReady(context);
        }

        /// <summary>
        /// ��UI׼����֮��ִ�еĲ�����
        /// </summary>
        /// <param name="context">�ܵ������ġ�</param>
        /// <returns>һ��Task��ʾ�첽������</returns>
        protected override Task OnAfterReady(PipelineContext context)
        {
            AfterReadyAction.Invoke(context.Data["params"]);

            return base.OnAfterReady(context);
        }

        #endregion
    }
}