using System.Threading.Tasks;
namespace Framework.Core
{
    public class AlignUIAttachment : UIAttachment
    {

        protected override Task OnBeforeShow(PipelineContext context)
        {
            switch (Target.AlignType)
            {
                case UIAlignType.Center:
                {
                    OnAlignCenter();
                    break;
                }
                case UIAlignType.Top:
                {
                    OnAlignTop();
                    break;
                }
                case UIAlignType.Bottom:
                {
                    OnAlignBottom();
                    break;
                }
            }
            return base.OnBeforeShow(context);
        }

        protected virtual void OnAlignTop()
        {
            
        }
        
        protected virtual void OnAlignBottom()
        {
            
        }
        
        protected virtual void OnAlignCenter()
        {
            
        }
    }
}