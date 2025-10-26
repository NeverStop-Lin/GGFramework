using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// UIRoot管理器
    /// 负责创建和管理UI的根节点
    /// </summary>
    public static class UIRootManager
    {
        private static GameObject _uiRoot;
        private static Transform _uiRootTransform;
        
        /// <summary>
        /// 获取或创建UIRoot
        /// </summary>
        public static Transform GetOrCreateUIRoot()
        {
            if (_uiRoot == null)
            {
                _uiRoot = new GameObject("UIRoot");
                Object.DontDestroyOnLoad(_uiRoot);
                _uiRootTransform = _uiRoot.transform;
                
                FrameworkLogger.Info("[UIRoot] 创建UIRoot节点");
            }
            
            return _uiRootTransform;
        }
        
        /// <summary>
        /// 获取UIRoot（如果不存在返回null）
        /// </summary>
        public static Transform GetUIRoot()
        {
            return _uiRootTransform;
        }
        
        /// <summary>
        /// 销毁UIRoot
        /// </summary>
        public static void DestroyUIRoot()
        {
            if (_uiRoot != null)
            {
                Object.Destroy(_uiRoot);
                _uiRoot = null;
                _uiRootTransform = null;
                
                FrameworkLogger.Info("[UIRoot] 销毁UIRoot节点");
            }
        }
    }
}
