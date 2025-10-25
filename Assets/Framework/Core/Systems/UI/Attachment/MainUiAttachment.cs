using System.Threading.Tasks;

namespace Framework.Core
{
    public class MainUiAttachment : UIAttachment
    {

        public static BaseUI MainTarget;

        protected override Task OnBeforeShow(PipelineContext context)
        {
            if (MainTarget == null) return base.OnBeforeShow(context);
            if (MainTarget != context.Data["target"])
            {
                MainTarget.Hide();
            }

            MainTarget = (BaseUI)context.Data["target"];

            return base.OnBeforeShow(context);
        }


    }
}