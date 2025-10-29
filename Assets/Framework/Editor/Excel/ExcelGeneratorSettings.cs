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
        [Tooltip("Excel文件根目录（拖拽文件夹到此处）")]
        [SerializeField] private DefaultAsset _excelRootFolder;
        
        [Header("输出路径")]
        [Tooltip("JSON数据输出目录")]
        [SerializeField] private DefaultAsset _jsonOutputFolder;
        
        [Tooltip("C#代码输出目录")]
        [SerializeField] private DefaultAsset _csharpOutputFolder;
        
        [Header("代码生成设置")]
        [Tooltip("生成代码的默认命名空间")]
        [SerializeField] private string _defaultNamespace = "Generate.Scripts.Configs";
        
        /// <summary>
        /// Excel文件根目录路径
        /// </summary>
        public string ExcelRootPath
        {
            get
            {
                if (_excelRootFolder != null)
                {
                    var path = AssetDatabase.GetAssetPath(_excelRootFolder);
                    // 如果是 Assets/../Excel 这种相对路径，转换为绝对路径
                    if (path.StartsWith("Assets/"))
                    {
                        return path;
                    }
                }
                // 默认返回项目外的 Excel 目录
                return "Assets/../Excel/";
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
                return "Assets/Resources/Configs/";
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
                return "Assets/Generate/Scripts/Configs/";
            }
        }
        
        /// <summary>
        /// 默认命名空间
        /// </summary>
        public string DefaultNamespace
        {
            get => string.IsNullOrEmpty(_defaultNamespace) ? "Generate.Scripts.Configs" : _defaultNamespace;
            set => _defaultNamespace = value;
        }
        
        /// <summary>
        /// Excel根文件夹对象引用
        /// </summary>
        public DefaultAsset ExcelRootFolder
        {
            get => _excelRootFolder;
            set
            {
                _excelRootFolder = value;
                Save();
            }
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

