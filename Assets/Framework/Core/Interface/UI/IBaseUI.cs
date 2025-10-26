using System.Threading.Tasks;

namespace Framework.Core
{
    public interface IBaseUI
    {
        /// <summary>
        /// UI层级名称
        /// </summary>
        string LayerName { get; set; }
        
        /// <summary>
        /// 初始化UI
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 执行Create
        /// </summary>
        Task<object> DoCreate(params object[] args);
        
        /// <summary>
        /// 执行Show
        /// </summary>
        Task<object> DoShow(params object[] args);
        
        /// <summary>
        /// 执行Show动画
        /// </summary>
        Task<object> DoShowAnim(params object[] args);
        
        /// <summary>
        /// 执行Hide
        /// </summary>
        Task<object> DoHide(params object[] args);
        
        /// <summary>
        /// 执行Hide动画
        /// </summary>
        Task<object> DoHideAnim(params object[] args);
        
        /// <summary>
        /// 执行Destroy
        /// </summary>
        Task<object> DoDestroy(params object[] args);
        
        /// <summary>
        /// 获取UI层级索引
        /// </summary>
        int GetIndex();
        
        /// <summary>
        /// 设置UI层级索引
        /// </summary>
        void SetIndex(int i);
    }
}