#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI管理器初始化向导
    /// 引导用户完成UI管理器的初始配置
    /// </summary>
    public class UIManagerSetupWizard : EditorWindow
    {
        private UIManagerSettings _settings;
        private Vector2 _scrollPosition;
        
        // 分辨率设置
        private int _resolutionWidth = Core.FrameworkDefaultPaths.DefaultResolutionWidth;
        private int _resolutionHeight = Core.FrameworkDefaultPaths.DefaultResolutionHeight;
        private float _matchWidthOrHeight = Core.FrameworkDefaultPaths.DefaultMatchWidthOrHeight;
        
        // 命名空间
        private string _defaultNamespace;
        
        /// <summary>
        /// 显示初始化向导
        /// </summary>
        public static void ShowWizard(UIManagerSettings settings)
        {
            var window = GetWindow<UIManagerSetupWizard>(true, "UI管理器初始化向导", true);
            window._settings = settings;
            window.minSize = new Vector2(560, 520);
            window.maxSize = new Vector2(560, 520);
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
            _defaultNamespace = string.IsNullOrEmpty(_settings.DefaultNamespace) 
                ? Core.FrameworkDefaultPaths.UIDefaultNamespace 
                : _settings.DefaultNamespace;
            
            // 尝试加载现有的UI项目配置
            var projectConfig = UIProjectConfigEditorHelper.GetConfig();
            if (projectConfig != null)
            {
                _resolutionWidth = projectConfig.ReferenceResolutionWidth;
                _resolutionHeight = projectConfig.ReferenceResolutionHeight;
                _matchWidthOrHeight = projectConfig.MatchWidthOrHeight;
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
            EditorGUILayout.LabelField("UI管理器初始化向导", titleStyle);
            
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
                    
                    // 打开UI管理器主窗口
                    EditorApplication.delayCall += () =>
                    {
                        var window = GetWindow<UIManagerWindow>("UI管理器");
                        window.minSize = new Vector2(1000, 600);
                        window.Show();
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
            // 1. UI项目配置
            DrawConfigFileField(
                "1. UI项目配置文件",
                _settings.ConfigCodeFile,
                (file) => _settings.ConfigCodeFile = file,
                Core.FrameworkDefaultPaths.UIProjectConfigCodePath
            );
            
            EditorGUILayout.Space(15);
            
            // 2. 默认预制体目录
            DrawFolderField(
                "2. 默认预制体目录",
                _settings.UIPrefabCreationFolder,
                (folder) => _settings.UIPrefabCreationFolder = folder,
                Core.FrameworkDefaultPaths.UIPrefabFolder
            );
            
            EditorGUILayout.Space(15);
            
            // 3. UI脚本目录
            DrawFolderField(
                "3. UI脚本目录",
                _settings.LogicScriptOutputFolder,
                (folder) => _settings.LogicScriptOutputFolder = folder,
                Core.FrameworkDefaultPaths.UILogicScriptFolder
            );
            
            EditorGUILayout.Space(15);
            
            // 4. 分辨率设置
            EditorGUILayout.LabelField("4. Canvas分辨率设置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("宽度:", GUILayout.Width(60));
            _resolutionWidth = EditorGUILayout.IntField(_resolutionWidth, GUILayout.Width(100));
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("高度:", GUILayout.Width(60));
            _resolutionHeight = EditorGUILayout.IntField(_resolutionHeight, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 5. 匹配模式设置
            EditorGUILayout.LabelField("5. 屏幕匹配模式", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _matchWidthOrHeight = EditorGUILayout.Slider(_matchWidthOrHeight, 0f, 1f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("横屏(1)", GUILayout.Width(80)))
            {
                _matchWidthOrHeight = 1f;
            }
            if (GUILayout.Button("平衡(0.5)", GUILayout.Width(80)))
            {
                _matchWidthOrHeight = 0.5f;
            }
            if (GUILayout.Button("竖屏(0)", GUILayout.Width(80)))
            {
                _matchWidthOrHeight = 0f;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制文件夹字段
        /// </summary>
        private void DrawFolderField(string label, DefaultAsset currentFolder, System.Action<DefaultAsset> setter, string defaultPath)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            var newFolder = EditorGUILayout.ObjectField(
                currentFolder,
                typeof(DefaultAsset),
                false,
                GUILayout.Width(400),
                GUILayout.Height(25)
            ) as DefaultAsset;
            
            if (newFolder != currentFolder)
            {
                setter(newFolder);
            }
            
            if (GUILayout.Button("浏览...", GUILayout.Width(60)))
            {
                var folder = BrowseFolder("选择文件夹", defaultPath);
                if (folder != null)
                {
                    setter(folder);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 显示默认路径提示
            var hintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
            };
            
            if (currentFolder == null)
            {
                EditorGUILayout.LabelField($"默认: {defaultPath}", hintStyle);
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(currentFolder);
                EditorGUILayout.LabelField($"当前: {path}", hintStyle);
            }
        }
        
        /// <summary>
        /// 绘制配置文件字段
        /// </summary>
        private void DrawConfigFileField(string label, MonoScript currentFile, System.Action<MonoScript> setter, string defaultPath)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            var newFile = EditorGUILayout.ObjectField(
                currentFile,
                typeof(MonoScript),
                false,
                GUILayout.Width(400),
                GUILayout.Height(25)
            ) as MonoScript;
            
            if (newFile != currentFile)
            {
                setter(newFile);
            }
            
            if (GUILayout.Button("浏览...", GUILayout.Width(60)))
            {
                var file = BrowseFile("选择配置文件", defaultPath, "cs");
                if (file != null)
                {
                    setter(file);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 显示默认路径提示
            var hintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
            };
            
            if (currentFile == null)
            {
                EditorGUILayout.LabelField($"默认: {defaultPath}", hintStyle);
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(currentFile);
                EditorGUILayout.LabelField($"当前: {path}", hintStyle);
            }
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
                // 1. 保存命名空间（如果为空则使用框架默认值）
                _settings.DefaultNamespace = string.IsNullOrEmpty(_defaultNamespace) 
                    ? Core.FrameworkDefaultPaths.UIDefaultNamespace 
                    : _defaultNamespace;
                
                // 2. UI项目配置文件 - 未设置则创建
                if (_settings.ConfigCodeFile == null)
                {
                    var defaultConfigPath = Core.FrameworkDefaultPaths.UIProjectConfigCodePath;
                    
                    // 确保目录存在
                    var dir = Path.GetDirectoryName(defaultConfigPath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    
                    // 创建默认配置代码文件
                    UIProjectConfigEditorHelper.CreateConfigCodeFile(defaultConfigPath);
                    AssetDatabase.Refresh();
                    
                    // 加载创建的文件
                    _settings.ConfigCodeFile = AssetDatabase.LoadAssetAtPath<MonoScript>(defaultConfigPath);
                }
                
                // 3. UI预制体创建目录 - 未设置则使用默认路径
                if (_settings.UIPrefabCreationFolder == null)
                {
                    EnsureFolderExists(Core.FrameworkDefaultPaths.UIPrefabFolder);
                    _settings.UIPrefabCreationFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Core.FrameworkDefaultPaths.UIPrefabFolder);
                }
                
                // 确保UI预制体创建目录在PrefabFolders列表中
                if (_settings.UIPrefabCreationFolder != null && !_settings.PrefabFolders.Contains(_settings.UIPrefabCreationFolder))
                {
                    _settings.PrefabFolders.Add(_settings.UIPrefabCreationFolder);
                }
                
                // 4. UI逻辑脚本目录 - 未设置则使用默认路径
                if (_settings.LogicScriptOutputFolder == null)
                {
                    EnsureFolderExists(Core.FrameworkDefaultPaths.UILogicScriptFolder);
                    _settings.LogicScriptOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Core.FrameworkDefaultPaths.UILogicScriptFolder);
                }
                
                // 保存所有设置
                _settings.Save();
                
                // 5. 保存UI项目配置（分辨率和匹配模式）
                var projectConfig = UIProjectConfigEditorHelper.GetConfig();
                if (projectConfig != null)
                {
                    projectConfig.ReferenceResolutionWidth = _resolutionWidth;
                    projectConfig.ReferenceResolutionHeight = _resolutionHeight;
                    projectConfig.MatchWidthOrHeight = _matchWidthOrHeight;
                    
                    UIProjectConfigEditorHelper.SaveConfig(projectConfig);
                }
                
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
        private void EnsureFolderExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
        
        /// <summary>
        /// 浏览文件夹
        /// </summary>
        private DefaultAsset BrowseFolder(string title, string defaultPath)
        {
            if (string.IsNullOrEmpty(defaultPath) || !Directory.Exists(defaultPath))
            {
                defaultPath = "Assets";
            }
            
            var selectedPath = EditorUtility.OpenFolderPanel(title, defaultPath, "");
            
            if (string.IsNullOrEmpty(selectedPath))
                return null;
            
            if (!selectedPath.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("选择失败", "文件夹必须在Assets目录下", "确定");
                return null;
            }
            
            var relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(relativePath);
            
            if (folder == null)
            {
                EditorUtility.DisplayDialog("选择失败", "选择的不是有效的文件夹", "确定");
            }
            
            return folder;
        }
        
        /// <summary>
        /// 浏览文件
        /// </summary>
        private MonoScript BrowseFile(string title, string defaultPath, string extension)
        {
            var dir = Path.GetDirectoryName(defaultPath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                dir = "Assets";
            }
            
            var selectedPath = EditorUtility.OpenFilePanel(title, dir, extension);
            
            if (string.IsNullOrEmpty(selectedPath))
                return null;
            
            if (!selectedPath.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("选择失败", "文件必须在Assets目录下", "确定");
                return null;
            }
            
            var relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            var file = AssetDatabase.LoadAssetAtPath<MonoScript>(relativePath);
            
            if (file == null)
            {
                EditorUtility.DisplayDialog("选择失败", "选择的不是有效的脚本文件", "确定");
            }
            
            return file;
        }
    }
}
#endif

