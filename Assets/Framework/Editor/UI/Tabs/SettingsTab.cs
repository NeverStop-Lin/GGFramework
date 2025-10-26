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
        private string _logicOutputPath;
        private string _bindingOutputPath;
        private UIProjectConfig _currentConfig;
        private UIManagerWindow _parentWindow;
        
        public void OnEnable()
        {
            // 重新加载设置实例
            UIManagerSettings.Reload();
            _settings = UIManagerSettings.Instance;
            
            // 加载设置
            if (_settings != null)
            {
                _defaultNamespace = _settings.DefaultNamespace;
                _logicOutputPath = _settings.LogicScriptOutputPath;
                _bindingOutputPath = _settings.BindingScriptOutputPath;
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
            
            DrawProjectConfigSection();
            EditorGUILayout.Space(10);
            DrawCanvasSettingsSection();
            EditorGUILayout.Space(10);
            DrawCodeGenSection();
            EditorGUILayout.Space(10);
            DrawSaveSection();
        }
        
        public void OnDisable()
        {
        }
        
        private void DrawProjectConfigSection()
        {
            EditorGUILayout.LabelField("UI项目配置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // 当前配置（支持拖拽替换）
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("当前配置:", GUILayout.Width(100));
            
            var newConfig = (UIProjectConfig)EditorGUILayout.ObjectField(
                _currentConfig, 
                typeof(UIProjectConfig), 
                false
            );
            
            if (newConfig != _currentConfig && newConfig != null)
            {
                // 验证必须在Resources文件夹中
                var newPath = AssetDatabase.GetAssetPath(newConfig);
                if (!newPath.Contains("/Resources/"))
                {
                    EditorUtility.DisplayDialog(
                        "路径错误",
                        "UIProjectConfig 必须放在 Resources 文件夹中！",
                        "确定"
                    );
                }
                else
                {
                    // 更新配置
                    _currentConfig = newConfig;
                    
                    // 更新路径
                    var resourcesIndex = newPath.IndexOf("Resources/");
                    if (resourcesIndex >= 0)
                    {
                        var configPath = newPath.Substring(resourcesIndex + "Resources/".Length);
                        if (configPath.EndsWith(".asset"))
                        {
                            configPath = configPath.Substring(0, configPath.Length - 6);
                        }
                        
                        _settings.ConfigPath = configPath;
                        _settings.Save();
                        UIProjectConfigManager.SetConfigPath(configPath);
                        UIProjectConfigManager.Reload();
                        
                        // 刷新所有Tab
                        if (_parentWindow != null)
                        {
                            _parentWindow.RequestRefresh();
                        }
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 显示路径
            if (_currentConfig != null)
            {
                var currentPath = AssetDatabase.GetAssetPath(_currentConfig);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("路径:", GUILayout.Width(100));
                EditorGUILayout.SelectableLabel(currentPath, EditorStyles.miniLabel, GUILayout.Height(16));
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.HelpBox(
                "💡 拖拽 UIProjectConfig 配置文件到上方输入框即可切换配置\n" +
                "注意：配置文件必须在 Resources 文件夹中",
                MessageType.Info
            );
            
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
                    EditorUtility.SetDirty(_currentConfig);
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
                    EditorUtility.SetDirty(_currentConfig);
                }
                
                // 竖屏按钮（宽度优先）
                GUI.backgroundColor = new Color(0.9f, 0.8f, 1f);
                if (GUILayout.Button("竖屏", GUILayout.Width(80)))
                {
                    _currentConfig.MatchWidthOrHeight = 0f; // 宽度优先
                    EditorUtility.SetDirty(_currentConfig);
                }
                
                // 自定义按钮（平衡）
                GUI.backgroundColor = new Color(0.8f, 1f, 0.8f);
                if (GUILayout.Button("自定义", GUILayout.Width(80)))
                {
                    _currentConfig.MatchWidthOrHeight = 0.5f; // 平衡
                    EditorUtility.SetDirty(_currentConfig);
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
                    EditorUtility.SetDirty(_currentConfig);
                }
                
                if (GUILayout.Button("1280x720", GUILayout.Width(100)))
                {
                    _currentConfig.ReferenceResolutionWidth = 1280;
                    _currentConfig.ReferenceResolutionHeight = 720;
                    EditorUtility.SetDirty(_currentConfig);
                }
                
                if (GUILayout.Button("750x1334", GUILayout.Width(100)))
                {
                    _currentConfig.ReferenceResolutionWidth = 750;
                    _currentConfig.ReferenceResolutionHeight = 1334;
                    EditorUtility.SetDirty(_currentConfig);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.HelpBox(
                    "💡 此设置将统一应用于所有UI预制体的Canvas Scaler\n" +
                    "• 创建/更新UI时会自动检查并修复不一致的配置\n" +
                    "• 横屏：匹配高度优先（Match=1），适合宽屏适配\n" +
                    "• 竖屏：匹配宽度优先（Match=0），适合窄屏适配\n" +
                    "• 自定义：平衡模式（Match=0.5），或手动调整滑块\n" +
                    "• 默认推荐：1280x720",
                    MessageType.Info
                );
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
            
            // 默认命名空间
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("默认命名空间:", GUILayout.Width(120));
            _defaultNamespace = EditorGUILayout.TextField(_defaultNamespace);
            EditorGUILayout.EndHorizontal();
            
            // 逻辑脚本输出路径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("逻辑脚本路径:", GUILayout.Width(120));
            _logicOutputPath = EditorGUILayout.TextField(_logicOutputPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("选择逻辑脚本输出目录", _logicOutputPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(UnityEngine.Application.dataPath))
                {
                    _logicOutputPath = "Assets" + path.Substring(UnityEngine.Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 绑定脚本输出路径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("绑定脚本路径:", GUILayout.Width(120));
            _bindingOutputPath = EditorGUILayout.TextField(_bindingOutputPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("选择绑定脚本输出目录", _bindingOutputPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(UnityEngine.Application.dataPath))
                {
                    _bindingOutputPath = "Assets" + path.Substring(UnityEngine.Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(
                "逻辑脚本：业务代码（.cs），仅首次生成\n" +
                "绑定脚本：自动生成代码（.Binding.cs），每次覆盖",
                MessageType.Info
            );
            
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
            _currentConfig = UIProjectConfigManager.GetConfig();
        }
        
        
        private void SaveSettings()
        {
            _settings.DefaultNamespace = _defaultNamespace;
            _settings.LogicScriptOutputPath = _logicOutputPath;
            _settings.BindingScriptOutputPath = _bindingOutputPath;
            _settings.Save();
            
            EditorUtility.DisplayDialog("成功", "设置已保存", "确定");
        }
        
        public string GetDefaultNamespace()
        {
            return _defaultNamespace;
        }
        
        public string GetLogicOutputPath()
        {
            return _logicOutputPath;
        }
        
        public string GetBindingOutputPath()
        {
            return _bindingOutputPath;
        }
        
        public UIProjectConfig GetCurrentConfig()
        {
            return _currentConfig;
        }
    }
}
#endif

