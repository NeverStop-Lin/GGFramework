#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI代码生成器
    /// 提供可视化界面生成UI代码
    /// </summary>
    public class UICodeGenerator : EditorWindow
    {
        #region 常量
        
        private const string PREF_NAMESPACE = "UIGenerator_Namespace";
        private const string PREF_OUTPUT_PATH = "UIGenerator_OutputPath";
        private const string PREF_PREFAB_PATH = "UIGenerator_PrefabPath";
        private const string DEFAULT_NAMESPACE = "Game.UI";
        private const string DEFAULT_OUTPUT_PATH = "Assets/Game/Scripts/UI";
        
        #endregion
        
        #region 字段
        
        // 选择的Prefab
        private List<GameObject> _selectedPrefabs = new List<GameObject>();
        
        // 配置
        private string _namespace = DEFAULT_NAMESPACE;
        private string _outputPath = DEFAULT_OUTPUT_PATH;
        private bool _autoUpdateManifest = true;
        private bool _openAfterGenerate = true;
        
        // UI
        private Vector2 _scrollPosition;
        private string _statusMessage = "";
        
        #endregion
        
        #region 菜单入口
        
        [MenuItem("Tools/UI工具/生成UI代码")]
        public static void ShowWindow()
        {
            var window = GetWindow<UICodeGenerator>("UI代码生成器");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }
        
        [MenuItem("Assets/生成UI代码", true)]
        private static bool ValidateGenerateFromAsset()
        {
            // 检查选中的是否是Prefab
            return Selection.activeGameObject != null &&
                   PrefabUtility.GetPrefabAssetType(Selection.activeGameObject) != PrefabAssetType.NotAPrefab;
        }
        
        [MenuItem("Assets/生成UI代码", false, 20)]
        private static void GenerateFromAsset()
        {
            var window = GetWindow<UICodeGenerator>("UI代码生成器");
            window.minSize = new Vector2(500, 600);
            
            // 添加选中的Prefab
            window._selectedPrefabs.Clear();
            foreach (var obj in Selection.gameObjects)
            {
                if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
                {
                    window._selectedPrefabs.Add(obj);
                }
            }
            
            window.Show();
        }
        
        #endregion
        
        #region Unity生命周期
        
        private void OnEnable()
        {
            LoadPreferences();
        }
        
        private void OnDisable()
        {
            SavePreferences();
        }
        
        #endregion
        
        #region GUI绘制
        
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawPrefabSelection();
            EditorGUILayout.Space(10);
            
            DrawOutputConfig();
            EditorGUILayout.Space(10);
            
            DrawPreview();
            EditorGUILayout.Space(10);
            
            DrawButtons();
            EditorGUILayout.Space(10);
            
            DrawStatus();
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制头部
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("UI代码生成器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "自动扫描UI Prefab，生成组件绑定代码和业务逻辑框架。\n" +
                "支持单个/批量生成，增量更新。",
                MessageType.Info
            );
        }
        
        /// <summary>
        /// 绘制Prefab选择区域
        /// </summary>
        private void DrawPrefabSelection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("1. 选择Prefab", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加Prefab", GUILayout.Width(100)))
            {
                AddPrefab();
            }
            if (GUILayout.Button("批量添加（目录）", GUILayout.Width(150)))
            {
                AddPrefabsFromDirectory();
            }
            if (GUILayout.Button("清空", GUILayout.Width(100)))
            {
                _selectedPrefabs.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 显示选中的Prefab列表
            if (_selectedPrefabs.Count == 0)
            {
                EditorGUILayout.HelpBox("未选择Prefab", MessageType.Warning);
            }
            else
            {
                for (int i = 0; i < _selectedPrefabs.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    _selectedPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                        $"Prefab {i + 1}:",
                        _selectedPrefabs[i],
                        typeof(GameObject),
                        false
                    );
                    
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        _selectedPrefabs.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制输出配置区域
        /// </summary>
        private void DrawOutputConfig()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("2. 输出配置", EditorStyles.boldLabel);
            
            // 命名空间
            _namespace = EditorGUILayout.TextField("命名空间:", _namespace);
            
            // 输出路径
            EditorGUILayout.BeginHorizontal();
            _outputPath = EditorGUILayout.TextField("代码输出路径:", _outputPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var path = EditorUtility.OpenFolderPanel("选择输出目录", _outputPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    // 转换为相对路径
                    if (path.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    _outputPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 选项
            _autoUpdateManifest = EditorGUILayout.Toggle("自动更新UIManifest", _autoUpdateManifest);
            _openAfterGenerate = EditorGUILayout.Toggle("生成后打开代码文件", _openAfterGenerate);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制预览区域
        /// </summary>
        private void DrawPreview()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("3. 生成预览", EditorStyles.boldLabel);
            
            if (_selectedPrefabs.Count == 0)
            {
                EditorGUILayout.HelpBox("请先选择Prefab", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"将生成 {_selectedPrefabs.Count * 2} 个文件:");
                
                foreach (var prefab in _selectedPrefabs)
                {
                    if (prefab != null)
                    {
                        var uiName = GetUIName(prefab.name);
                        EditorGUILayout.LabelField($"  ✓ {uiName}.cs");
                        EditorGUILayout.LabelField($"  ✓ {uiName}.Binding.cs");
                    }
                }
                
                if (_autoUpdateManifest)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("将更新 UIManifest.asset");
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制按钮区域
        /// </summary>
        private void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.FlexibleSpace();
            
            GUI.enabled = _selectedPrefabs.Count > 0;
            if (GUILayout.Button("生成代码", GUILayout.Width(150), GUILayout.Height(30)))
            {
                GenerateCode();
            }
            GUI.enabled = true;
            
            if (GUILayout.Button("取消", GUILayout.Width(100), GUILayout.Height(30)))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制状态信息
        /// </summary>
        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }
        }
        
        #endregion
        
        #region 功能方法
        
        /// <summary>
        /// 添加Prefab
        /// </summary>
        private void AddPrefab()
        {
            var path = EditorUtility.OpenFilePanel("选择UI Prefab", "Assets", "prefab");
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为相对路径
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && !_selectedPrefabs.Contains(prefab))
                {
                    _selectedPrefabs.Add(prefab);
                }
            }
        }
        
        /// <summary>
        /// 从目录批量添加Prefab
        /// </summary>
        private void AddPrefabsFromDirectory()
        {
            var path = EditorUtility.OpenFolderPanel("选择Prefab目录", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为相对路径
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                
                // 查找所有prefab文件
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    
                    if (prefab != null && !_selectedPrefabs.Contains(prefab))
                    {
                        _selectedPrefabs.Add(prefab);
                    }
                }
                
                _statusMessage = $"从目录添加了 {guids.Length} 个Prefab";
            }
        }
        
        /// <summary>
        /// 生成代码
        /// </summary>
        private void GenerateCode()
        {
            try
            {
                var successCount = 0;
                var errorCount = 0;
                var errorMessages = new List<string>();
                
                foreach (var prefab in _selectedPrefabs)
                {
                    if (prefab == null) continue;
                    
                    try
                    {
                        GenerateCodeForPrefab(prefab);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errorMessages.Add($"{prefab.name}: {ex.Message}");
                        Debug.LogError($"[UICodeGenerator] 生成失败: {prefab.name}\n{ex}");
                    }
                }
                
                // 显示结果
                if (errorCount == 0)
                {
                    _statusMessage = $"生成成功！共 {successCount} 个UI";
                    EditorUtility.DisplayDialog("生成完成", $"成功生成 {successCount} 个UI的代码", "确定");
                }
                else
                {
                    _statusMessage = $"部分成功：{successCount} 个成功，{errorCount} 个失败";
                    
                    var errorMsg = string.Join("\n", errorMessages);
                    EditorUtility.DisplayDialog("生成完成（有错误）",
                        $"成功: {successCount} 个\n失败: {errorCount} 个\n\n{errorMsg}",
                        "确定");
                }
                
                // 刷新资源
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                _statusMessage = $"生成失败: {ex.Message}";
                EditorUtility.DisplayDialog("生成失败", ex.Message, "确定");
                Debug.LogError($"[UICodeGenerator] {ex}");
            }
        }
        
        /// <summary>
        /// 为单个Prefab生成代码
        /// </summary>
        private void GenerateCodeForPrefab(GameObject prefab)
        {
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            var uiName = GetUIName(prefab.name);
            
            Debug.Log($"[UICodeGenerator] 开始生成: {uiName} <- {prefabPath}");
            
            // 扫描Prefab
            var scanResult = UIPrefabScanner.ScanPrefab(prefab);
            
            // 检查错误
            if (scanResult.HasErrors)
            {
                ShowErrorDialog(uiName, prefabPath, scanResult);
                throw new Exception($"Prefab扫描失败，发现 {scanResult.Errors.Count} 个错误");
            }
            
            // 生成资源路径（从Resources开始）
            var resourcePath = GetResourcePath(prefabPath);
            
            // 生成代码
            var bindingCode = UICodeTemplate.GenerateBindingCode(
                uiName,
                _namespace,
                scanResult.Components,
                prefabPath
            );
            
            var logicCode = UICodeTemplate.GenerateLogicCode(
                uiName,
                _namespace,
                scanResult.Components,
                resourcePath
            );
            
            // 确保输出目录存在
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }
            
            // 写入Binding文件（总是覆盖）
            var bindingFilePath = Path.Combine(_outputPath, $"{uiName}.Binding.cs");
            File.WriteAllText(bindingFilePath, bindingCode);
            Debug.Log($"[UICodeGenerator] 生成Binding: {bindingFilePath}");
            
            // 写入Logic文件（仅首次生成）
            var logicFilePath = Path.Combine(_outputPath, $"{uiName}.cs");
            if (!File.Exists(logicFilePath))
            {
                File.WriteAllText(logicFilePath, logicCode);
                Debug.Log($"[UICodeGenerator] 生成Logic: {logicFilePath}");
            }
            else
            {
                Debug.Log($"[UICodeGenerator] Logic文件已存在，跳过: {logicFilePath}");
            }
            
            // 更新UIManifest
            if (_autoUpdateManifest)
            {
                UpdateUIManifest(uiName, resourcePath);
            }
            
            // 打开文件
            if (_openAfterGenerate && _selectedPrefabs.Count == 1)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(bindingFilePath);
                AssetDatabase.OpenAsset(asset);
            }
        }
        
        /// <summary>
        /// 显示错误对话框
        /// </summary>
        private void ShowErrorDialog(string uiName, string prefabPath, UIPrefabScanner.ScanResult scanResult)
        {
            var message = $"{uiName}.prefab 存在以下问题:\n\n";
            
            if (scanResult.Errors.Count > 0)
            {
                message += $"❌ 错误 ({scanResult.Errors.Count}个):\n\n";
                for (int i = 0; i < scanResult.Errors.Count; i++)
                {
                    message += $"  {i + 1}. {scanResult.Errors[i]}\n\n";
                }
            }
            
            if (scanResult.Warnings.Count > 0)
            {
                message += $"⚠️ 警告 ({scanResult.Warnings.Count}个):\n\n";
                for (int i = 0; i < scanResult.Warnings.Count; i++)
                {
                    message += $"  {i + 1}. {scanResult.Warnings[i]}\n\n";
                }
            }
            
            if (EditorUtility.DisplayDialog("生成失败", message, "打开Prefab", "关闭"))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
        }
        
        /// <summary>
        /// 更新UIManifest
        /// </summary>
        private void UpdateUIManifest(string uiName, string resourcePath)
        {
            const string manifestPath = "Assets/Resources/Config/UIManifest.asset";
            
            var manifest = AssetDatabase.LoadAssetAtPath<Framework.Core.UIManifest>(manifestPath);
            
            if (manifest == null)
            {
                // 创建目录
                var dir = Path.GetDirectoryName(manifestPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                // 创建新的Manifest
                manifest = ScriptableObject.CreateInstance<Framework.Core.UIManifest>();
                AssetDatabase.CreateAsset(manifest, manifestPath);
                Debug.Log($"[UICodeGenerator] 创建UIManifest: {manifestPath}");
            }
            
            // 添加或更新配置
            var config = new Framework.Core.UIConfig
            {
                ResourcePath = resourcePath,
                UIType = Framework.Core.UIType.Main,
                CacheStrategy = Framework.Core.UICacheStrategy.AlwaysCache,
                Preload = false
            };
            
            manifest.AddOrUpdateConfig(uiName, config);
            
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[UICodeGenerator] 更新UIManifest: {uiName}");
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 获取UI类名
        /// </summary>
        private string GetUIName(string prefabName)
        {
            var name = prefabName.Replace(".prefab", "");
            
            // 如果不以UI结尾，添加UI后缀
            if (!name.EndsWith("UI"))
            {
                name += "UI";
            }
            
            return name;
        }
        
        /// <summary>
        /// 获取资源路径（相对于Resources）
        /// </summary>
        private string GetResourcePath(string assetPath)
        {
            // Assets/Resources/UI/MainMenu.prefab -> UI/MainMenu
            var index = assetPath.IndexOf("Resources/");
            if (index >= 0)
            {
                var path = assetPath.Substring(index + "Resources/".Length);
                path = path.Replace(".prefab", "");
                return path;
            }
            
            // 如果不在Resources下，返回相对路径
            return assetPath.Replace("Assets/", "").Replace(".prefab", "");
        }
        
        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadPreferences()
        {
            _namespace = EditorPrefs.GetString(PREF_NAMESPACE, DEFAULT_NAMESPACE);
            _outputPath = EditorPrefs.GetString(PREF_OUTPUT_PATH, DEFAULT_OUTPUT_PATH);
        }
        
        /// <summary>
        /// 保存配置
        /// </summary>
        private void SavePreferences()
        {
            EditorPrefs.SetString(PREF_NAMESPACE, _namespace);
            EditorPrefs.SetString(PREF_OUTPUT_PATH, _outputPath);
        }
        
        #endregion
    }
}
#endif
