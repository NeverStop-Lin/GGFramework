#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.Core
{
    /// <summary>
    /// 配置引导欢迎窗口
    /// 用于首次使用时引导用户创建或关联配置文件
    /// </summary>
    public class SettingsWelcomeWindow : EditorWindow
    {
        private enum ConfigType
        {
            UIManager,
            ExcelGenerator
        }
        
        private ConfigType _configType;
        private string _title;
        private string _message;
        private Vector2 _scrollPosition;
        private ScriptableObject _selectedConfig;
        private DefaultAsset _saveFolder;
        private string _configValidationMessage;
        private bool _configValidationSuccess;
        
        /// <summary>
        /// 显示UI管理器配置引导窗口
        /// </summary>
        public static void ShowUIManagerWelcome()
        {
            var window = GetWindow<SettingsWelcomeWindow>(true, "UI管理器配置引导", true);
            window._configType = ConfigType.UIManager;
            window._title = "UI管理器配置";
            window._message = "UI管理器配置文件尚未设置";
            window.minSize = new Vector2(520, 340);
            window.maxSize = new Vector2(520, 340);
            window.InitializeRecommendedFolder();
            window.Show();
        }
        
        /// <summary>
        /// 显示Excel生成器配置引导窗口
        /// </summary>
        public static void ShowExcelGeneratorWelcome()
        {
            var window = GetWindow<SettingsWelcomeWindow>(true, "Excel生成器配置引导", true);
            window._configType = ConfigType.ExcelGenerator;
            window._title = "Excel生成器配置";
            window._message = "Excel生成器配置文件尚未设置";
            window.minSize = new Vector2(520, 340);
            window.maxSize = new Vector2(520, 340);
            window.InitializeRecommendedFolder();
            window.Show();
        }
        
        /// <summary>
        /// 初始化推荐文件夹
        /// </summary>
        private void InitializeRecommendedFolder()
        {
            string recommendPath = _configType == ConfigType.UIManager
                ? Core.FrameworkDefaultPaths.UIManagerSettingsFolder
                : Core.FrameworkDefaultPaths.ExcelGeneratorSettingsFolder;
            
            // 尝试加载推荐文件夹（如果存在）
            if (Directory.Exists(recommendPath))
            {
                _saveFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(recommendPath);
            }
            else
            {
                // 不存在时保持为 null，会显示默认路径提示
                _saveFolder = null;
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // 标题
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(_title, titleStyle);
            
            EditorGUILayout.Space(5);
            
            // 说明信息
            var messageStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                wordWrap = true
            };
            EditorGUILayout.LabelField(_message, messageStyle);
            
            EditorGUILayout.Space(10);
            
            // 绘制分隔线
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            
            EditorGUILayout.Space(15);
            
            DrawConfigSelection();
        }
        
        /// <summary>
        /// 绘制配置选择界面
        /// </summary>
        private void DrawConfigSelection()
        {
            // 获取配置类型
            System.Type configType = _configType == ConfigType.UIManager 
                ? typeof(UI.UIManagerSettings) 
                : typeof(Excel.ExcelGeneratorSettings);
            
            // 内容区域（居中）
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.Space(5);
            
            // 第一行：配置文件关联
            // 标签（居中）
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("关联现有配置:", EditorStyles.boldLabel, GUILayout.Width(420));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // 控件行（居中）
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            // ObjectField（固定宽度）
            EditorGUI.BeginChangeCheck();
            var newConfig = EditorGUILayout.ObjectField(
                _selectedConfig,
                configType,
                false,
                GUILayout.Width(280),
                GUILayout.Height(30)
            ) as ScriptableObject;
            
            if (EditorGUI.EndChangeCheck())
            {
                _selectedConfig = newConfig;
                ValidateConfig();
            }
            
            // 浏览按钮
            if (GUILayout.Button("浏览...", GUILayout.Width(60), GUILayout.Height(30)))
            {
                BrowseConfigFile();
            }
            
            // 使用按钮
            GUI.enabled = _selectedConfig != null && _configValidationSuccess;
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("使用", GUILayout.Width(80), GUILayout.Height(30)))
            {
                // 关联配置
                LinkConfigToIndex(_selectedConfig);
                
                // 选中配置文件
                Selection.activeObject = _selectedConfig;
                EditorGUIUtility.PingObject(_selectedConfig);
                
                // 检查是否需要继续引导
                CheckAndShowNextStep();
                
                // 成功，直接关闭
                Close();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // 验证消息（居中）
            if (!string.IsNullOrEmpty(_configValidationMessage))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                var messageStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    normal = { textColor = _configValidationSuccess ? new Color(0.2f, 0.8f, 0.2f) : Color.red }
                };
                EditorGUILayout.LabelField(_configValidationMessage, messageStyle, GUILayout.Width(420), GUILayout.Height(14));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(10);
            
            // 第二行：创建新配置
            // 标签（居中）
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("创建保存路径:", EditorStyles.boldLabel, GUILayout.Width(420));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // 控件行（居中）
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            // ObjectField（固定宽度，与第一行相同）
            _saveFolder = EditorGUILayout.ObjectField(
                _saveFolder,
                typeof(DefaultAsset),
                false,
                GUILayout.Width(280),
                GUILayout.Height(30)
            ) as DefaultAsset;
            
            // 浏览按钮（与第一行相同）
            if (GUILayout.Button("浏览...", GUILayout.Width(60), GUILayout.Height(30)))
            {
                BrowseSaveFolder();
            }
            
            // 创建按钮（与第一行相同）
            GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
            if (GUILayout.Button("创建", GUILayout.Width(80), GUILayout.Height(30)))
            {
                if (CreateNewConfigInFolder())
                {
                    // 成功，直接关闭
                    Close();
                }
                // 失败会在方法内部提示，保持窗口打开
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(20);
            
            // 快速创建按钮（居中）
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("快速创建配置", GUILayout.Width(200), GUILayout.Height(40)))
            {
                // 使用默认路径快速创建
                _saveFolder = null; // 确保使用默认路径
                if (CreateNewConfigInFolder())
                {
                    // 成功，直接关闭
                    Close();
                }
                // 失败会在方法内部提示，保持窗口打开
            }
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 路径提示（居中，简洁）
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            // 获取文件名
            string fileName = _configType == ConfigType.UIManager 
                ? Core.FrameworkDefaultPaths.UIManagerSettingsFileName
                : Core.FrameworkDefaultPaths.ExcelGeneratorSettingsFileName;
            
            // 获取完整路径
            string folderPath = _saveFolder != null 
                ? AssetDatabase.GetAssetPath(_saveFolder) 
                : (_configType == ConfigType.UIManager 
                    ? Core.FrameworkDefaultPaths.UIManagerSettingsFolder 
                    : Core.FrameworkDefaultPaths.ExcelGeneratorSettingsFolder);
            
            string fullPath = $"{folderPath}/{fileName}";
            
            var pathStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.3f, 0.8f, 0.3f) },
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            EditorGUILayout.LabelField(fullPath, pathStyle, GUILayout.Width(500), GUILayout.Height(14));
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // 底部留白
            EditorGUILayout.Space(20);
        }
        
        /// <summary>
        /// 浏览配置文件
        /// </summary>
        private void BrowseConfigFile()
        {
            string recommendPath = _configType == ConfigType.UIManager
                ? Core.FrameworkDefaultPaths.UIManagerSettingsFolder
                : Core.FrameworkDefaultPaths.ExcelGeneratorSettingsFolder;
            
            // 如果推荐目录不存在，从Assets开始
            if (!Directory.Exists(recommendPath))
            {
                recommendPath = "Assets";
            }
            
            string title = "";
            switch (_configType)
            {
                case ConfigType.UIManager:
                    title = "选择UI管理器配置文件";
                    break;
                case ConfigType.ExcelGenerator:
                    title = "选择Excel生成器配置文件";
                    break;
            }
            
            // 使用系统文件浏览器选择文件（树状结构）
            var selectedPath = EditorUtility.OpenFilePanel(
                title,
                recommendPath,
                "asset"
            );
            
            if (string.IsNullOrEmpty(selectedPath))
                return;
            
            // 转换为相对路径
            if (!selectedPath.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("选择失败", "配置文件必须在Assets目录下", "确定");
                return;
            }
            
            var relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            
            // 加载配置文件
            ScriptableObject config = null;
            switch (_configType)
            {
                case ConfigType.UIManager:
                    config = AssetDatabase.LoadAssetAtPath<UI.UIManagerSettings>(relativePath);
                    break;
                case ConfigType.ExcelGenerator:
                    config = AssetDatabase.LoadAssetAtPath<Excel.ExcelGeneratorSettings>(relativePath);
                    break;
            }
            
            if (config != null)
            {
                _selectedConfig = config;
                ValidateConfig();
            }
            else
            {
                EditorUtility.DisplayDialog("选择失败", "选择的文件不是有效的配置文件", "确定");
            }
        }
        
        /// <summary>
        /// 浏览保存文件夹
        /// </summary>
        private void BrowseSaveFolder()
        {
            string currentPath = _saveFolder != null 
                ? AssetDatabase.GetAssetPath(_saveFolder) 
                : (_configType == ConfigType.UIManager
                    ? Core.FrameworkDefaultPaths.UIManagerSettingsFolder
                    : Core.FrameworkDefaultPaths.ExcelGeneratorSettingsFolder);
            
            // 如果默认路径不存在，从Assets开始
            if (!Directory.Exists(currentPath))
            {
                currentPath = "Assets";
            }
            
            // 使用系统文件浏览器选择文件夹
            var selectedPath = EditorUtility.OpenFolderPanel(
                "选择配置保存文件夹",
                currentPath,
                ""
            );
            
            if (string.IsNullOrEmpty(selectedPath))
                return;
            
            // 转换为相对路径
            if (!selectedPath.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("选择失败", "配置文件夹必须在Assets目录下", "确定");
                return;
            }
            
            var relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            
            // 加载文件夹
            _saveFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(relativePath);
            
            if (_saveFolder == null)
            {
                EditorUtility.DisplayDialog("选择失败", "选择的不是有效的文件夹", "确定");
            }
        }
        
        /// <summary>
        /// 验证配置文件
        /// </summary>
        private void ValidateConfig()
        {
            _configValidationMessage = "";
            _configValidationSuccess = false;
            
            if (_selectedConfig == null)
            {
                return;
            }
            
            // 根据类型验证
            bool isValid = false;
            switch (_configType)
            {
                case ConfigType.UIManager:
                    isValid = _selectedConfig is UI.UIManagerSettings;
                    if (isValid)
                    {
                        _configValidationMessage = "✓ 配置文件有效";
                        _configValidationSuccess = true;
                    }
                    else
                    {
                        _configValidationMessage = "✗ 错误：这不是有效的UI管理器配置文件";
                    }
                    break;
                    
                case ConfigType.ExcelGenerator:
                    isValid = _selectedConfig is Excel.ExcelGeneratorSettings;
                    if (isValid)
                    {
                        _configValidationMessage = "✓ 配置文件有效";
                        _configValidationSuccess = true;
                    }
                    else
                    {
                        _configValidationMessage = "✗ 错误：这不是有效的Excel生成器配置文件";
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 在指定文件夹创建新配置
        /// </summary>
        /// <returns>是否创建成功</returns>
        private bool CreateNewConfigInFolder()
        {
            string folderPath;
            
            // 获取文件夹路径
            if (_saveFolder == null)
            {
                // 使用默认路径
                folderPath = _configType == ConfigType.UIManager
                    ? Core.FrameworkDefaultPaths.UIManagerSettingsFolder
                    : Core.FrameworkDefaultPaths.ExcelGeneratorSettingsFolder;
                
                // 确保默认路径存在
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                // 使用用户选择的路径
                folderPath = AssetDatabase.GetAssetPath(_saveFolder);
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    EditorUtility.DisplayDialog("创建失败", "选择的不是有效的文件夹", "确定");
                    return false;
                }
            }
            
            // 生成文件名
            string fileName = _configType == ConfigType.UIManager
                ? Core.FrameworkDefaultPaths.UIManagerSettingsFileName
                : Core.FrameworkDefaultPaths.ExcelGeneratorSettingsFileName;
            
            // 组合完整路径
            var savePath = Path.Combine(folderPath, fileName);
            
            // 检查文件是否已存在
            if (File.Exists(savePath))
            {
                if (!EditorUtility.DisplayDialog(
                    "文件已存在",
                    $"文件 {fileName} 已存在\n是否覆盖？",
                    "覆盖",
                    "取消"))
                {
                    return false; // 用户取消
                }
            }
            
            // 创建配置文件
            ScriptableObject config = null;
            switch (_configType)
            {
                case ConfigType.UIManager:
                    config = CreateDefaultUIManagerSettings();
                    break;
                case ConfigType.ExcelGenerator:
                    config = CreateDefaultExcelGeneratorSettings();
                    break;
            }
            
            if (config == null)
            {
                EditorUtility.DisplayDialog("创建失败", "无法创建配置对象", "确定");
                return false;
            }
            
            try
            {
                AssetDatabase.CreateAsset(config, savePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // 关联到索引
                LinkConfigToIndex(config);
                
                // 选中配置文件
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
                
                // 如果是UI管理器配置，打开初始化向导
                if (_configType == ConfigType.UIManager && config is UI.UIManagerSettings uiSettings)
                {
                    // 延迟调用，确保当前窗口关闭后再打开新窗口
                    EditorApplication.delayCall += () =>
                    {
                        UI.UIManagerSetupWizard.ShowWizard(uiSettings);
                    };
                }
                
                // 成功，不弹窗提示
                return true;
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("创建失败", $"创建配置文件失败:\n{ex.Message}", "确定");
                return false;
            }
        }
        
        /// <summary>
        /// 检查并显示下一步引导
        /// </summary>
        private void CheckAndShowNextStep()
        {
            if (_configType == ConfigType.UIManager)
            {
                var uiSettings = UI.UIManagerSettings.Instance;
                if (uiSettings != null && !uiSettings.IsInitialized())
                {
                    // 需要继续初始化，延迟调用打开向导
                    EditorApplication.delayCall += () =>
                    {
                        UI.UIManagerSetupWizard.ShowWizard(uiSettings);
                    };
                }
            }
        }
        
        /// <summary>
        /// 将配置关联到索引文件
        /// </summary>
        private void LinkConfigToIndex(ScriptableObject config)
        {
            // 确保索引文件存在
            var index = FrameworkSettingsIndex.GetOrCreate();
            
            // 根据类型关联
            switch (_configType)
            {
                case ConfigType.UIManager:
                    index.UIManagerSettings = config as UI.UIManagerSettings;
                    break;
                case ConfigType.ExcelGenerator:
                    index.ExcelGeneratorSettings = config as Excel.ExcelGeneratorSettings;
                    break;
            }
            
            index.Save();
        }
        
        /// <summary>
        /// 创建默认UI管理器配置
        /// </summary>
        private UI.UIManagerSettings CreateDefaultUIManagerSettings()
        {
            var settings = CreateInstance<UI.UIManagerSettings>();
            return settings;
        }
        
        /// <summary>
        /// 创建默认Excel生成器配置
        /// </summary>
        private Excel.ExcelGeneratorSettings CreateDefaultExcelGeneratorSettings()
        {
            var settings = CreateInstance<Excel.ExcelGeneratorSettings>();
            return settings;
        }
    }
}
#endif

