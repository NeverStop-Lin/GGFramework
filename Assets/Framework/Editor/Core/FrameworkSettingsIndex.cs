#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.Core
{
    /// <summary>
    /// 框架配置索引文件
    /// 固定位置存储，记录所有编辑器工具的配置文件引用
    /// </summary>
    [CreateAssetMenu(fileName = "FrameworkSettingsIndex", menuName = "Framework/Core/Settings Index")]
    public class FrameworkSettingsIndex : ScriptableObject
    {
        private const string INDEX_PATH = "Assets/Editor/FrameworkSettingsIndex.asset";
        
        [Header("编辑器工具配置")]
        [Tooltip("UI管理器配置文件")]
        [SerializeField] private UI.UIManagerSettings _uiManagerSettings;
        
        [Tooltip("Excel生成器配置文件")]
        [SerializeField] private Excel.ExcelGeneratorSettings _excelGeneratorSettings;
        
        /// <summary>
        /// UI管理器配置
        /// </summary>
        public UI.UIManagerSettings UIManagerSettings
        {
            get => _uiManagerSettings;
            set
            {
                _uiManagerSettings = value;
                Save();
            }
        }
        
        /// <summary>
        /// Excel生成器配置
        /// </summary>
        public Excel.ExcelGeneratorSettings ExcelGeneratorSettings
        {
            get => _excelGeneratorSettings;
            set
            {
                _excelGeneratorSettings = value;
                Save();
            }
        }
        
        private static FrameworkSettingsIndex _instance;
        
        /// <summary>
        /// 获取索引实例（懒加载）
        /// </summary>
        public static FrameworkSettingsIndex Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadOrNull();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 从固定路径加载索引文件
        /// </summary>
        private static FrameworkSettingsIndex LoadOrNull()
        {
            return AssetDatabase.LoadAssetAtPath<FrameworkSettingsIndex>(INDEX_PATH);
        }
        
        /// <summary>
        /// 检查索引文件是否存在
        /// </summary>
        public static bool Exists()
        {
            return File.Exists(INDEX_PATH);
        }
        
        /// <summary>
        /// 获取或创建索引文件
        /// </summary>
        public static FrameworkSettingsIndex GetOrCreate()
        {
            var index = LoadOrNull();
            if (index == null)
            {
                index = CreateIndexFile();
            }
            return index;
        }
        
        /// <summary>
        /// 创建索引文件（固定位置）
        /// </summary>
        private static FrameworkSettingsIndex CreateIndexFile()
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(INDEX_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 创建索引文件
            var index = CreateInstance<FrameworkSettingsIndex>();
            AssetDatabase.CreateAsset(index, INDEX_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[FrameworkSettings] 配置索引文件已创建: {INDEX_PATH}");
            
            _instance = index;
            return index;
        }
        
        /// <summary>
        /// 验证配置完整性
        /// </summary>
        public static bool ValidateSettings(out string errorMessage)
        {
            errorMessage = null;
            
            if (!Exists())
            {
                errorMessage = "配置索引文件不存在";
                return false;
            }
            
            var index = Instance;
            if (index == null)
            {
                errorMessage = "无法加载配置索引文件";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 验证UI管理器配置
        /// </summary>
        public bool ValidateUIManagerSettings(out string errorMessage)
        {
            errorMessage = null;
            
            if (_uiManagerSettings == null)
            {
                errorMessage = "UI管理器配置缺失";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 验证Excel生成器配置
        /// </summary>
        public bool ValidateExcelGeneratorSettings(out string errorMessage)
        {
            errorMessage = null;
            
            if (_excelGeneratorSettings == null)
            {
                errorMessage = "Excel生成器配置缺失";
                return false;
            }
            
            return true;
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
        /// 重新加载索引
        /// </summary>
        public static void Reload()
        {
            _instance = null;
        }
        
        /// <summary>
        /// 删除索引文件（用于重置）
        /// </summary>
        public static bool DeleteIndex()
        {
            if (Exists())
            {
                AssetDatabase.DeleteAsset(INDEX_PATH);
                AssetDatabase.Refresh();
                _instance = null;
                return true;
            }
            return false;
        }
    }
}
#endif

