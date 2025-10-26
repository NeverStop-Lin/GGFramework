namespace Framework.Core
{
    /// <summary>
    /// UI运行时状态
    /// </summary>
    public enum UIRuntimeState
    {
        /// <summary>
        /// 未创建
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 正在创建
        /// </summary>
        Creating = 1,
        
        /// <summary>
        /// 正在显示
        /// </summary>
        Showing = 2,
        
        /// <summary>
        /// 已隐藏
        /// </summary>
        Hidden = 3,
        
        /// <summary>
        /// 正在销毁
        /// </summary>
        Destroying = 4,
        
        /// <summary>
        /// 已销毁
        /// </summary>
        Destroyed = 5
    }
}
