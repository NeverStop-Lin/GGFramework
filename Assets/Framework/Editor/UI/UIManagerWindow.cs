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
        
        // Tab实例
        private UIManagementTab _uiManagementTab;
        private LayerConfigTab _layerConfigTab;
        private SettingsTab _settingsTab;
        
        // 刷新标记
        private bool _needRefresh = false;
        
        [MenuItem("Framework/UI Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<UIManagerWindow>("UI管理器");
            window.minSize = new Vector2(1000, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 检查配置索引是否存在
            if (!Core.FrameworkSettingsIndex.Exists())
            {
                Debug.Log("[UIManager] 配置索引不存在，创建索引文件");
                Core.FrameworkSettingsIndex.GetOrCreate();
            }
            
            // 检查UI管理器配置是否存在
            var settings = UIManagerSettings.Instance;
            if (settings == null)
            {
                Debug.Log("[UIManager] UI管理器配置不存在，显示欢迎窗口");
                Core.SettingsWelcomeWindow.ShowUIManagerWelcome();
                Close();
                return;
            }
            
            // 确保配置代码文件存在
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
            
            // 显示当前Tab（每个Tab自己管理滚动）
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

