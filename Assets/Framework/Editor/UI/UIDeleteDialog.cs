#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI删除对话框数据
    /// </summary>
    public class UIDeleteOptions
    {
        public bool DeletePrefab;
        public bool DeleteLogicScript;
        public bool DeleteBindingScript;
        public bool DeleteConfig;
        public bool Confirmed;
    }
    
    /// <summary>
    /// UI删除对话框
    /// 提供灵活的删除选项
    /// </summary>
    public class UIDeleteDialog : EditorWindow
    {
        private string _uiName;
        private bool _prefabExists;
        private bool _logicExists;
        private bool _bindingExists;
        private bool _configExists;
        
        private bool _deletePrefab = false;
        private bool _deleteLogic = true;
        private bool _deleteBinding = true;
        private bool _deleteConfig = true;
        private bool _shouldClose = false;
        
        private static UIDeleteOptions _result = null;
        
        /// <summary>
        /// 显示删除对话框
        /// </summary>
        public static UIDeleteOptions Show(string uiName, bool prefabExists, bool logicExists, bool bindingExists, bool configExists)
        {
            _result = null;
            
            var window = GetWindow<UIDeleteDialog>(true, "删除UI", true);
            window.minSize = new Vector2(450, 380);
            window.maxSize = new Vector2(450, 380);
            
            // 初始化数据
            window._uiName = uiName;
            window._prefabExists = prefabExists;
            window._logicExists = logicExists;
            window._bindingExists = bindingExists;
            window._configExists = configExists;
            
            // 默认选项：代码和配置都删除，预制体不删除
            window._deletePrefab = false;
            window._deleteLogic = logicExists;
            window._deleteBinding = bindingExists;
            window._deleteConfig = configExists;
            window._shouldClose = false;
            
            // 居中显示
            var mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            var windowRect = window.position;
            windowRect.x = mainWindowRect.x + (mainWindowRect.width - windowRect.width) * 0.5f;
            windowRect.y = mainWindowRect.y + (mainWindowRect.height - windowRect.height) * 0.5f;
            window.position = windowRect;
            
            window.ShowModal();
            
            return _result;
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // 标题
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField($"删除 {_uiName}", titleStyle);
            
            EditorGUILayout.Space(10);
            
            // 警告提示
            EditorGUILayout.HelpBox(
                "⚠️ 请选择要删除的内容，此操作不可撤销！",
                MessageType.Warning
            );
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("选择要删除的内容:", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);
            
            // 预制体选项
            GUI.enabled = _prefabExists;
            _deletePrefab = EditorGUILayout.ToggleLeft(
                _prefabExists ? "预制体 (Prefab)" : "预制体 (不存在)",
                _deletePrefab
            );
            GUI.enabled = true;
            
            EditorGUILayout.Space(3);
            
            // 逻辑脚本选项
            GUI.enabled = _logicExists;
            _deleteLogic = EditorGUILayout.ToggleLeft(
                _logicExists ? "逻辑脚本 (Logic.cs)" : "逻辑脚本 (不存在)",
                _deleteLogic
            );
            GUI.enabled = true;
            
            EditorGUILayout.Space(3);
            
            // 绑定脚本选项
            GUI.enabled = _bindingExists;
            _deleteBinding = EditorGUILayout.ToggleLeft(
                _bindingExists ? "绑定脚本 (Binding.cs)" : "绑定脚本 (不存在)",
                _deleteBinding
            );
            GUI.enabled = true;
            
            EditorGUILayout.Space(3);
            
            // 配置选项
            GUI.enabled = _configExists;
            _deleteConfig = EditorGUILayout.ToggleLeft(
                _configExists ? "UI配置表记录" : "UI配置表记录 (不存在)",
                _deleteConfig
            );
            GUI.enabled = true;
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(12);
            
            // 快捷按钮
            EditorGUILayout.LabelField("快捷选择:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Height(25)))
            {
                _deletePrefab = _prefabExists;
                _deleteLogic = _logicExists;
                _deleteBinding = _bindingExists;
                _deleteConfig = _configExists;
            }
            if (GUILayout.Button("全不选", GUILayout.Height(25)))
            {
                _deletePrefab = false;
                _deleteLogic = false;
                _deleteBinding = false;
                _deleteConfig = false;
            }
            if (GUILayout.Button("仅代码", GUILayout.Height(25)))
            {
                _deletePrefab = false;
                _deleteLogic = _logicExists;
                _deleteBinding = _bindingExists;
                _deleteConfig = false;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("确认删除", GUILayout.Width(140), GUILayout.Height(35)))
            {
                OnConfirm();
            }
            GUI.backgroundColor = oldColor;
            
            if (GUILayout.Button("取消", GUILayout.Width(140), GUILayout.Height(35)))
            {
                OnCancel();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(15);
            
            // 处理ESC键
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                OnCancel();
                Event.current.Use();
            }
            
            // 延迟关闭窗口
            if (_shouldClose)
            {
                Close();
            }
        }
        
        private void OnConfirm()
        {
            // 检查是否至少选择了一项
            if (!_deletePrefab && !_deleteLogic && !_deleteBinding && !_deleteConfig)
            {
                EditorUtility.DisplayDialog("提示", "请至少选择一项要删除的内容", "确定");
                return;
            }
            
            _shouldClose = true;
            
            _result = new UIDeleteOptions
            {
                DeletePrefab = _deletePrefab && _prefabExists,
                DeleteLogicScript = _deleteLogic && _logicExists,
                DeleteBindingScript = _deleteBinding && _bindingExists,
                DeleteConfig = _deleteConfig && _configExists,
                Confirmed = true
            };
        }
        
        private void OnCancel()
        {
            _shouldClose = true;
            
            _result = new UIDeleteOptions
            {
                Confirmed = false
            };
        }
    }
}
#endif

