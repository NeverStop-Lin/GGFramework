namespace Framework.Core
{
    /// <summary>
    /// UI动画类型
    /// </summary>
    public enum UIAnimationType
    {
        /// <summary>
        /// 无动画
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 淡入淡出
        /// </summary>
        Fade = 1,
        
        /// <summary>
        /// 缩放
        /// </summary>
        Scale = 2,
        
        /// <summary>
        /// 滑动
        /// </summary>
        Slide = 3,
        
        /// <summary>
        /// 自定义（使用Prefab自带的Animator）
        /// </summary>
        Custom = 99
    }
}
