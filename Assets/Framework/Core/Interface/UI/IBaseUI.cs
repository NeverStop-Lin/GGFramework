using System.Threading.Tasks;

namespace Framework.Core
{
    public interface IBaseUI
    {
        /// <summary>
        /// UI类型
        /// </summary>
        UIType UIType { get; set; }
        
        /// <summary>
        /// 初始化UI
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 执行Create Pipeline
        /// </summary>
        Task<object> DoCreate(params object[] args);
        
        /// <summary>
        /// 执行Show Pipeline
        /// </summary>
        Task<object> DoShow(params object[] args);
        
        /// <summary>
        /// 执行Ready Pipeline
        /// </summary>
        Task<object> DoReady(params object[] args);
        
        /// <summary>
        /// 执行Hide Pipeline
        /// </summary>
        Task<object> DoHide(params object[] args);
        
        /// <summary>
        /// 执行Destroy Pipeline
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