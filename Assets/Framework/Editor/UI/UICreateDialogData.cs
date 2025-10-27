#if UNITY_EDITOR
using Framework.Core;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI创建对话框数据
    /// </summary>
    public class UICreateDialogData
    {
        /// <summary>
        /// UI名称
        /// </summary>
        public string UIName;
        
        /// <summary>
        /// 保存目录路径（不包含文件名）
        /// </summary>
        public string SaveDirectory;
        
        /// <summary>
        /// UI层级名称
        /// </summary>
        public string LayerName;
        
        /// <summary>
        /// 选择的模板路径
        /// </summary>
        public string TemplatePath;
        
        /// <summary>
        /// 是否确认
        /// </summary>
        public bool Confirmed;
    }
}
#endif

