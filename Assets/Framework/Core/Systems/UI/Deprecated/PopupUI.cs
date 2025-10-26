using System;
using System.Collections.Generic;

namespace Framework.Core
{
    /// <summary>
    /// PopupUI基类（已废弃）
    /// 
    /// 请直接继承UGUIBaseUI，使用UIConfig配置UI类型
    /// 
    /// 迁移示例：
    /// <code>
    /// // 旧代码
    /// public class MyPopup : PopupUI { }
    /// 
    /// // 新代码
    /// public class MyPopup : UGUIBaseUI
    /// {
    ///     protected override UIConfig CreateUIConfig()
    ///     {
    ///         return new UIConfig
    ///         {
    ///             UIType = UIType.Popup,
    ///             UseMask = true,
    ///             ResourcePath = "UI/MyPopup"
    ///         };
    ///     }
    /// }
    /// </code>
    /// </summary>
    [Obsolete("请直接继承UGUIBaseUI，使用UIConfig配置UI类型。详见类注释中的迁移示例。", false)]
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