using System;
using System.Collections.Generic;

namespace Framework.Core
{
    /// <summary>
    /// MainUI基类（已废弃）
    /// 
    /// 请直接继承UGUIBaseUI，使用UIConfig配置UI类型
    /// 
    /// 迁移示例：
    /// <code>
    /// // 旧代码
    /// public class MyUI : MainUI { }
    /// 
    /// // 新代码
    /// public class MyUI : UGUIBaseUI
    /// {
    ///     protected override UIConfig CreateUIConfig()
    ///     {
    ///         return new UIConfig
    ///         {
    ///             UIType = UIType.Main,
    ///             ResourcePath = "UI/MyUI"
    ///         };
    ///     }
    /// }
    /// </code>
    /// </summary>
    [Obsolete("请直接继承UGUIBaseUI，使用UIConfig配置UI类型。详见类注释中的迁移示例。", false)]
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