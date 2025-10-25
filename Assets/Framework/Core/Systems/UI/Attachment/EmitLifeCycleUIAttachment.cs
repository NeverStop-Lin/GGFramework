using System.Threading.Tasks;

namespace Framework.Core
{
    public class EmitLifeCycleUIAttachment : UIAttachment
    {

        protected override Task OnAfterCreate(PipelineContext context)
        {

            EventBus.Emit(GlobalEventType.UI, GlobalEventType.UIEvent.Create,
                Target.GetType().Name);
            return base.OnAfterCreate(context);
        }

        protected override Task OnAfterShow(PipelineContext context)
        {
            EventBus.Emit(GlobalEventType.UI, GlobalEventType.UIEvent.Show, Target.GetType().Name);
            return base.OnAfterShow(context);
        }

        protected override Task OnAfterReady(PipelineContext context)
        {
            EventBus.Emit(GlobalEventType.UI, GlobalEventType.UIEvent.Ready, Target.GetType().Name);
            return base.OnAfterReady(context);
        }

        protected override Task OnAfterHide(PipelineContext context)
        {
            EventBus.Emit(GlobalEventType.UI, GlobalEventType.UIEvent.Hide, Target.GetType().Name);
            return base.OnAfterHide(context);
        }

        protected override Task OnAfterDestroy(PipelineContext context)
        {
            EventBus.Emit(GlobalEventType.UI, GlobalEventType.UIEvent.Destroy,
                Target.GetType().Name);
            return base.OnAfterDestroy(context);
        }
    }
}