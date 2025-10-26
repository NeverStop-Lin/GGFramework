using System;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// UIManifest管理器
    /// 负责加载和访问UIManifest配置
    /// </summary>
    public static class UIManifestManager
    {
        private static UIManifest _manifest;
        private const string MANIFEST_PATH = "Config/UIManifest";
        
        /// <summary>
        /// 获取UIManifest
        /// </summary>
        public static UIManifest GetManifest()
        {
            if (_manifest == null)
            {
                _manifest = Resources.Load<UIManifest>(MANIFEST_PATH);
                
                if (_manifest == null)
                {
                    FrameworkLogger.Warn($"[UIManifest] 未找到UIManifest配置文件，路径: Resources/{MANIFEST_PATH}");
                }
                else
                {
                    FrameworkLogger.Info($"[UIManifest] 加载UIManifest配置，共 {_manifest.Entries.Count} 个UI");
                }
            }
            
            return _manifest;
        }
        
        /// <summary>
        /// 获取指定UI的配置
        /// </summary>
        public static UIConfig GetConfig(Type uiType)
        {
            var manifest = GetManifest();
            return manifest?.GetConfig(uiType);
        }
        
        /// <summary>
        /// 获取指定UI的配置
        /// </summary>
        public static UIConfig GetConfig(string uiName)
        {
            var manifest = GetManifest();
            return manifest?.GetConfig(uiName);
        }
        
        /// <summary>
        /// 重新加载Manifest
        /// </summary>
        public static void Reload()
        {
            _manifest = null;
            GetManifest();
        }
    }
}
