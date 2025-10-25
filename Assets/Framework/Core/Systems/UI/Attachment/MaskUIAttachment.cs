using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.Core
{
    public class MaskUIAttachment : UIAttachment
    {

        static readonly List<object> MaskIds = new List<object>();

        protected override Task OnAfterShow(PipelineContext context)
        {
            MaskIds.Add(context.Data["target"]);
            ShowMask(MaskIds.Last());
            return base.OnAfterShow(context);
        }

        protected override Task OnBeforeHide(PipelineContext context)
        {
            MaskIds.Remove(context.Data["target"]);
            if (MaskIds.Count > 0)
            {
                ShowMask(MaskIds.Last());
            }
            else { HideMask(); }
            return base.OnBeforeHide(context);
        }

        protected virtual void ShowMask(object target) { }

        protected virtual void HideMask() { }


    }
}