namespace Framework.Core
{
    /// <summary>
    /// UI缓存策略
    /// </summary>
    public enum UICacheStrategy
    {
        /// <summary>
        /// 默认策略（使用框架默认配置）
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// 总是缓存（Hide后保留实例，再次Show时直接使用）
        /// </summary>
        AlwaysCache = 1,
        
        /// <summary>
        /// 从不缓存（Hide后销毁实例，再次Show时重新创建）
        /// </summary>
        NeverCache = 2,
        
        /// <summary>
        /// LRU策略（最近最少使用，缓存数量有限时淘汰）
        /// </summary>
        LRU = 3
    }
}
