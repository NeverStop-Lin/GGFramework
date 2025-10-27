#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Framework.Core;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI创建对话框
    /// 提供完整的UI创建配置选项
    /// </summary>
    public class UICreateDialog : EditorWindow
    {
        private string _uiName = "";
        private string _saveDirectory = "";
        private int _layerIndex = 0;
        private int _templateIndex = 0;
        private bool _shouldClose = false;
        
        private string[] _layerNames;
        private string[] _templateNames;
        private string[] _templatePaths;
        
        private static UICreateDialogData _result = null;
        
        /// <summary>
        /// 显示对话框
        /// </summary>
        public static UICreateDialogData Show(string defaultName, string defaultDirectory, List<UILayerDefinition> layers)
        {
            _result = null;
            
            var window = GetWindow<UICreateDialog>(true, "创建UI预制体", true);
            window.minSize = new Vector2(500, 280);
            window.maxSize = new Vector2(500, 280);
            
            // 初始化数据
            window._uiName = defaultName;
            window._saveDirectory = defaultDirectory;
            window._layerIndex = 0;
            window._templateIndex = 0;
            window._shouldClose = false;
            
            // 初始化层级列表
            if (layers != null && layers.Count > 0)
            {
                window._layerNames = layers.Select(l => l.LayerName).ToArray();
            }
            else
            {
                window._layerNames = new[] { "Main" };
            }
            
            // 扫描模板目录
            window.ScanTemplates();
            
            // 居中显示
            var mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            var windowRect = window.position;
            windowRect.x = mainWindowRect.x + (mainWindowRect.width - windowRect.width) * 0.5f;
            windowRect.y = mainWindowRect.y + (mainWindowRect.height - windowRect.height) * 0.5f;
            window.position = windowRect;
            
            window.ShowModal();
            
            return _result;
        }
        
        /// <summary>
        /// 扫描模板目录
        /// </summary>
        private void ScanTemplates()
        {
            const string TEMPLATE_DIR = "Assets/Framework/Editor/UI/Template";
            
            var templateList = new System.Collections.Generic.List<string>();
            var templateNameList = new System.Collections.Generic.List<string>();
            
            if (System.IO.Directory.Exists(TEMPLATE_DIR))
            {
                // 查找所有.prefab文件
                var prefabFiles = System.IO.Directory.GetFiles(TEMPLATE_DIR, "*.prefab", System.IO.SearchOption.TopDirectoryOnly);
                
                foreach (var prefabFile in prefabFiles)
                {
                    var relativePath = prefabFile.Replace("\\", "/");
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(relativePath);
                    
                    // 去掉 "Template" 后缀显示
                    var displayName = fileName.EndsWith("Template") 
                        ? fileName.Substring(0, fileName.Length - "Template".Length) 
                        : fileName;
                    
                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName = fileName;
                    }
                    
                    templateList.Add(relativePath);
                    templateNameList.Add(displayName);
                }
            }
            
            // 如果没有找到模板，添加提示
            if (templateList.Count == 0)
            {
                templateList.Add("");
                templateNameList.Add("无可用模板");
            }
            
            _templatePaths = templateList.ToArray();
            _templateNames = templateNameList.ToArray();
        }
        
        private void OnGUI()
        {
            var oldColor = GUI.backgroundColor;
            
            EditorGUILayout.Space(15);
            
            // 标题
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("配置新UI预制体", titleStyle);
            
            EditorGUILayout.Space(15);
            
            EditorGUILayout.BeginVertical("box");
            
            // UI名称
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UI名称:", GUILayout.Width(100));
            GUI.SetNextControlName("UINameField");
            _uiName = EditorGUILayout.TextField(_uiName);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 保存目录
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("保存目录:", GUILayout.Width(100));
            
            GUI.backgroundColor = new Color(0.9f, 0.9f, 1f);
            EditorGUILayout.TextField(_saveDirectory);
            GUI.backgroundColor = oldColor;
            
            if (GUILayout.Button("浏览...", GUILayout.Width(70)))
            {
                BrowseSaveDirectory();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // UI层级
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UI层级:", GUILayout.Width(100));
            _layerIndex = EditorGUILayout.Popup(_layerIndex, _layerNames);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 模板选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UI模板:", GUILayout.Width(100));
            _templateIndex = EditorGUILayout.Popup(_templateIndex, _templateNames);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 提示信息
            EditorGUILayout.HelpBox(
                "💡 提示：\n" +
                "• UI名称只能包含字母、数字、下划线，且不能以数字开头\n" +
                "• 保存路径必须在Assets目录下\n" +
                "• 创建后会自动应用项目的Canvas Scaler配置",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            // 按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("创建", GUILayout.Width(120), GUILayout.Height(30)))
            {
                OnConfirm();
            }
            GUI.backgroundColor = oldColor;
            
            if (GUILayout.Button("取消", GUILayout.Width(120), GUILayout.Height(30)))
            {
                OnCancel();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 自动聚焦到名称输入框
            if (Event.current.type == EventType.Layout)
            {
                EditorGUI.FocusTextInControl("UINameField");
            }
            
            // 处理回车键
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                OnConfirm();
                Event.current.Use();
            }
            
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
            
            // 恢复背景色
            GUI.backgroundColor = oldColor;
        }
        
        private void BrowseSaveDirectory()
        {
            var currentDir = string.IsNullOrEmpty(_saveDirectory) 
                ? "Assets" 
                : _saveDirectory;
            
            var path = EditorUtility.OpenFolderPanel("选择保存目录", currentDir, "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                _saveDirectory = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        
        private void OnConfirm()
        {
            _shouldClose = true;
            
            _result = new UICreateDialogData
            {
                UIName = _uiName.Trim(),
                SaveDirectory = _saveDirectory,
                LayerName = _layerIndex >= 0 && _layerIndex < _layerNames.Length 
                    ? _layerNames[_layerIndex] 
                    : "Main",
                TemplatePath = _templateIndex >= 0 && _templateIndex < _templatePaths.Length 
                    ? _templatePaths[_templateIndex] 
                    : "",
                Confirmed = true
            };
        }
        
        private void OnCancel()
        {
            _shouldClose = true;
            
            _result = new UICreateDialogData
            {
                Confirmed = false
            };
        }
    }
}
#endif

