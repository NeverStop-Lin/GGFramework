using Framework.Scripts;

namespace Framework.Modules.UI
{
    /// <summary>
    /// 通用UI组件模块
    /// 提供常用的UI组件（Toast、Loading、MessageBox等）
    /// </summary>
    public class CommonUIModule
    {
        /// <summary>
        /// 显示Toast提示
        /// </summary>
        /// <param name="message">提示信息</param>
        public void ShowToast(string message)
        {
            // TODO: 实现 UGUI 版本�?Toast
            // GridFramework.UI.Show<ToastUI>(message);
        }
        
        /// <summary>
        /// 显示加载界面
        /// </summary>
        public void ShowLoading()
        {
            // TODO: 实现 UGUI 版本�?Loading
            // GridFramework.UI.Show<LoadingUI>();
        }
        
        /// <summary>
        /// 隐藏加载界面
        /// </summary>
        public void HideLoading()
        {
            // TODO: 实现 UGUI 版本�?Loading
            // GridFramework.UI.Hide<LoadingUI>();
        }
        
        /// <summary>
        /// 显示消息�?
        /// </summary>
        public void ShowMessageBox(string title, string message)
        {
            // TODO: 实现 UGUI 版本�?MessageBox
            // GridFramework.UI.Show<MessageBoxUI>(title, message);
        }
    }
}