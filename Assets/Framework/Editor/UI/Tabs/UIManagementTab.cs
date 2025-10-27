#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Framework.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI管理Tab
    /// 整合UI配置和代码生成功能
    /// </summary>
    public class UIManagementTab
    {
        private UIProjectConfig _config;
        private UIManagerSettings _settings;
        private List<UIPrefabInfo> _prefabInfos = new List<UIPrefabInfo>();
        private HashSet<int> _selectedIndices = new HashSet<int>();
        
        private Vector2 _scrollPosition;
        private string _statusMessage = "";
        
        // 生成设置
        private string _namespace = "Game.UI";
        private string _logicOutputPath = "Assets/Game/Scripts/UI";
        private string _bindingOutputPath = "Assets/Game/Scripts/UI/Generated";
        
        public void OnEnable()
        {
            LoadConfig();
            LoadSettings();
            ScanPrefabs();
        }
        
        public void OnGUI()
        {
            EditorGUILayout.LabelField("UI管理", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (_config == null)
            {
                EditorGUILayout.HelpBox("未找到配置文件", MessageType.Warning);
                return;
            }
            
            DrawDirectoryManagement();
            EditorGUILayout.Space();
            DrawToolbar();
            EditorGUILayout.Space();
            DrawPrefabList();
            EditorGUILayout.Space();
            DrawBatchActions();
            EditorGUILayout.Space();
            DrawStatus();
        }
        
        public void OnDisable()
        {
        }
        
        private void LoadConfig()
        {
            _config = UIProjectConfigEditorHelper.GetConfig();
        }
        
        private void LoadSettings()
        {
            UIManagerSettings.Reload();
            _settings = UIManagerSettings.Instance;
            
            if (_settings != null)
            {
                _namespace = _settings.DefaultNamespace;
                _logicOutputPath = _settings.LogicScriptOutputPath;
                _bindingOutputPath = _settings.BindingScriptOutputPath;
            }
        }
        
        /// <summary>
        /// 扫描所有Prefab（从目录 + 配置表）
        /// </summary>
        private void ScanPrefabs()
        {
            _prefabInfos.Clear();
            _selectedIndices.Clear();
            
            var processedUINames = new HashSet<string>();
            
            // 1. 从目录扫描Prefab
            if (_settings != null && _settings.PrefabDirectories != null)
            {
                foreach (var directory in _settings.PrefabDirectories)
                {
                    if (!string.IsNullOrEmpty(directory) && AssetDatabase.IsValidFolder(directory))
                    {
                        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { directory });
                        
                        foreach (var guid in guids)
                        {
                            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                            
                            if (prefab != null)
                            {
                                var info = CreatePrefabInfo(prefab);
                                _prefabInfos.Add(info);
                                processedUINames.Add(info.UIName);
                            }
                        }
                    }
                }
            }
            
            // 2. 从配置表获取UI（没有对应Prefab的会显示为异常）
            if (_config != null && _config.UIConfigs != null)
            {
                foreach (var uiConfig in _config.UIConfigs)
                {
                    if (!processedUINames.Contains(uiConfig.UIName))
                    {
                        // 配置存在但Prefab不存在
                        var info = CreatePrefabInfoFromConfig(uiConfig);
                        _prefabInfos.Add(info);
                    }
                }
            }
        }
        
        /// <summary>
        /// 创建Prefab信息（从Prefab）
        /// </summary>
        private UIPrefabInfo CreatePrefabInfo(GameObject prefab)
        {
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            var uiName = GetUIName(prefab.name);
            
            var info = new UIPrefabInfo
            {
                Prefab = prefab,
                PrefabPath = prefabPath,
                UIName = uiName
            };
            
            // 检查状态
            CheckPrefabStatus(info);
            
            return info;
        }
        
        /// <summary>
        /// 从配置创建Prefab信息（Prefab不存在的情况）
        /// </summary>
        private UIPrefabInfo CreatePrefabInfoFromConfig(UIInstanceConfig config)
        {
            var info = new UIPrefabInfo
            {
                Prefab = null,  // Prefab不存在
                PrefabPath = config.ResourcePath,
                UIName = config.UIName,
                LayerName = config.LayerName,
                InstanceStrategy = config.InstanceStrategy
            };
            
            // 检查状态
            CheckPrefabStatus(info);
            
            // 标记为异常（Prefab丢失）
            if (info.Status == UIPrefabStatus.Created || info.Status == UIPrefabStatus.Partial)
            {
                info.Status = UIPrefabStatus.PrefabMissing;
            }
            
            return info;
        }
        
        /// <summary>
        /// 检查Prefab状态
        /// </summary>
        private void CheckPrefabStatus(UIPrefabInfo info)
        {
            // 检查代码文件
            var logicPath = Path.Combine(_logicOutputPath, $"{info.UIName}.cs");
            var bindingPath = Path.Combine(_bindingOutputPath, $"{info.UIName}.Binding.cs");
            
            info.LogicFileExists = File.Exists(logicPath);
            info.BindingFileExists = File.Exists(bindingPath);
            info.LogicFilePath = logicPath;
            info.BindingFilePath = bindingPath;
            
            // 检查配置
            var uiConfig = _config.GetUIConfig(info.UIName);
            info.ConfigExists = uiConfig != null;
            
            if (uiConfig != null)
            {
                info.LayerName = uiConfig.LayerName;
                info.InstanceStrategy = uiConfig.InstanceStrategy;
            }
            else
            {
                // 使用默认层级（第一个）
                info.LayerName = _config.LayerDefinitions.Count > 0 
                    ? _config.LayerDefinitions[0].LayerName 
                    : "Main";
                info.InstanceStrategy = UIInstanceStrategy.Singleton;
            }
            
            // 判断状态
            if (info.Prefab == null)
            {
                // Prefab不存在但配置存在
                info.Status = UIPrefabStatus.PrefabMissing;
            }
            else if (info.LogicFileExists && info.BindingFileExists && info.ConfigExists)
            {
                info.Status = UIPrefabStatus.Created;
            }
            else if (info.LogicFileExists || info.BindingFileExists || info.ConfigExists)
            {
                info.Status = UIPrefabStatus.Partial;
            }
            else
            {
                info.Status = UIPrefabStatus.NotCreated;
            }
        }
        
        private void DrawDirectoryManagement()
        {
            EditorGUILayout.LabelField("Prefab目录配置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            if (_settings != null && _settings.PrefabDirectories != null && _settings.PrefabDirectories.Count > 0)
            {
                for (int i = 0; i < _settings.PrefabDirectories.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(25));
                    EditorGUILayout.LabelField(_settings.PrefabDirectories[i]);
                    
                    if (GUILayout.Button("删除", GUILayout.Width(60)))
                    {
                        EditorGUILayout.EndHorizontal();
                        _settings.PrefabDirectories.RemoveAt(i);
                        _settings.Save();
                        ScanPrefabs(); // 重新扫描
                        break;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("暂无Prefab目录，请添加", MessageType.Info);
            }
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("添加目录", GUILayout.Width(100)))
            {
                AddDirectory();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField($"共 {_prefabInfos.Count} 个UI Prefab", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("刷新列表", GUILayout.Width(80)))
            {
                ScanPrefabs();
            }
            
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                SelectAll();
            }
            
            if (GUILayout.Button("反选", GUILayout.Width(60)))
            {
                InvertSelection();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void AddDirectory()
        {
            var path = EditorUtility.OpenFolderPanel("选择Prefab目录", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                
                if (_settings != null)
                {
                    if (!_settings.PrefabDirectories.Contains(path))
                    {
                        _settings.PrefabDirectories.Add(path);
                        _settings.Save();
                        ScanPrefabs(); // 重新扫描
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "该目录已在列表中", "确定");
                    }
                }
            }
        }
        
        private void DrawPrefabList()
        {
            if (_prefabInfos.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无UI Prefab\n请在设置Tab中添加Prefab目录", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical("box");
            
            // 表头
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("☑", GUILayout.Width(30));
            EditorGUILayout.LabelField("Prefab", GUILayout.Width(150));
            EditorGUILayout.LabelField("状态", GUILayout.Width(100));
            EditorGUILayout.LabelField("Logic.cs", GUILayout.Width(100));
            EditorGUILayout.LabelField("Binding.cs", GUILayout.Width(100));
            EditorGUILayout.LabelField("层级", GUILayout.Width(100));
            EditorGUILayout.LabelField("实例策略", GUILayout.Width(100));
            EditorGUILayout.LabelField("操作", GUILayout.Width(100));
            EditorGUILayout.LabelField("删除", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(400));
            
            for (int i = 0; i < _prefabInfos.Count; i++)
            {
                DrawPrefabRow(i, _prefabInfos[i]);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPrefabRow(int index, UIPrefabInfo info)
        {
            EditorGUILayout.BeginHorizontal("box");
            
            // 复选框
            var isSelected = _selectedIndices.Contains(index);
            var newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(30));
            if (newSelected != isSelected)
            {
                if (newSelected)
                {
                    _selectedIndices.Add(index);
                }
                else
                {
                    _selectedIndices.Remove(index);
                }
            }
            
            // Prefab引用
            if (info.Prefab != null)
            {
                EditorGUILayout.ObjectField(info.Prefab, typeof(GameObject), false, GUILayout.Width(150));
            }
            else
            {
                EditorGUILayout.LabelField(info.UIName, EditorStyles.helpBox, GUILayout.Width(150));
            }
            
            // 状态
            DrawStatus(info, 100);
            
            // Logic.cs
            DrawScriptFile(info.LogicFilePath, info.LogicFileExists, 100);
            
            // Binding.cs
            DrawScriptFile(info.BindingFilePath, info.BindingFileExists, 100);
            
            // 层级（下拉选择）
            DrawLayerSelector(info, 100);
            
            // 实例策略（下拉选择）
            DrawInstanceStrategySelector(info, 100);
            
            // 操作按钮
            DrawActionButton(info, 100);
            
            // 删除按钮
            if (GUILayout.Button("删除", GUILayout.Width(60)))
            {
                DeleteUI(info);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStatus(UIPrefabInfo info, int width)
        {
            string statusText;
            Color statusColor;
            
            switch (info.Status)
            {
                case UIPrefabStatus.Created:
                    statusText = "✓ 已创建";
                    statusColor = Color.green;
                    break;
                case UIPrefabStatus.Partial:
                    statusText = "⚠ 不完整";
                    statusColor = Color.yellow;
                    break;
                case UIPrefabStatus.PrefabMissing:
                    statusText = "❌ Prefab丢失";
                    statusColor = Color.red;
                    break;
                default:
                    statusText = "✗ 未创建";
                    statusColor = Color.gray;
                    break;
            }
            
            var oldColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusText, GUILayout.Width(width));
            GUI.color = oldColor;
        }
        
        private void DrawScriptFile(string filePath, bool exists, int width)
        {
            if (exists)
            {
                var fileName = Path.GetFileName(filePath);
                var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);
                EditorGUILayout.ObjectField(asset, typeof(MonoScript), false, GUILayout.Width(width));
            }
            else
            {
                EditorGUILayout.LabelField("-", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(width));
            }
        }
        
        private void DrawLayerSelector(UIPrefabInfo info, int width)
        {
            if (_config.LayerDefinitions.Count == 0)
            {
                EditorGUILayout.LabelField("-", GUILayout.Width(width));
                return;
            }
            
            var layerNames = _config.LayerDefinitions.Select(l => l.LayerName).ToArray();
            var currentIndex = System.Array.IndexOf(layerNames, info.LayerName);
            if (currentIndex < 0) currentIndex = 0;
            
            var newIndex = EditorGUILayout.Popup(currentIndex, layerNames, GUILayout.Width(width));
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < layerNames.Length)
            {
                info.LayerName = layerNames[newIndex];
            }
        }
        
        private void DrawInstanceStrategySelector(UIPrefabInfo info, int width)
        {
            var strategyNames = new[] { "单例", "多实例" };
            var currentIndex = (int)info.InstanceStrategy;
            
            var oldColor = GUI.backgroundColor;
            // 单例显示绿色，多实例显示蓝色
            GUI.backgroundColor = currentIndex == 0 ? new Color(0.8f, 1f, 0.8f) : new Color(0.8f, 0.9f, 1f);
            
            var newIndex = EditorGUILayout.Popup(currentIndex, strategyNames, GUILayout.Width(width));
            if (newIndex != currentIndex)
            {
                info.InstanceStrategy = (UIInstanceStrategy)newIndex;
            }
            
            GUI.backgroundColor = oldColor;
        }
        
        private void DrawActionButton(UIPrefabInfo info, int width)
        {
            string buttonText;
            Color buttonColor;
            
            switch (info.Status)
            {
                case UIPrefabStatus.Created:
                    buttonText = "更新";
                    buttonColor = new Color(0.5f, 0.8f, 1f);
                    break;
                default:
                    buttonText = "创建";
                    buttonColor = new Color(0.5f, 1f, 0.5f);
                    break;
            }
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = buttonColor;
            
            if (GUILayout.Button(buttonText, GUILayout.Width(width)))
            {
                GenerateOrUpdateUI(info);
            }
            
            GUI.backgroundColor = oldColor;
        }
        
        private void DrawBatchActions()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField($"已选择: {_selectedIndices.Count} 个", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            GUI.enabled = _selectedIndices.Count > 0;
            
            if (GUILayout.Button("批量创建/更新", GUILayout.Width(120)))
            {
                BatchCreateOrUpdate();
            }
            
            if (GUILayout.Button("批量修复Canvas", GUILayout.Width(120)))
            {
                BatchFixCanvasScaler();
            }
            
            if (GUILayout.Button("批量删除", GUILayout.Width(100)))
            {
                BatchDelete();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }
        }
        
        /// <summary>
        /// 生成或更新UI
        /// </summary>
        private void GenerateOrUpdateUI(UIPrefabInfo info)
        {
            try
            {
                var prefabPath = AssetDatabase.GetAssetPath(info.Prefab);
                
                // 步骤1: 检查并修复Canvas Scaler配置
                if (!CanvasScalerFixer.CheckCanvasScaler(info.Prefab, out string errorMessage))
                {
                    Debug.LogWarning($"[UI代码生成] {info.UIName} Canvas Scaler配置不一致，正在修复...\n{errorMessage}");
                    
                    if (CanvasScalerFixer.FixCanvasScaler(info.Prefab))
                    {
                        Debug.Log($"[UI代码生成] {info.UIName} Canvas Scaler配置已修复");
                        // 重新加载Prefab
                        AssetDatabase.Refresh();
                        info.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    }
                    else
                    {
                        Debug.LogWarning($"[UI代码生成] {info.UIName} Canvas Scaler配置修复失败");
                    }
                }
                
                // 步骤2: 扫描Prefab
                var scanResult = UIPrefabScanner.ScanPrefab(info.Prefab);
                
                if (scanResult.HasErrors)
                {
                    var errorMsg = string.Join("\n", scanResult.Errors);
                    EditorUtility.DisplayDialog("扫描失败", $"{info.UIName} Prefab扫描失败：\n\n{errorMsg}", "确定");
                    return;
                }
                
                // 生成资源路径
                var resourcePath = GetResourcePath(prefabPath);
                
                // 生成代码
                var bindingCode = UICodeTemplate.GenerateBindingCode(info.UIName, _namespace, scanResult.Components, prefabPath);
                var logicCode = UICodeTemplate.GenerateLogicCode(info.UIName, _namespace, scanResult.Components, resourcePath);
                
                // 确保目录存在
                if (!Directory.Exists(_logicOutputPath))
                {
                    Directory.CreateDirectory(_logicOutputPath);
                }
                
                if (!Directory.Exists(_bindingOutputPath))
                {
                    Directory.CreateDirectory(_bindingOutputPath);
                }
                
                // 写入Binding文件（对比后更新）
                if (File.Exists(info.BindingFilePath))
                {
                    var existingBindingCode = File.ReadAllText(info.BindingFilePath);
                    if (existingBindingCode != bindingCode)
                    {
                        File.WriteAllText(info.BindingFilePath, bindingCode);
                        Debug.Log($"[UI代码生成] {info.UIName} Binding 文件已更新");
                    }
                }
                else
                {
                    File.WriteAllText(info.BindingFilePath, bindingCode);
                    Debug.Log($"[UI代码生成] {info.UIName} Binding 文件已创建");
                }
                
                // 写入Logic文件
                if (!File.Exists(info.LogicFilePath))
                {
                    // 首次生成：创建完整文件
                    File.WriteAllText(info.LogicFilePath, logicCode);
                    Debug.Log($"[UI代码生成] {info.UIName} Logic 文件已创建");
                }
                else
                {
                    // 已存在：增量更新（添加缺失的事件处理方法）
                    var existingCode = File.ReadAllText(info.LogicFilePath);
                    var updatedCode = UICodeTemplate.AppendMissingEventHandlers(existingCode, scanResult.Components);
                    
                    // 只有在有变化时才写入
                    if (updatedCode != existingCode)
                    {
                        File.WriteAllText(info.LogicFilePath, updatedCode);
                        Debug.Log($"[UI代码生成] {info.UIName} Logic 文件已添加缺失的事件处理方法");
                    }
                }
                
                // 刷新资源
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                
                // 更新配置
                UpdateUIConfig(info);
                
                // 等待编译并绑定脚本
                WaitForCompilationAndAttach(info.Prefab, info.UIName, _namespace);
                
                // 刷新状态
                CheckPrefabStatus(info);
                
                _statusMessage = $"{info.UIName} 生成成功";
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"{info.UIName} 生成失败: {ex.Message}";
                Debug.LogError($"[UIManagement] {_statusMessage}");
            }
        }
        
        /// <summary>
        /// 批量创建/更新
        /// </summary>
        private void BatchCreateOrUpdate()
        {
            var selectedInfos = _selectedIndices.Select(i => _prefabInfos[i]).ToList();
            
            if (!EditorUtility.DisplayDialog(
                "确认批量操作",
                $"将为以下 {selectedInfos.Count} 个UI生成/更新代码：\n\n" +
                string.Join("\n", selectedInfos.Select(i => $"• {i.UIName}")),
                "确定",
                "取消"))
            {
                return;
            }
            
            var successCount = 0;
            var errorCount = 0;
            
            foreach (var info in selectedInfos)
            {
                try
                {
                    GenerateOrUpdateUI(info);
                    successCount++;
                }
                catch (System.Exception ex)
                {
                    errorCount++;
                    Debug.LogError($"[UIManagement] {info.UIName} 失败: {ex.Message}");
                }
            }
            
            _statusMessage = $"批量操作完成：成功 {successCount} 个，失败 {errorCount} 个";
            EditorUtility.DisplayDialog("批量操作完成", _statusMessage, "确定");
        }
        
        /// <summary>
        /// 更新UI配置
        /// </summary>
        private void UpdateUIConfig(UIPrefabInfo info)
        {
            if (_config == null) return;
            
            var resourcePath = GetResourcePath(info.PrefabPath);
            
            var uiConfig = new UIInstanceConfig
            {
                UIName = info.UIName,
                ResourcePath = resourcePath,
                LayerName = info.LayerName,
                CacheStrategy = UICacheStrategy.AlwaysCache,
                Preload = false,
                UseMask = false,
                InstanceStrategy = info.InstanceStrategy
            };
            
            _config.AddOrUpdateUIConfig(uiConfig);
            
            // 保存配置（触发代码生成）
            UIProjectConfigEditorHelper.SaveConfig(_config);
        }
        
        /// <summary>
        /// 删除UI
        /// </summary>
        private void DeleteUI(UIPrefabInfo info)
        {
            var choice = EditorUtility.DisplayDialogComplex(
                "删除确认",
                $"如何删除 {info.UIName}？\n\n" +
                $"逻辑脚本: {(info.LogicFileExists ? "✓" : "✗")}\n" +
                $"绑定脚本: {(info.BindingFileExists ? "✓" : "✗")}\n" +
                $"配置: {(info.ConfigExists ? "✓" : "✗")}",
                "删除文件+配置",
                "取消",
                "仅删除配置"
            );
            
            if (choice == 1) // 取消
            {
                return;
            }
            
            try
            {
                if (choice == 0) // 删除文件+配置
                {
                    // 删除文件
                    if (info.LogicFileExists && File.Exists(info.LogicFilePath))
                    {
                        File.Delete(info.LogicFilePath);
                        File.Delete(info.LogicFilePath + ".meta");
                    }
                    
                    if (info.BindingFileExists && File.Exists(info.BindingFilePath))
                    {
                        File.Delete(info.BindingFilePath);
                        File.Delete(info.BindingFilePath + ".meta");
                    }
                    
                    AssetDatabase.Refresh();
                }
                
                // 删除配置（choice == 0 或 choice == 2）
                if (info.ConfigExists)
                {
                    _config.RemoveUIConfig(info.UIName);
                    
                    // 保存配置（触发代码生成）
                    UIProjectConfigEditorHelper.SaveConfig(_config);
                }
                
                // 刷新列表
                ScanPrefabs();
                
                _statusMessage = $"已删除 {info.UIName}";
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"删除失败: {ex.Message}";
                EditorUtility.DisplayDialog("删除失败", ex.Message, "确定");
            }
        }
        
        /// <summary>
        /// 批量修复Canvas Scaler
        /// </summary>
        private void BatchFixCanvasScaler()
        {
            var selectedInfos = _selectedIndices.Select(i => _prefabInfos[i]).Where(info => info.Prefab != null).ToList();
            
            if (selectedInfos.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有可修复的Prefab（Prefab为空或丢失）", "确定");
                return;
            }
            
            if (!EditorUtility.DisplayDialog(
                "确认批量修复Canvas Scaler",
                $"将为以下 {selectedInfos.Count} 个UI修复Canvas Scaler配置：\n\n" +
                string.Join("\n", selectedInfos.Select(i => $"• {i.UIName}")),
                "确定",
                "取消"))
            {
                return;
            }
            
            var prefabs = selectedInfos.Select(i => i.Prefab).ToArray();
            var successCount = CanvasScalerFixer.BatchFixCanvasScaler(prefabs, showProgress: true);
            
            _statusMessage = $"批量修复完成：成功 {successCount}/{selectedInfos.Count} 个";
            EditorUtility.DisplayDialog("批量修复完成", _statusMessage, "确定");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// 批量删除
        /// </summary>
        private void BatchDelete()
        {
            var selectedInfos = _selectedIndices.Select(i => _prefabInfos[i]).ToList();
            
            var choice = EditorUtility.DisplayDialogComplex(
                "批量删除确认",
                $"如何删除以下 {selectedInfos.Count} 个UI？\n\n" +
                string.Join("\n", selectedInfos.Select(i => $"• {i.UIName}")),
                "删除文件+配置",
                "取消",
                "仅删除配置"
            );
            
            if (choice == 1) // 取消
            {
                return;
            }
            
            var successCount = 0;
            var errorCount = 0;
            
            foreach (var info in selectedInfos)
            {
                try
                {
                    if (choice == 0) // 删除文件+配置
                    {
                        if (info.LogicFileExists && File.Exists(info.LogicFilePath))
                        {
                            File.Delete(info.LogicFilePath);
                            File.Delete(info.LogicFilePath + ".meta");
                        }
                        
                        if (info.BindingFileExists && File.Exists(info.BindingFilePath))
                        {
                            File.Delete(info.BindingFilePath);
                            File.Delete(info.BindingFilePath + ".meta");
                        }
                    }
                    
                    // 删除配置
                    if (info.ConfigExists)
                    {
                        _config.RemoveUIConfig(info.UIName);
                    }
                    
                    successCount++;
                }
                catch (System.Exception ex)
                {
                    errorCount++;
                    Debug.LogError($"[UIManagement] 删除 {info.UIName} 失败: {ex.Message}");
                }
            }
            
            // 保存配置（触发代码生成）
            UIProjectConfigEditorHelper.SaveConfig(_config);
            
            AssetDatabase.Refresh();
            
            // 刷新列表
            ScanPrefabs();
            _selectedIndices.Clear();
            
            _statusMessage = $"批量删除完成：成功 {successCount} 个，失败 {errorCount} 个";
            EditorUtility.DisplayDialog("批量删除完成", _statusMessage, "确定");
        }
        
        /// <summary>
        /// 等待编译完成并绑定脚本
        /// </summary>
        private void WaitForCompilationAndAttach(GameObject prefab, string uiName, string namespaceName)
        {
            var startTime = EditorApplication.timeSinceStartup;
            var maxWaitTime = 5.0;
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            var typeName = $"{namespaceName}.{uiName}";
            
            EditorApplication.update += CheckCompilationAndAttach;
            
            void CheckCompilationAndAttach()
            {
                if (EditorApplication.timeSinceStartup - startTime > maxWaitTime)
                {
                    EditorApplication.update -= CheckCompilationAndAttach;
                    Debug.LogWarning($"[UIManagement] {uiName} 等待编译超时，请手动添加组件到Prefab");
                    return;
                }
                
                if (EditorApplication.isCompiling)
                {
                    return;
                }
                
                EditorApplication.update -= CheckCompilationAndAttach;
                AttachScriptToPrefab(prefabPath, typeName);
            }
        }
        
        /// <summary>
        /// 绑定脚本到Prefab
        /// </summary>
        private void AttachScriptToPrefab(string prefabPath, string typeName)
        {
            try
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                System.Type scriptType = null;
                
                foreach (var assembly in assemblies)
                {
                    scriptType = assembly.GetType(typeName);
                    if (scriptType != null) break;
                }
                
                if (scriptType == null) return;
                
                var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                var existingComponent = prefabRoot.GetComponent(scriptType);
                
                if (existingComponent == null)
                {
                    prefabRoot.AddComponent(scriptType);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                }
                
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManagement] 绑定脚本失败: {ex.Message}");
            }
        }
        
        private void SelectAll()
        {
            _selectedIndices.Clear();
            for (int i = 0; i < _prefabInfos.Count; i++)
            {
                _selectedIndices.Add(i);
            }
        }
        
        private void InvertSelection()
        {
            var newSelection = new HashSet<int>();
            for (int i = 0; i < _prefabInfos.Count; i++)
            {
                if (!_selectedIndices.Contains(i))
                {
                    newSelection.Add(i);
                }
            }
            _selectedIndices = newSelection;
        }
        
        private string GetUIName(string prefabName)
        {
            var name = prefabName.Replace(".prefab", "");
            
            // 将空格替换为下划线
            name = name.Replace(" ", "_");
            
            // 移除其他非法字符
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[^\w]", "_");
            
            // 直接使用预制体名称，不添加UI后缀
            return name;
        }
        
        private string GetResourcePath(string assetPath)
        {
            var index = assetPath.IndexOf("Resources/");
            if (index >= 0)
            {
                var path = assetPath.Substring(index + "Resources/".Length);
                path = path.Replace(".prefab", "");
                return path;
            }
            
            return assetPath.Replace("Assets/", "").Replace(".prefab", "");
        }
    }
    
        /// <summary>
        /// UI Prefab信息
        /// </summary>
        public class UIPrefabInfo
        {
            public GameObject Prefab;
            public string PrefabPath;
            public string UIName;
            
            public bool LogicFileExists;
            public bool BindingFileExists;
            public bool ConfigExists;
            
            public string LogicFilePath;
            public string BindingFilePath;
            
            public string LayerName;
            public UIInstanceStrategy InstanceStrategy = UIInstanceStrategy.Singleton;
            public UIPrefabStatus Status;
        }
    
    /// <summary>
    /// UI Prefab状态
    /// </summary>
    public enum UIPrefabStatus
    {
        NotCreated,     // 未创建
        Partial,        // 部分创建
        Created,        // 已创建
        PrefabMissing   // Prefab丢失（仅配置存在）
    }
}
#endif

