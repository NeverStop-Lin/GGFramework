#if UNITY_EDITOR
using UnityEngine;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI管理器设置
    /// 存储UI生成工具的默认配置
    /// </summary>
    [CreateAssetMenu(fileName = "UIManagerSettings", menuName = "Framework/UI/UI Manager Settings")]
    public class UIManagerSettings : ScriptableObject
    {
        [Header("UI配置代码")]
        [Tooltip("UI项目配置代码文件路径")]
        public string ConfigCodeFilePath = "Assets/Game/Scripts/Generated/UIProjectConfigData.cs";
        
        [Header("UI代码生成")]
        [Tooltip("生成UI代码的默认命名空间")]
        public string DefaultNamespace = "Game.UI";
        
        [Tooltip("业务逻辑脚本（Logic.cs）的输出路径")]
        public string LogicScriptOutputPath = "Assets/Game/Scripts/UI";
        
        [Tooltip("自动生成绑定脚本（Binding.cs）的输出路径")]
        public string BindingScriptOutputPath = "Assets/Game/Scripts/UI/Generated";
        
        [Tooltip("生成代码后是否自动打开文件")]
        public bool OpenAfterGenerate = false;
        
        [Header("Prefab扫描")]
        [Tooltip("常用的UI Prefab目录列表")]
        public System.Collections.Generic.List<string> PrefabDirectories = new System.Collections.Generic.List<string>
        {
            "Assets/Game/Resources/UI"
        };
        
        /// <summary>
        /// 获取单例实例
        /// </summary>
        private static UIManagerSettings _instance;
        
        public static UIManagerSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 尝试从固定位置加载（编辑器配置固定位置）
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<UIManagerSettings>(
                        "Assets/Editor/Framework/Configs/UIManagerSettings.asset"
                    );
                    
                    // 如果不存在，查找项目中的任意一个
                    if (_instance == null)
                    {
                        var guids = UnityEditor.AssetDatabase.FindAssets("t:UIManagerSettings");
                        if (guids.Length > 0)
                        {
                            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<UIManagerSettings>(path);
                        }
                    }
                }
                
                return _instance;
            }
        }
        
        /// <summary>
        /// 重新加载配置
        /// </summary>
        public static void Reload()
        {
            _instance = null;
        }
        
        /// <summary>
        /// 保存配置
        /// </summary>
        public void Save()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
    }
}
#endif

