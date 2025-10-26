#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI管理器窗口
    /// 提供统一的UI管理界面，包括层级配置、UI配置、代码生成等功能
    /// </summary>
    public class UIManagerWindow : EditorWindow
    {
        private int _currentTab = 0;
        private readonly string[] _tabNames = { "UI管理", "层级配置", "设置" };
        
        private Vector2 _scrollPosition;
        
        // Tab实例
        private UIManagementTab _uiManagementTab;
        private LayerConfigTab _layerConfigTab;
        private SettingsTab _settingsTab;
        
        // 刷新标记
        private bool _needRefresh = false;
        
        [MenuItem("Tools/Framework/UI Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<UIManagerWindow>("UI管理器");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 确保编辑器配置存在（固定位置，自动创建）
            EnsureManagerSettingsExists();
            
            // 确保UI项目配置存在（默认位置，自动创建）
            EnsureProjectConfigExists();
            
            // 初始化Tab
            _uiManagementTab = new UIManagementTab();
            _layerConfigTab = new LayerConfigTab();
            _settingsTab = new SettingsTab();
            
            // 设置父窗口引用
            _settingsTab.SetParentWindow(this);
            
            _uiManagementTab.OnEnable();
            _layerConfigTab.OnEnable();
            _settingsTab.OnEnable();
        }
        
        /// <summary>
        /// 确保编辑器配置存在
        /// </summary>
        private void EnsureManagerSettingsExists()
        {
            var settingsPath = "Assets/Editor/Framework/Configs/UIManagerSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<UIManagerSettings>(settingsPath);
            
            if (settings == null)
            {
                // 自动创建
                var dir = System.IO.Path.GetDirectoryName(settingsPath);
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                
                settings = ScriptableObject.CreateInstance<UIManagerSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();
            }
        }
        
        /// <summary>
        /// 确保UI项目配置存在
        /// </summary>
        private void EnsureProjectConfigExists()
        {
            var settings = UIManagerSettings.Instance;
            if (settings == null) return;
            
            var configPath = settings.ConfigPath;
            var fullPath = $"Resources/{configPath}";
            
            // 先检查文件是否存在（避免调用GetConfig()打印警告）
            var config = Resources.Load<Framework.Core.UIProjectConfig>(configPath);
            
            if (config == null)
            {
                // 自动创建默认配置
                var defaultPath = "Assets/Resources/Framework/Configs/UIProjectConfig.asset";
                var dir = System.IO.Path.GetDirectoryName(defaultPath);
                
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                
                config = ScriptableObject.CreateInstance<Framework.Core.UIProjectConfig>();
                config.CreateDefaultLayers();
                
                AssetDatabase.CreateAsset(config, defaultPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // 更新配置路径
                var resourcesIndex = defaultPath.IndexOf("Resources/");
                if (resourcesIndex >= 0)
                {
                    var newConfigPath = defaultPath.Substring(resourcesIndex + "Resources/".Length);
                    if (newConfigPath.EndsWith(".asset"))
                    {
                        newConfigPath = newConfigPath.Substring(0, newConfigPath.Length - 6);
                    }
                    
                    settings.ConfigPath = newConfigPath;
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    
                    Framework.Core.UIProjectConfigManager.SetConfigPath(newConfigPath);
                }
                
                Framework.Core.UIProjectConfigManager.SetConfig(config);
            }
            else
            {
                // 配置已存在，设置到管理器
                Framework.Core.UIProjectConfigManager.SetConfig(config);
            }
        }
        
        private void OnGUI()
        {
            // 处理刷新请求
            if (_needRefresh)
            {
                RefreshAllTabs();
                _needRefresh = false;
            }
            
            // Tab选择
            EditorGUILayout.BeginHorizontal();
            _currentTab = GUILayout.Toolbar(_currentTab, _tabNames, GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 滚动区域
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // 显示当前Tab
            switch (_currentTab)
            {
                case 0:
                    _uiManagementTab.OnGUI();
                    break;
                case 1:
                    _layerConfigTab.OnGUI();
                    break;
                case 2:
                    _settingsTab.OnGUI();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 请求刷新所有Tab
        /// </summary>
        public void RequestRefresh()
        {
            _needRefresh = true;
            Repaint();
        }
        
        /// <summary>
        /// 刷新所有Tab
        /// </summary>
        private void RefreshAllTabs()
        {
            // 先刷新配置缓存
            Framework.Core.UIProjectConfigManager.Reload();
            UIManagerSettings.Reload();
            
            // 再刷新所有Tab
            _uiManagementTab?.OnEnable();
            _layerConfigTab?.OnEnable();
            _settingsTab?.OnEnable();
        }
        
        private void OnDisable()
        {
            _uiManagementTab?.OnDisable();
            _layerConfigTab?.OnDisable();
            _settingsTab?.OnDisable();
        }
    }
}
#endif

