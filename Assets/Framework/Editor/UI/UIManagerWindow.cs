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
            
            // 确保配置代码文件存在（自动生成）
            EnsureConfigCodeExists();
            
            // 确保UI模板存在
            EnsureUITemplateExists();
            
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
            
            // 确保默认创建路径已添加到Prefab目录列表
            if (!string.IsNullOrEmpty(settings.UIPrefabCreationDefaultPath))
            {
                if (!settings.PrefabDirectories.Contains(settings.UIPrefabCreationDefaultPath))
                {
                    settings.PrefabDirectories.Add(settings.UIPrefabCreationDefaultPath);
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
            }
        }
        
        /// <summary>
        /// 确保UI模板存在
        /// </summary>
        private void EnsureUITemplateExists()
        {
            UITemplateGenerator.EnsureTemplateExists();
        }
        
        /// <summary>
        /// 确保配置代码文件存在
        /// </summary>
        private void EnsureConfigCodeExists()
        {
            var settings = UIManagerSettings.Instance;
            if (settings == null) return;
            
            // 检查配置文件路径是否已设置
            if (string.IsNullOrEmpty(settings.ConfigCodeFilePath))
            {
                // 使用默认路径
                settings.ConfigCodeFilePath = "Assets/Game/Scripts/Generated/UIProjectConfigData.cs";
                settings.Save();
            }
            
            // 检查代码文件是否存在
            if (!System.IO.File.Exists(settings.ConfigCodeFilePath))
            {
                Debug.Log($"[UIManagerWindow] 配置代码文件不存在，将提示用户创建");
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
            
            try
            {
                // 显示当前Tab
                switch (_currentTab)
                {
                    case 0:
                        _uiManagementTab?.OnGUI();
                        break;
                    case 1:
                        _layerConfigTab?.OnGUI();
                        break;
                    case 2:
                        _settingsTab?.OnGUI();
                        break;
                }
            }
            finally
            {
                // 确保ScrollView总是正确结束
                EditorGUILayout.EndScrollView();
            }
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

