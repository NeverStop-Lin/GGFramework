namespace Framework.Core
{
    /// <summary>
    /// UI缓存策略
    /// </summary>
    public enum UICacheStrategy
    {
        /// <summary>
        /// 总是缓存（Hide后保留实例，再次Show时直接使用）
        /// </summary>
        AlwaysCache = 0,
        
        /// <summary>
        /// 从不缓存（Hide后销毁实例，再次Show时重新创建）
        /// </summary>
        NeverCache = 1
    }
}
