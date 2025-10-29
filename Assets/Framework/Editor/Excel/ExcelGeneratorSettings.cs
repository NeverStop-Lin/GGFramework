#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.Excel
{
    /// <summary>
    /// Excel配置生成器设置
    /// 存储Excel生成工具的配置
    /// </summary>
    [CreateAssetMenu(fileName = "ExcelGeneratorSettings", menuName = "Framework/Excel/Generator Settings")]
    public class ExcelGeneratorSettings : ScriptableObject
    {
        [Header("Excel文件路径")]
        [Tooltip("Excel文件根目录路径（支持项目外路径，如：C:/Excel 或 Assets/../Excel）")]
        [SerializeField] private string _excelRootPath;
        
        [Header("输出路径")]
        [Tooltip("JSON数据输出目录")]
        [SerializeField] private DefaultAsset _jsonOutputFolder;
        
        [Tooltip("C#代码输出目录")]
        [SerializeField] private DefaultAsset _csharpOutputFolder;
        
        [Header("代码生成设置")]
        [Tooltip("生成代码的默认命名空间")]
        [SerializeField] private string _defaultNamespace;
        
        /// <summary>
        /// Excel文件根目录路径
        /// </summary>
        public string ExcelRootPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_excelRootPath))
                {
                    return _excelRootPath;
                }
                // 默认返回项目外的 Excel 目录
                return Core.FrameworkDefaultPaths.ExcelRootFolder;
            }
            set
            {
                _excelRootPath = value;
                Save();
            }
        }
        
        /// <summary>
        /// JSON输出目录路径
        /// </summary>
        public string JsonOutputPath
        {
            get
            {
                if (_jsonOutputFolder != null)
                {
                    return AssetDatabase.GetAssetPath(_jsonOutputFolder);
                }
                return Core.FrameworkDefaultPaths.ExcelJsonOutputFolder;
            }
        }
        
        /// <summary>
        /// C#代码输出目录路径
        /// </summary>
        public string CSharpOutputPath
        {
            get
            {
                if (_csharpOutputFolder != null)
                {
                    return AssetDatabase.GetAssetPath(_csharpOutputFolder);
                }
                return Core.FrameworkDefaultPaths.ExcelCSharpOutputFolder;
            }
        }
        
        /// <summary>
        /// 默认命名空间
        /// </summary>
        public string DefaultNamespace
        {
            get => string.IsNullOrEmpty(_defaultNamespace) ? Core.FrameworkDefaultPaths.ExcelDefaultNamespace : _defaultNamespace;
            set => _defaultNamespace = value;
        }
        
        /// <summary>
        /// 获取当前激活的Excel生成器配置实例
        /// </summary>
        public static ExcelGeneratorSettings Instance
        {
            get
            {
                var index = Core.FrameworkSettingsIndex.Instance;
                return index?.ExcelGeneratorSettings;
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
        /// JSON输出文件夹对象引用
        /// </summary>
        public DefaultAsset JsonOutputFolder
        {
            get => _jsonOutputFolder;
            set
            {
                _jsonOutputFolder = value;
                Save();
            }
        }
        
        /// <summary>
        /// C#输出文件夹对象引用
        /// </summary>
        public DefaultAsset CSharpOutputFolder
        {
            get => _csharpOutputFolder;
            set
            {
                _csharpOutputFolder = value;
                Save();
            }
        }
        
        /// <summary>
        /// 检查配置是否已初始化（所有必需字段都已设置）
        /// 检查最终值（包括默认值）
        /// </summary>
        public bool IsInitialized()
        {
            return !string.IsNullOrEmpty(ExcelRootPath) &&
                   !string.IsNullOrEmpty(JsonOutputPath) &&
                   !string.IsNullOrEmpty(CSharpOutputPath) &&
                   !string.IsNullOrEmpty(DefaultNamespace);
        }
        
        /// <summary>
        /// 验证配置是否完整
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = null;
            
            // Excel根目录可以为空（使用默认值）
            // 但输出目录必须配置
            if (_jsonOutputFolder == null)
            {
                errorMessage = "JSON输出目录未配置";
                return false;
            }
            
            if (_csharpOutputFolder == null)
            {
                errorMessage = "C#代码输出目录未配置";
                return false;
            }
            
            if (string.IsNullOrEmpty(_defaultNamespace))
            {
                errorMessage = "默认命名空间未配置";
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
    }
}
#endif

