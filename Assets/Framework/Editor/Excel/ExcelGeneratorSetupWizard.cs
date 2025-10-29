#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.Excel
{
    /// <summary>
    /// Excel生成器初始化向导
    /// 引导用户完成Excel生成器的初始配置
    /// </summary>
    public class ExcelGeneratorSetupWizard : EditorWindow
    {
        private ExcelGeneratorSettings _settings;
        private Vector2 _scrollPosition;
        
        // Excel根目录路径
        private string _excelRootPath;
        
        // 默认命名空间
        private string _defaultNamespace;
        
        /// <summary>
        /// 显示初始化向导
        /// </summary>
        public static void ShowWizard(ExcelGeneratorSettings settings)
        {
            var window = GetWindow<ExcelGeneratorSetupWizard>(true, "Excel生成器初始化向导", true);
            window._settings = settings;
            window.minSize = new Vector2(560, 480);
            window.maxSize = new Vector2(560, 480);
            window.InitializeDefaults();
            window.Show();
        }
        
        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaults()
        {
            if (_settings == null) return;
            
            // 从现有配置读取（如果有），否则使用框架默认值
            _excelRootPath = string.IsNullOrEmpty(_settings.ExcelRootPath) 
                ? Core.FrameworkDefaultPaths.ExcelRootFolder 
                : _settings.ExcelRootPath;
            
            _defaultNamespace = string.IsNullOrEmpty(_settings.DefaultNamespace) 
                ? Core.FrameworkDefaultPaths.ExcelDefaultNamespace 
                : _settings.DefaultNamespace;
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
            EditorGUILayout.LabelField("Excel生成器初始化向导", titleStyle);
            
            EditorGUILayout.Space(5);
            
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                wordWrap = true
            };
            EditorGUILayout.LabelField("请配置以下设置以完成初始化", subtitleStyle);
            
            EditorGUILayout.Space(10);
            
            // 分隔线
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            
            EditorGUILayout.Space(10);
            
            // 滚动区域
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawConfigSettings();
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // 完成按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("完成初始化", GUILayout.Width(200), GUILayout.Height(40)))
            {
                if (ApplySettings())
                {
                    // 关闭向导
                    Close();
                    
                    // 打开Excel生成器主窗口
                    EditorApplication.delayCall += () =>
                    {
                        ExcelGeneratorWindow.ShowWindow();
                    };
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(15);
        }
        
        /// <summary>
        /// 绘制配置设置
        /// </summary>
        private void DrawConfigSettings()
        {
            // 1. Excel根目录（支持项目外路径）
            DrawExcelRootPathField();
            
            EditorGUILayout.Space(15);
            
            // 2. JSON输出目录
            DrawFolderField(
                "2. JSON数据输出目录",
                _settings.JsonOutputFolder,
                (folder) => _settings.JsonOutputFolder = folder,
                Core.FrameworkDefaultPaths.ExcelJsonOutputFolder,
                "生成的JSON配置文件保存目录"
            );
            
            EditorGUILayout.Space(15);
            
            // 3. C#输出目录
            DrawFolderField(
                "3. C#代码输出目录",
                _settings.CSharpOutputFolder,
                (folder) => _settings.CSharpOutputFolder = folder,
                Core.FrameworkDefaultPaths.ExcelCSharpOutputFolder,
                "生成的C#配置代码保存目录"
            );
            
            EditorGUILayout.Space(15);
            
            // 4. 默认命名空间
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("4. 默认命名空间", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            _defaultNamespace = EditorGUILayout.TextField("命名空间", _defaultNamespace);
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制Excel根目录路径字段（支持项目外路径）
        /// </summary>
        private void DrawExcelRootPathField()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("1. Excel根目录", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Excel文件根目录（支持项目外路径，如：C:/Excel 或 Assets/../Excel）", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(3);
            
            EditorGUILayout.BeginHorizontal();
            
            _excelRootPath = EditorGUILayout.TextField("路径", _excelRootPath, GUILayout.Height(30));
            
            if (GUILayout.Button("浏览...", GUILayout.Width(70), GUILayout.Height(30)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("选择Excel根目录", _excelRootPath ?? "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 尝试转换为相对路径（如果在项目内）
                    var projectPath = Path.GetFullPath(Application.dataPath + "/..");
                    if (selectedPath.StartsWith(projectPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        selectedPath = selectedPath.Replace("\\", "/");
                    }
                    _excelRootPath = selectedPath;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 显示默认路径提示
            if (string.IsNullOrEmpty(_excelRootPath))
            {
                var hintStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.3f, 0.6f, 1f) }
                };
                EditorGUILayout.LabelField($"默认: {Core.FrameworkDefaultPaths.ExcelRootFolder}", hintStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制文件夹选择字段
        /// </summary>
        private void DrawFolderField(string label, DefaultAsset currentFolder, System.Action<DefaultAsset> onChanged, string defaultPath, string hint = null)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            if (!string.IsNullOrEmpty(hint))
            {
                EditorGUILayout.LabelField(hint, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(3);
            }
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginChangeCheck();
            var newFolder = EditorGUILayout.ObjectField("目录", currentFolder, typeof(DefaultAsset), false, GUILayout.Height(30)) as DefaultAsset;
            if (EditorGUI.EndChangeCheck())
            {
                onChanged?.Invoke(newFolder);
            }
            
            if (GUILayout.Button("浏览...", GUILayout.Width(70), GUILayout.Height(30)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("选择目录", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 转换为相对路径
                    var projectPath = System.IO.Path.GetFullPath(Application.dataPath + "/..");
                    if (selectedPath.StartsWith(projectPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        selectedPath = selectedPath.Replace("\\", "/");
                        
                        if (!AssetDatabase.IsValidFolder(selectedPath))
                        {
                            EnsureFolderExists(selectedPath);
                        }
                        
                        var folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(selectedPath);
                        onChanged?.Invoke(folderAsset);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "请选择项目内的目录", "确定");
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 显示默认路径提示
            if (currentFolder == null && !string.IsNullOrEmpty(defaultPath))
            {
                var hintStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.3f, 0.6f, 1f) }
                };
                EditorGUILayout.LabelField($"默认: {defaultPath}", hintStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 应用设置
        /// </summary>
        private bool ApplySettings()
        {
            if (_settings == null)
            {
                EditorUtility.DisplayDialog("初始化失败", "配置对象为空", "确定");
                return false;
            }
            
            try
            {
                // 1. Excel根目录 - 使用默认值或用户设置的路径
                if (string.IsNullOrEmpty(_excelRootPath))
                {
                    _excelRootPath = Core.FrameworkDefaultPaths.ExcelRootFolder;
                }
                
                // 确保Excel根目录存在
                var fullExcelPath = _excelRootPath;
                if (_excelRootPath.StartsWith("Assets/../"))
                {
                    fullExcelPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", _excelRootPath.Substring("Assets/../".Length)));
                }
                else if (_excelRootPath.StartsWith("Assets/"))
                {
                    fullExcelPath = Path.Combine(Application.dataPath, _excelRootPath.Substring("Assets/".Length));
                }
                
                if (!Directory.Exists(fullExcelPath))
                {
                    Directory.CreateDirectory(fullExcelPath);
                }
                
                _settings.ExcelRootPath = _excelRootPath;
                
                // 2. JSON输出目录 - 使用默认值
                if (_settings.JsonOutputFolder == null)
                {
                    EnsureFolderExists(Core.FrameworkDefaultPaths.ExcelJsonOutputFolder);
                    _settings.JsonOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Core.FrameworkDefaultPaths.ExcelJsonOutputFolder);
                }
                
                // 3. C#输出目录 - 使用默认值
                if (_settings.CSharpOutputFolder == null)
                {
                    EnsureFolderExists(Core.FrameworkDefaultPaths.ExcelCSharpOutputFolder);
                    _settings.CSharpOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Core.FrameworkDefaultPaths.ExcelCSharpOutputFolder);
                }
                
                // 4. 默认命名空间
                _settings.DefaultNamespace = string.IsNullOrEmpty(_defaultNamespace)
                    ? Core.FrameworkDefaultPaths.ExcelDefaultNamespace
                    : _defaultNamespace;
                
                _settings.Save();
                return true;
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("初始化失败", $"保存配置失败:\n{ex.Message}", "确定");
                return false;
            }
        }
        
        /// <summary>
        /// 确保文件夹存在
        /// </summary>
        private void EnsureFolderExists(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;
            
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                var parts = folderPath.Split('/');
                var currentPath = parts[0];
                
                for (int i = 1; i < parts.Length; i++)
                {
                    var nextPath = currentPath + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath = nextPath;
                }
                
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif

