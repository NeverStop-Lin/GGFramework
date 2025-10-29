#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Framework.Core;

namespace Framework.Editor.UI
{
    /// <summary>
    /// 设置Tab
    /// 管理配置文件路径和其他全局设置
    /// </summary>
    public class SettingsTab
    {
        private UIManagerSettings _settings;
        private string _defaultNamespace;
        private UIProjectConfig _currentConfig;
        private UIManagerWindow _parentWindow;
        private Vector2 _scrollPosition;
        
        public void OnEnable()
        {
            // 重新加载设置实例
            UIManagerSettings.Reload();
            _settings = UIManagerSettings.Instance;
            
            // 加载设置
            if (_settings != null)
            {
                _defaultNamespace = _settings.DefaultNamespace;
            }
            
            // 加载UI项目配置
            LoadConfig();
        }
        
        public void SetParentWindow(UIManagerWindow window)
        {
            _parentWindow = window;
        }
        
        public void OnGUI()
        {
            EditorGUILayout.LabelField("UI管理器设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 使用垂直布局，固定底部区域
            EditorGUILayout.BeginVertical();
            
            // 可滚动内容区域（自动填充剩余空间，为底部按钮留出约80px）
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            
            DrawProjectConfigSection();
            EditorGUILayout.Space(10);
            DrawCanvasSettingsSection();
            EditorGUILayout.Space(10);
            DrawUICreationSection();
            EditorGUILayout.Space(10);
            DrawCodeGenSection();
            
            EditorGUILayout.EndScrollView();
            
            // 固定底部区域（不随内容滚动）
            EditorGUILayout.Space(10);
            
            // 使用分隔线
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            
            EditorGUILayout.Space(10);
            DrawSaveSection();
            
            EditorGUILayout.EndVertical();
        }
        
        public void OnDisable()
        {
        }
        
        private void DrawProjectConfigSection()
        {
            EditorGUILayout.LabelField("UI项目配置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            if (_settings != null)
            {
                // 配置文件（Object引用）
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("配置文件:", GUILayout.Width(120));
                
                var newConfigFile = EditorGUILayout.ObjectField(
                    _settings.ConfigCodeFile,
                    typeof(MonoScript),
                    false
                ) as MonoScript;
                
                if (newConfigFile != _settings.ConfigCodeFile)
                {
                    _settings.ConfigCodeFile = newConfigFile;
                    _settings.Save();
                    LoadConfig();
                }
                
                // 浏览按钮
                if (GUILayout.Button("浏览...", GUILayout.Width(60)))
                {
                    var file = BrowseFile("选择UI项目配置文件", _settings.ConfigCodeFilePath, "cs");
                    if (file != null)
                    {
                        _settings.ConfigCodeFile = file;
                        _settings.Save();
                        LoadConfig();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("未加载编辑器设置", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 创建配置代码文件
        /// </summary>
        private void CreateConfigCodeFile()
        {
            // 让用户选择保存位置
            var defaultPath = "Assets/Game/Scripts/Generated/UIProjectConfigData.cs";
            var savePath = EditorUtility.SaveFilePanel(
                "创建 UI 项目配置文件",
                System.IO.Path.GetDirectoryName(defaultPath),
                "UIProjectConfigData.cs",
                "cs"
            );
            
            if (string.IsNullOrEmpty(savePath))
                return;
            
            // 转换为相对路径
            if (!savePath.StartsWith(UnityEngine.Application.dataPath))
            {
                EditorUtility.DisplayDialog("路径错误", "配置文件必须在 Assets 目录下", "确定");
                return;
            }
            
            var relativePath = "Assets" + savePath.Substring(UnityEngine.Application.dataPath.Length);
            
            // 创建配置文件
            UIProjectConfigEditorHelper.CreateConfigCodeFile(relativePath);
            
            // 刷新
            AssetDatabase.Refresh();
            
            // 重新加载配置
            LoadConfig();
            
            EditorUtility.DisplayDialog("成功", $"配置文件已创建:\n{relativePath}", "确定");
        }
        
        /// <summary>
        /// 重新选择配置代码文件
        /// </summary>
        private void SelectConfigCodeFile()
        {
            var currentPath = UIProjectConfigEditorHelper.GetConfigCodeFilePath();
            var defaultDir = string.IsNullOrEmpty(currentPath) 
                ? "Assets/Game/Scripts/Generated" 
                : System.IO.Path.GetDirectoryName(currentPath);
            
            var selectedPath = EditorUtility.OpenFilePanel(
                "选择 UI 项目配置文件",
                defaultDir,
                "cs"
            );
            
            if (string.IsNullOrEmpty(selectedPath))
                return;
            
            // 转换为相对路径
            if (!selectedPath.StartsWith(UnityEngine.Application.dataPath))
            {
                EditorUtility.DisplayDialog("路径错误", "配置文件必须在 Assets 目录下", "确定");
                return;
            }
            
            var relativePath = "Assets" + selectedPath.Substring(UnityEngine.Application.dataPath.Length);
            
            // 验证文件名
            if (!System.IO.Path.GetFileName(relativePath).Contains("UIProjectConfigData"))
            {
                var confirm = EditorUtility.DisplayDialog(
                    "文件名不匹配",
                    $"选择的文件名不包含 'UIProjectConfigData'\n" +
                    $"确定要使用这个文件吗？\n\n{relativePath}",
                    "确定",
                    "取消"
                );
                
                if (!confirm)
                    return;
            }
            
            // 更新设置
            _settings.ConfigCodeFilePath = relativePath;
            _settings.Save();
            
            // 重新加载
            UIProjectConfigManager.Reload();
            LoadConfig();
            
            EditorUtility.DisplayDialog("成功", $"已切换到:\n{relativePath}", "确定");
        }
        
        private void DrawUICreationSection()
        {
            EditorGUILayout.LabelField("UI创建设置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            if (_settings != null)
            {
                // 默认预制体目录（Object引用）
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("默认预制体目录:", GUILayout.Width(120));
                
                var newCreationFolder = EditorGUILayout.ObjectField(
                    _settings.UIPrefabCreationFolder, 
                    typeof(DefaultAsset), 
                    false
                ) as DefaultAsset;
                
                if (newCreationFolder != _settings.UIPrefabCreationFolder)
                {
                    _settings.UIPrefabCreationFolder = newCreationFolder;
                    
                    // 确保添加到Prefab目录列表
                    if (newCreationFolder != null && !_settings.PrefabFolders.Contains(newCreationFolder))
                    {
                        _settings.PrefabFolders.Add(newCreationFolder);
                    }
                    
                    _settings.Save();
                }
                
                // 浏览按钮（树状结构）
                if (GUILayout.Button("浏览...", GUILayout.Width(60)))
                {
                    var folder = BrowseFolder("选择默认预制体目录", _settings.UIPrefabCreationDefaultPath);
                    if (folder != null)
                    {
                        _settings.UIPrefabCreationFolder = folder;
                        
                        // 确保添加到Prefab目录列表
                        if (!_settings.PrefabFolders.Contains(folder))
                        {
                            _settings.PrefabFolders.Add(folder);
                        }
                        
                        _settings.Save();
                    }
                }
                
                // 在Finder/Explorer中显示
                if (GUILayout.Button("显示", GUILayout.Width(60)))
                {
                    if (_settings.UIPrefabCreationFolder != null)
                    {
                        var path = AssetDatabase.GetAssetPath(_settings.UIPrefabCreationFolder);
                        EditorUtility.RevealInFinder(path);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "请先设置预制体目录", "确定");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("未加载编辑器设置", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCanvasSettingsSection()
        {
            EditorGUILayout.LabelField("Canvas设计尺寸", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            if (_currentConfig != null)
            {
                EditorGUI.BeginChangeCheck();
                
                // 参考分辨率宽度
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("参考分辨率宽度:", GUILayout.Width(120));
                var width = EditorGUILayout.IntField(_currentConfig.ReferenceResolutionWidth);
                EditorGUILayout.EndHorizontal();
                
                // 参考分辨率高度
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("参考分辨率高度:", GUILayout.Width(120));
                var height = EditorGUILayout.IntField(_currentConfig.ReferenceResolutionHeight);
                EditorGUILayout.EndHorizontal();
                
                // 屏幕匹配模式
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("屏幕匹配模式:", GUILayout.Width(120));
                var match = EditorGUILayout.Slider(_currentConfig.MatchWidthOrHeight, 0f, 1f);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(120));
                EditorGUILayout.LabelField($"← 宽度优先 (0)        平衡 (0.5)        高度优先 (1) →", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndHorizontal();
                
                if (EditorGUI.EndChangeCheck())
                {
                    _currentConfig.ReferenceResolutionWidth = width;
                    _currentConfig.ReferenceResolutionHeight = height;
                    _currentConfig.MatchWidthOrHeight = match;
                }
                
                EditorGUILayout.Space(5);
                
                // 匹配模式快捷设置
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("匹配模式:", GUILayout.Width(120));
                
                var oldColor = GUI.backgroundColor;
                
                // 横屏按钮（高度优先）
                GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
                if (GUILayout.Button("横屏", GUILayout.Width(80)))
                {
                    _currentConfig.MatchWidthOrHeight = 1f; // 高度优先
                }
                
                // 竖屏按钮（宽度优先）
                GUI.backgroundColor = new Color(0.9f, 0.8f, 1f);
                if (GUILayout.Button("竖屏", GUILayout.Width(80)))
                {
                    _currentConfig.MatchWidthOrHeight = 0f; // 宽度优先
                }
                
                // 自定义按钮（平衡）
                GUI.backgroundColor = new Color(0.8f, 1f, 0.8f);
                if (GUILayout.Button("自定义", GUILayout.Width(80)))
                {
                    _currentConfig.MatchWidthOrHeight = 0.5f; // 平衡
                }
                
                GUI.backgroundColor = oldColor;
                
                EditorGUILayout.EndHorizontal();
                
                // 常用分辨率按钮
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("常用分辨率:", GUILayout.Width(120));
                
                if (GUILayout.Button("1920x1080", GUILayout.Width(100)))
                {
                    _currentConfig.ReferenceResolutionWidth = 1920;
                    _currentConfig.ReferenceResolutionHeight = 1080;
                }
                
                if (GUILayout.Button("1280x720", GUILayout.Width(100)))
                {
                    _currentConfig.ReferenceResolutionWidth = 1280;
                    _currentConfig.ReferenceResolutionHeight = 720;
                }
                
                if (GUILayout.Button("750x1334", GUILayout.Width(100)))
                {
                    _currentConfig.ReferenceResolutionWidth = 750;
                    _currentConfig.ReferenceResolutionHeight = 1334;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("未加载UI项目配置", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCodeGenSection()
        {
            EditorGUILayout.LabelField("代码生成设置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            if (_settings != null)
            {
                // 默认命名空间
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("默认命名空间:", GUILayout.Width(120));
                _defaultNamespace = EditorGUILayout.TextField(_defaultNamespace);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // UI脚本输出文件夹（Object引用）
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("UI脚本目录:", GUILayout.Width(120));
                var newLogicFolder = EditorGUILayout.ObjectField(
                    _settings.LogicScriptOutputFolder, 
                    typeof(DefaultAsset), 
                    false
                ) as DefaultAsset;
                
                if (newLogicFolder != _settings.LogicScriptOutputFolder)
                {
                    _settings.LogicScriptOutputFolder = newLogicFolder;
                }
                
                // 浏览按钮（树状结构）
                if (GUILayout.Button("浏览...", GUILayout.Width(60)))
                {
                    var folder = BrowseFolder("选择UI脚本输出目录", _settings.LogicScriptOutputPath);
                    if (folder != null)
                    {
                        _settings.LogicScriptOutputFolder = folder;
                    }
                }
                
                // 在Finder/Explorer中显示
                if (GUILayout.Button("显示", GUILayout.Width(60)))
                {
                    if (_settings.LogicScriptOutputFolder != null)
                    {
                        var path = AssetDatabase.GetAssetPath(_settings.LogicScriptOutputFolder);
                        EditorUtility.RevealInFinder(path);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "请先设置UI脚本目录", "确定");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("未加载编辑器设置", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSaveSection()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("保存设置", GUILayout.Width(150), GUILayout.Height(30)))
            {
                SaveSettings();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private void LoadConfig()
        {
            // 获取配置（不需要每次都Reload，只在必要时重新加载）
            _currentConfig = UIProjectConfigEditorHelper.GetConfig();
        }
        
        
        private void SaveSettings()
        {
            if (_settings != null)
            {
                _settings.DefaultNamespace = _defaultNamespace;
                _settings.Save();
            }
            
            // 保存配置数据（触发代码生成）
            if (_currentConfig != null)
            {
                UIProjectConfigEditorHelper.SaveConfig(_currentConfig);
                EditorUtility.DisplayDialog("成功", "设置和配置已保存并生成代码", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("成功", "设置已保存", "确定");
            }
        }
        
        public string GetDefaultNamespace()
        {
            return _defaultNamespace;
        }
        
        public string GetLogicOutputPath()
        {
            return _settings?.LogicScriptOutputPath ?? "";
        }
        
        public UIProjectConfig GetCurrentConfig()
        {
            return _currentConfig;
        }
        
        /// <summary>
        /// 浏览文件夹（树状结构）
        /// </summary>
        private MonoScript BrowseFile(string title, string defaultPath, string extension)
        {
            var dir = System.IO.Path.GetDirectoryName(defaultPath);
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir))
            {
                dir = "Assets";
            }
            
            // 使用系统文件浏览器选择文件（树状结构）
            var selectedPath = EditorUtility.OpenFilePanel(
                title,
                dir,
                extension
            );
            
            if (string.IsNullOrEmpty(selectedPath))
                return null;
            
            // 转换为相对路径
            if (!selectedPath.StartsWith(UnityEngine.Application.dataPath))
            {
                EditorUtility.DisplayDialog("错误", "文件必须在Assets目录下", "确定");
                return null;
            }
            
            var relativePath = "Assets" + selectedPath.Substring(UnityEngine.Application.dataPath.Length);
            
            // 加载文件
            var file = AssetDatabase.LoadAssetAtPath<MonoScript>(relativePath);
            
            if (file == null)
            {
                EditorUtility.DisplayDialog("错误", "选择的不是有效的脚本文件", "确定");
                return null;
            }
            
            return file;
        }
        
        private DefaultAsset BrowseFolder(string title, string defaultPath)
        {
            if (string.IsNullOrEmpty(defaultPath))
            {
                defaultPath = "Assets";
            }
            
            // 使用系统文件浏览器选择文件夹（树状结构）
            var selectedPath = EditorUtility.OpenFolderPanel(
                title,
                defaultPath,
                ""
            );
            
            if (string.IsNullOrEmpty(selectedPath))
                return null;
            
            // 转换为相对路径
            if (!selectedPath.StartsWith(UnityEngine.Application.dataPath))
            {
                EditorUtility.DisplayDialog("错误", "文件夹必须在Assets目录下", "确定");
                return null;
            }
            
            var relativePath = "Assets" + selectedPath.Substring(UnityEngine.Application.dataPath.Length);
            
            // 加载文件夹
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(relativePath);
            
            if (folder == null)
            {
                EditorUtility.DisplayDialog("错误", "选择的不是有效的文件夹", "确定");
                return null;
            }
            
            return folder;
        }
    }
}
#endif

