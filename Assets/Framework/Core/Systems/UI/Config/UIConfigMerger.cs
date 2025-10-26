namespace Framework.Core
{
    /// <summary>
    /// UI配置合并器
    /// 负责将代码配置和运行时配置进行融合
    /// </summary>
    public static class UIConfigMerger
    {
        /// <summary>
        /// 合并配置（运行时配置优先）
        /// </summary>
        /// <param name="codeConfig">代码中定义的配置</param>
        /// <param name="manifestConfig">UIManifest中的运行时配置</param>
        /// <returns>合并后的最终配置</returns>
        public static UIConfig Merge(UIConfig codeConfig, UIConfig manifestConfig)
        {
            if (codeConfig == null && manifestConfig == null)
            {
                return new UIConfig(); // 返回默认配置
            }
            
            if (manifestConfig == null)
            {
                return codeConfig.Clone();
            }
            
            if (codeConfig == null)
            {
                return manifestConfig.Clone();
            }
            
            // 运行时配置优先，如果运行时配置没有设置则使用代码配置
            var merged = new UIConfig();
            
            // 资源路径：运行时配置优先
            merged.ResourcePath = !string.IsNullOrEmpty(manifestConfig.ResourcePath) 
                ? manifestConfig.ResourcePath 
                : codeConfig.ResourcePath;
            
            // UI类型：运行时配置优先
            merged.UIType = manifestConfig.UIType != UIType.Main || codeConfig.UIType == UIType.Main
                ? manifestConfig.UIType 
                : codeConfig.UIType;
            
            // 缓存策略：如果运行时设置了非默认值则使用运行时的
            merged.CacheStrategy = manifestConfig.CacheStrategy != UICacheStrategy.Default
                ? manifestConfig.CacheStrategy
                : codeConfig.CacheStrategy;
            
            // 其他属性按相同逻辑合并
            merged.Layer = manifestConfig.Layer != 0 ? manifestConfig.Layer : codeConfig.Layer;
            merged.UseMask = manifestConfig.UseMask || codeConfig.UseMask;
            merged.Preload = manifestConfig.Preload || codeConfig.Preload;
            
            return merged;
        }
    }
}
