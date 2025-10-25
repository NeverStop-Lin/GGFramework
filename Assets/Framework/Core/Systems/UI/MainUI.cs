using System.Collections.Generic;

namespace Framework.Core
{
    public class MainUI : BaseUI
    {
        protected override void OnAttachmentInitialize(List<UIAttachment> attachments)
        {
            base.OnAttachmentInitialize(attachments);
            attachments.Add(OnCreateHideUiAttachment());
        }
        public override int GetIndex()
        {
            throw new System.NotImplementedException();
        }
        public override void SetIndex(int i)
        {
            throw new System.NotImplementedException();
        }

        protected virtual ShowHideUiAttachment OnCreateHideUiAttachment()
        {
            return new ShowHideUiAttachment();
        }
    }
}