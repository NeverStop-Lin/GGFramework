namespace Framework.Core
{
    /// <summary>
    /// UI缓存策略
    /// </summary>
    public enum UICacheStrategy
    {
        /// <summary>
        /// 永久缓存（Hide后保留实例，不参与LRU淘汰）
        /// </summary>
        AlwaysCache = 0,
        
        /// <summary>
        /// 智能缓存（Hide后保留实例，超出上限时按显示次数和时间智能淘汰）
        /// </summary>
        SmartCache = 1,
        
        /// <summary>
        /// 不缓存（Hide后立即销毁实例，再次Show时重新创建）
        /// </summary>
        NeverCache = 2
    }
}
