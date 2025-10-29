#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
        [Tooltip("UI项目配置代码文件引用")]
        [SerializeField] private MonoScript _configCodeFile;
        
        [Header("UI代码生成")]
        [Tooltip("生成UI代码的默认命名空间")]
        [SerializeField] private string _defaultNamespace = "Game.UI";
        
        [Tooltip("业务逻辑脚本（Logic.cs）的输出目录")]
        [SerializeField] private DefaultAsset _logicScriptOutputFolder;
        
        [Tooltip("自动生成绑定脚本（Binding.cs）的输出目录")]
        [SerializeField] private DefaultAsset _bindingScriptOutputFolder;
        
        [Tooltip("生成代码后是否自动打开文件")]
        [SerializeField] private bool _openAfterGenerate = false;
        
        [Header("UI创建设置")]
        [Tooltip("创建UI预制体的默认目录")]
        [SerializeField] private DefaultAsset _uiPrefabCreationFolder;
        
        [Header("Prefab扫描")]
        [Tooltip("常用的UI Prefab目录列表")]
        [SerializeField] private List<DefaultAsset> _prefabFolders = new List<DefaultAsset>();
        
        /// <summary>
        /// UI项目配置代码文件路径
        /// </summary>
        public string ConfigCodeFilePath
        {
            get => _configCodeFile != null ? AssetDatabase.GetAssetPath(_configCodeFile) : "";
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _configCodeFile = AssetDatabase.LoadAssetAtPath<MonoScript>(value);
                    Save();
                }
            }
        }
        
        /// <summary>
        /// 配置代码文件对象引用
        /// </summary>
        public MonoScript ConfigCodeFile
        {
            get => _configCodeFile;
            set
            {
                _configCodeFile = value;
                Save();
            }
        }
        
        /// <summary>
        /// 默认命名空间
        /// </summary>
        public string DefaultNamespace
        {
            get => string.IsNullOrEmpty(_defaultNamespace) ? "Game.UI" : _defaultNamespace;
            set
            {
                _defaultNamespace = value;
                Save();
            }
        }
        
        /// <summary>
        /// 业务逻辑脚本输出路径
        /// </summary>
        public string LogicScriptOutputPath
        {
            get => _logicScriptOutputFolder != null ? AssetDatabase.GetAssetPath(_logicScriptOutputFolder) : "Assets/Game/Scripts/UI";
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _logicScriptOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(value);
                    Save();
                }
            }
        }
        
        /// <summary>
        /// 逻辑脚本输出文件夹对象引用
        /// </summary>
        public DefaultAsset LogicScriptOutputFolder
        {
            get => _logicScriptOutputFolder;
            set
            {
                _logicScriptOutputFolder = value;
                Save();
            }
        }
        
        /// <summary>
        /// 绑定脚本输出路径
        /// </summary>
        public string BindingScriptOutputPath
        {
            get => _bindingScriptOutputFolder != null ? AssetDatabase.GetAssetPath(_bindingScriptOutputFolder) : "Assets/Game/Scripts/UI/Generated";
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _bindingScriptOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(value);
                    Save();
                }
            }
        }
        
        /// <summary>
        /// 绑定脚本输出文件夹对象引用
        /// </summary>
        public DefaultAsset BindingScriptOutputFolder
        {
            get => _bindingScriptOutputFolder;
            set
            {
                _bindingScriptOutputFolder = value;
                Save();
            }
        }
        
        /// <summary>
        /// 生成代码后是否自动打开文件
        /// </summary>
        public bool OpenAfterGenerate
        {
            get => _openAfterGenerate;
            set
            {
                _openAfterGenerate = value;
                Save();
            }
        }
        
        /// <summary>
        /// UI预制体创建默认路径
        /// </summary>
        public string UIPrefabCreationDefaultPath
        {
            get => _uiPrefabCreationFolder != null ? AssetDatabase.GetAssetPath(_uiPrefabCreationFolder) : "Assets/Game/Resources/UI";
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _uiPrefabCreationFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(value);
                    Save();
                }
            }
        }
        
        /// <summary>
        /// UI预制体创建文件夹对象引用
        /// </summary>
        public DefaultAsset UIPrefabCreationFolder
        {
            get => _uiPrefabCreationFolder;
            set
            {
                _uiPrefabCreationFolder = value;
                Save();
            }
        }
        
        /// <summary>
        /// Prefab目录列表（字符串路径）
        /// </summary>
        public List<string> PrefabDirectories
        {
            get => _prefabFolders.Where(f => f != null).Select(f => AssetDatabase.GetAssetPath(f)).ToList();
            set
            {
                _prefabFolders = value.Where(p => !string.IsNullOrEmpty(p))
                    .Select(p => AssetDatabase.LoadAssetAtPath<DefaultAsset>(p))
                    .Where(f => f != null)
                    .ToList();
                Save();
            }
        }
        
        /// <summary>
        /// Prefab文件夹对象列表
        /// </summary>
        public List<DefaultAsset> PrefabFolders
        {
            get => _prefabFolders;
            set
            {
                _prefabFolders = value;
                Save();
            }
        }
        
        /// <summary>
        /// 获取单例实例（通过配置索引）
        /// </summary>
        public static UIManagerSettings Instance
        {
            get
            {
                var index = Core.FrameworkSettingsIndex.Instance;
                if (index == null)
                    return null;
                    
                return index.UIManagerSettings;
            }
        }
        
        /// <summary>
        /// 重新加载配置
        /// </summary>
        public static void Reload()
        {
            Core.FrameworkSettingsIndex.Reload();
        }
        
        /// <summary>
        /// 保存配置
        /// </summary>
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// 验证配置是否完整
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = null;
            
            if (_configCodeFile == null)
            {
                errorMessage = "UI项目配置代码文件未设置";
                return false;
            }
            
            if (_logicScriptOutputFolder == null)
            {
                errorMessage = "业务逻辑脚本输出目录未设置";
                return false;
            }
            
            if (_bindingScriptOutputFolder == null)
            {
                errorMessage = "绑定脚本输出目录未设置";
                return false;
            }
            
            if (_uiPrefabCreationFolder == null)
            {
                errorMessage = "UI预制体创建目录未设置";
                return false;
            }
            
            if (string.IsNullOrEmpty(_defaultNamespace))
            {
                errorMessage = "默认命名空间未设置";
                return false;
            }
            
            return true;
        }
    }
}
#endif

