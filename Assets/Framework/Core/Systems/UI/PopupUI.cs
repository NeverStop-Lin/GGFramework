using System.Collections.Generic;

namespace Framework.Core
{
    public class PopupUI : BaseUI
    {

        protected override void OnAttachmentInitialize(List<UIAttachment> attachments)
        {
            base.OnAttachmentInitialize(attachments);
            attachments.Add(OnCreateMaskUIAttachment());
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


        #region 可重写的方法

        protected virtual MaskUIAttachment OnCreateMaskUIAttachment()
        {
            return new MaskUIAttachment();
        }

        protected virtual ShowHideUiAttachment OnCreateHideUiAttachment()
        {
            return new ShowHideUiAttachment();
        }

        #endregion


    }
}