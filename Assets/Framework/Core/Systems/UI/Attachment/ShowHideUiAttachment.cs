using System.Threading.Tasks;

namespace Framework.Core
{
    public class ShowHideUiAttachment : UIAttachment
    {
        protected override async Task OnAfterShow(PipelineContext context)
        {
            await OnShowAnimation();
            await base.OnAfterShow(context);
        }

        protected override async Task OnBeforeHide(PipelineContext context)
        {
            await OnHideAnimation();
            await base.OnBeforeHide(context);
        }

        protected virtual Task OnShowAnimation() { return Task.CompletedTask; }
        protected virtual Task OnHideAnimation() { return Task.CompletedTask; }
    }
}