using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// UI配置清单
    /// 用于统一管理所有UI的配置信息
    /// </summary>
    [CreateAssetMenu(fileName = "UIManifest", menuName = "Framework/UI/UI Manifest")]
    public class UIManifest : ScriptableObject
    {
        [SerializeField]
        private List<UIConfigEntry> _entries = new List<UIConfigEntry>();
        
        /// <summary>
        /// 所有UI配置条目
        /// </summary>
        public List<UIConfigEntry> Entries => _entries;
        
        /// <summary>
        /// 获取指定UI的配置
        /// </summary>
        public UIConfig GetConfig(string uiName)
        {
            var entry = _entries.FirstOrDefault(e => e.UIName == uiName);
            return entry?.Config;
        }
        
        /// <summary>
        /// 获取指定UI的配置
        /// </summary>
        public UIConfig GetConfig(Type uiType)
        {
            return GetConfig(uiType.Name);
        }
        
        /// <summary>
        /// 添加或更新UI配置
        /// </summary>
        public void AddOrUpdateConfig(string uiName, UIConfig config)
        {
            var entry = _entries.FirstOrDefault(e => e.UIName == uiName);
            if (entry != null)
            {
                entry.Config = config;
            }
            else
            {
                _entries.Add(new UIConfigEntry
                {
                    UIName = uiName,
                    Config = config
                });
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// 移除UI配置
        /// </summary>
        public void RemoveConfig(string uiName)
        {
            _entries.RemoveAll(e => e.UIName == uiName);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// 获取所有需要预加载的UI
        /// </summary>
        public List<UIConfigEntry> GetPreloadUIs()
        {
            return _entries.Where(e => e.Config != null && e.Config.Preload).ToList();
        }
    }
    
    /// <summary>
    /// UI配置条目
    /// </summary>
    [Serializable]
    public class UIConfigEntry
    {
        /// <summary>
        /// UI名称（类名）
        /// </summary>
        public string UIName;
        
        /// <summary>
        /// UI配置
        /// </summary>
        public UIConfig Config;
    }
}
