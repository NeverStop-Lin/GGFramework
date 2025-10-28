#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
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
        
        // 延迟刷新标记
        private bool _needRefresh = false;
        
        public void OnEnable()
        {
            LoadConfig();
            LoadSettings();
            ScanPrefabs();
        }
        
        public void OnGUI()
        {
            // 处理延迟刷新
            if (_needRefresh)
            {
                _needRefresh = false;
                ScanPrefabs();
            }
            
            EditorGUILayout.LabelField("UI管理", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (_config == null)
            {
                EditorGUILayout.HelpBox("未找到配置文件", MessageType.Warning);
                return;
            }
            
            // 使用垂直布局，固定底部区域
            EditorGUILayout.BeginVertical();
            
            DrawDirectoryManagement();
            EditorGUILayout.Space();
            DrawToolbar();
            EditorGUILayout.Space();
            DrawPrefabList();
            
            // 固定底部区域（不随滚动）
            EditorGUILayout.Space();
            
            // 使用分隔线
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            
            EditorGUILayout.Space(5);
            DrawBatchActions();
            EditorGUILayout.Space(5);
            DrawStatus();
            
            EditorGUILayout.EndVertical();
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
            
            info.LogicFileExists = File.Exists(logicPath);
            info.LogicFilePath = logicPath;
            
            // 检查配置
            var uiConfig = _config.GetUIConfig(info.UIName);
            info.ConfigExists = uiConfig != null;
            
            if (uiConfig != null)
            {
                info.LayerName = uiConfig.LayerName;
                info.InstanceStrategy = uiConfig.InstanceStrategy;
                info.CacheStrategy = uiConfig.CacheStrategy;
                info.Preload = uiConfig.Preload;
            }
            else
            {
                // 使用默认层级（第一个）
                info.LayerName = _config.LayerDefinitions.Count > 0 
                    ? _config.LayerDefinitions[0].LayerName 
                    : "Main";
                info.InstanceStrategy = UIInstanceStrategy.Singleton;
                info.CacheStrategy = UICacheStrategy.SmartCache;  // 默认智能缓存
                info.Preload = false;
            }
            
            // 判断状态
            if (info.Prefab == null)
            {
                // Prefab不存在但配置存在
                info.Status = UIPrefabStatus.PrefabMissing;
            }
            else if (info.LogicFileExists && info.ConfigExists)
            {
                info.Status = UIPrefabStatus.Created;
            }
            else if (info.LogicFileExists || info.ConfigExists)
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
                    var directory = _settings.PrefabDirectories[i];
                    var isDefaultCreationPath = directory == _settings.UIPrefabCreationDefaultPath;
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(25));
                    EditorGUILayout.LabelField(directory);
                    
                    // 如果是默认创建路径，显示标签且不允许删除
                    if (isDefaultCreationPath)
                    {
                        var oldColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.8f, 1f, 0.8f);
                        GUILayout.Label("[默认创建路径]", EditorStyles.helpBox, GUILayout.Width(100));
                        GUI.backgroundColor = oldColor;
                    }
                    else
                    {
                    if (GUILayout.Button("删除", GUILayout.Width(60)))
                    {
                        EditorGUILayout.EndHorizontal();
                        _settings.PrefabDirectories.RemoveAt(i);
                        _settings.Save();
                        _needRefresh = true; // 延迟刷新
                        break;
                    }
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
                _needRefresh = true; // 延迟刷新
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
            
            // 创建UI预制体按钮（醒目的绿色样式）
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("✚ 创建UI预制体", GUILayout.Width(120), GUILayout.Height(25)))
            {
                // 延迟调用，避免打断当前GUI布局
                EditorApplication.delayCall += CreateNewUIPrefab;
            }
            GUI.backgroundColor = oldColor;
            
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
                        // 不在这里扫描，由调用者设置 _needRefresh
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
            EditorGUILayout.LabelField("层级", GUILayout.Width(80));
            EditorGUILayout.LabelField("实例", GUILayout.Width(70));
            EditorGUILayout.LabelField("缓存", GUILayout.Width(100));
            EditorGUILayout.LabelField("预加载", GUILayout.Width(60));
            EditorGUILayout.LabelField("操作", GUILayout.Width(100));
            EditorGUILayout.LabelField("删除", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            
            // 使用灵活高度，自动填充剩余空间
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            
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
            
            // 层级（下拉选择）
            DrawLayerSelector(info, 80);
            
            // 实例策略（下拉选择）
            DrawInstanceStrategySelector(info, 70);
            
            // 缓存策略（下拉选择）
            DrawCacheStrategySelector(info, 100);
            
            // 缓存策略警告（如果多实例使用SmartCache）
            if (info.InstanceStrategy == UIInstanceStrategy.Multiple && 
                info.CacheStrategy == UICacheStrategy.SmartCache)
            {
                var warningContent = new GUIContent("⚠", "多实例UI不建议使用智能缓存！可能导致缓存管理失效。");
                var warningStyle = new GUIStyle(EditorStyles.label);
                warningStyle.normal.textColor = new Color(1f, 0.5f, 0f);
                warningStyle.fontSize = 16;
                EditorGUILayout.LabelField(warningContent, warningStyle, GUILayout.Width(20));
            }
            else
            {
                GUILayout.Space(20);
            }
            
            // 预加载（复选框）
            DrawPreloadToggle(info, 60);
            
            // 操作按钮
            DrawActionButton(info, 100);
            
            // 删除按钮
            if (GUILayout.Button("删除", GUILayout.Width(60)))
            {
                // 延迟调用，避免打断当前GUI布局
                var infoToDelete = info;
                EditorApplication.delayCall += () => DeleteUI(infoToDelete);
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
                UpdateUIConfigFromInfo(info);
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
                UpdateUIConfigFromInfo(info);
            }
            
            GUI.backgroundColor = oldColor;
        }
        
        private void DrawCacheStrategySelector(UIPrefabInfo info, int width)
        {
            var strategies = new[] { "永久缓存", "智能缓存", "不缓存" };
            var currentIndex = (int)info.CacheStrategy;
            
            var newIndex = EditorGUILayout.Popup(currentIndex, strategies, GUILayout.Width(width));
            if (newIndex != currentIndex)
            {
                var newStrategy = (UICacheStrategy)newIndex;
                
                // 验证：多实例UI不建议使用SmartCache
                if (info.InstanceStrategy == UIInstanceStrategy.Multiple && 
                    newStrategy == UICacheStrategy.SmartCache)
                {
                    var result = EditorUtility.DisplayDialog(
                        "缓存策略警告",
                        "多实例UI不建议使用【智能缓存】策略！\n\n" +
                        "原因：\n" +
                        "• 智能缓存按UI类型统计，无法准确统计多实例数量\n" +
                        "• 多个实例可能占据大量缓存空间，影响其他UI\n" +
                        "• 可能导致缓存管理失效\n\n" +
                        "建议：\n" +
                        "• 临时UI（Toast、Dialog）→ 使用【不缓存】\n" +
                        "• 常驻UI → 使用【永久缓存】\n\n" +
                        "是否仍要设置为智能缓存？",
                        "仍要设置",
                        "取消"
                    );
                    
                    if (!result)
                    {
                        return; // 用户取消，不修改
                    }
                }
                
                info.CacheStrategy = newStrategy;
                UpdateUIConfigFromInfo(info);
            }
        }
        
        private void DrawPreloadToggle(UIPrefabInfo info, int width)
        {
            var options = new[] { "否", "是" };
            var currentIndex = info.Preload ? 1 : 0;
            
            var newIndex = EditorGUILayout.Popup(currentIndex, options, GUILayout.Width(width));
            if (newIndex != currentIndex)
            {
                info.Preload = (newIndex == 1);
                UpdateUIConfigFromInfo(info);
            }
        }
        
        /// <summary>
        /// 从UIPrefabInfo更新配置
        /// </summary>
        private void UpdateUIConfigFromInfo(UIPrefabInfo info)
        {
            if (_config == null || !info.ConfigExists) return;
            
            var uiConfig = _config.GetUIConfig(info.UIName);
            if (uiConfig == null) return;
            
            uiConfig.LayerName = info.LayerName;
            uiConfig.InstanceStrategy = info.InstanceStrategy;
            uiConfig.CacheStrategy = info.CacheStrategy;
            uiConfig.Preload = info.Preload;
            
            // 保存配置
            UIProjectConfigEditorHelper.SaveConfig(_config);
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
                // 延迟调用，避免打断当前GUI布局
                EditorApplication.delayCall += BatchDelete;
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
                
                // 步骤2: 确定逻辑脚本路径（相对于项目根目录）
                var logicFilePath = Path.Combine(_logicOutputPath, $"{info.UIName}.cs");
                var logicScriptPath = ConvertToRelativePath(logicFilePath);
                
                // 步骤3: 先更新配置（包含脚本路径）
                UpdateUIConfig(info, info.LayerName, logicScriptPath);
                
                // 步骤4: 确保目录存在
                if (!Directory.Exists(_logicOutputPath))
                {
                    Directory.CreateDirectory(_logicOutputPath);
                }
                
                // 步骤5: 生成代码（仅在不存在时创建）
                if (!File.Exists(logicFilePath))
                {
                    var logicCode = UICodeTemplate.GenerateLogicCode(info.UIName, _namespace);
                    File.WriteAllText(logicFilePath, logicCode);
                    Debug.Log($"[UI代码生成] {info.UIName} Logic 文件已创建: {logicScriptPath}");
                }
                else
                {
                    Debug.Log($"[UI代码生成] {info.UIName} Logic 文件已存在，跳过");
                }
                
                // 刷新资源
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                
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
        private void UpdateUIConfig(UIPrefabInfo info, string layerName, string logicScriptPath)
        {
            if (_config == null) return;
            
            var resourcePath = GetResourcePath(info.PrefabPath);
            
            var uiConfig = new UIInstanceConfig
            {
                UIName = info.UIName,
                ResourcePath = resourcePath,
                LayerName = layerName,
                CacheStrategy = info.CacheStrategy,  // 使用info中的缓存策略
                Preload = info.Preload,
                InstanceStrategy = info.InstanceStrategy,
                LogicScriptPath = logicScriptPath
            };
            
            _config.AddOrUpdateUIConfig(uiConfig);
            
            // 保存配置（触发代码生成）
            UIProjectConfigEditorHelper.SaveConfig(_config);
        }
        
        /// <summary>
        /// 更新UI配置（创建新UI时使用）
        /// </summary>
        private void UpdateUIConfig(string uiName, string prefabPath, string layerName, string logicScriptPath)
        {
            if (_config == null) return;
            
            var resourcePath = GetResourcePath(prefabPath);
            
            var uiConfig = new UIInstanceConfig
            {
                UIName = uiName,
                ResourcePath = resourcePath,
                LayerName = layerName,
                CacheStrategy = UICacheStrategy.SmartCache,  // 默认智能缓存
                Preload = false,
                InstanceStrategy = UIInstanceStrategy.Singleton,
                LogicScriptPath = logicScriptPath
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
            // 显示删除选项对话框
            var deleteOptions = UIDeleteDialog.Show(
                info.UIName,
                info.Prefab != null,
                info.LogicFileExists,
                info.ConfigExists
            );
            
            if (deleteOptions == null || !deleteOptions.Confirmed)
            {
                return; // 用户取消
            }
            
            try
            {
                var deletedItems = new System.Collections.Generic.List<string>();
                
                // 删除预制体
                if (deleteOptions.DeletePrefab && info.Prefab != null)
                {
                    var prefabPath = AssetDatabase.GetAssetPath(info.Prefab);
                    if (!string.IsNullOrEmpty(prefabPath) && File.Exists(prefabPath))
                    {
                        AssetDatabase.DeleteAsset(prefabPath);
                        deletedItems.Add("预制体");
                    }
                }
                
                // 删除逻辑脚本（先从预制体移除组件）
                if (deleteOptions.DeleteLogicScript && info.LogicFileExists && File.Exists(info.LogicFilePath))
                {
                    // 如果预制体存在，先移除上面的脚本组件
                    if (info.Prefab != null)
                    {
                        RemoveScriptFromPrefab(info.Prefab, $"{_namespace}.{info.UIName}");
                    }
                    
                    // 删除脚本文件
                    File.Delete(info.LogicFilePath);
                    if (File.Exists(info.LogicFilePath + ".meta"))
                    {
                        File.Delete(info.LogicFilePath + ".meta");
                    }
                    deletedItems.Add("逻辑脚本");
                }
                
                // 删除配置
                if (deleteOptions.DeleteConfig && info.ConfigExists)
                {
                    _config.RemoveUIConfig(info.UIName);
                    UIProjectConfigEditorHelper.SaveConfig(_config);
                    deletedItems.Add("配置");
                }
                
                // 刷新资源
                AssetDatabase.Refresh();
                
                // 同步IDE项目文件
                if (deleteOptions.DeleteLogicScript)
                {
                    SyncIDEProjectFiles();
                }
                
                // 延迟刷新列表（避免在GUI绘制中刷新）
                _needRefresh = true;
                
                _statusMessage = $"已删除 {info.UIName} 的 {string.Join("、", deletedItems)}";
                Debug.Log($"[UIManagement] {_statusMessage}");
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"删除失败: {ex.Message}";
                Debug.LogError($"[UIManagement] {_statusMessage}");
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
            
            if (selectedInfos.Count == 0)
            {
                return;
            }
            
            // 统计存在的内容
            var hasPrefab = selectedInfos.Any(i => i.Prefab != null);
            var hasLogic = selectedInfos.Any(i => i.LogicFileExists);
            var hasConfig = selectedInfos.Any(i => i.ConfigExists);
            
            // 显示统一的删除选项对话框（使用第一个UI的名称作为标题提示）
            var deleteOptions = UIDeleteDialog.Show(
                $"{selectedInfos.Count} 个UI",
                hasPrefab,
                hasLogic,
                hasConfig
            );
            
            if (deleteOptions == null || !deleteOptions.Confirmed)
            {
                return; // 用户取消
            }
            
            var successCount = 0;
            var errorCount = 0;
            
            foreach (var info in selectedInfos)
            {
                try
                {
                    // 删除预制体
                    if (deleteOptions.DeletePrefab && info.Prefab != null)
                    {
                        var prefabPath = AssetDatabase.GetAssetPath(info.Prefab);
                        if (!string.IsNullOrEmpty(prefabPath))
                        {
                            AssetDatabase.DeleteAsset(prefabPath);
                        }
                    }
                    
                    // 删除逻辑脚本（先从预制体移除组件）
                    if (deleteOptions.DeleteLogicScript && info.LogicFileExists && File.Exists(info.LogicFilePath))
                    {
                        // 如果预制体存在，先移除上面的脚本组件
                        if (info.Prefab != null)
                        {
                            RemoveScriptFromPrefab(info.Prefab, $"{_namespace}.{info.UIName}");
                        }
                        
                        // 删除脚本文件
                        File.Delete(info.LogicFilePath);
                        if (File.Exists(info.LogicFilePath + ".meta"))
                        {
                            File.Delete(info.LogicFilePath + ".meta");
                        }
                    }
                    
                    // 删除配置
                    if (deleteOptions.DeleteConfig && info.ConfigExists)
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
            if (deleteOptions.DeleteConfig)
            {
                UIProjectConfigEditorHelper.SaveConfig(_config);
            }
            
            AssetDatabase.Refresh();
            
            // 同步IDE项目文件
            if (deleteOptions.DeleteLogicScript)
            {
                SyncIDEProjectFiles();
            }
            
            // 延迟刷新列表（避免在GUI绘制中刷新）
            _needRefresh = true;
            _selectedIndices.Clear();
            
            _statusMessage = $"批量删除完成：成功 {successCount} 个，失败 {errorCount} 个";
            EditorUtility.DisplayDialog("批量删除完成", _statusMessage, "确定");
        }
        
        // 使用 SessionState 存储待绑定信息的键
        private const string PENDING_ATTACHMENTS_KEY = "UIManagement_PendingAttachments";
        
        /// <summary>
        /// 等待编译完成并绑定脚本
        /// </summary>
        private void WaitForCompilationAndAttach(GameObject prefab, string uiName, string namespaceName, string templateClassName = null)
        {
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            var typeName = $"{namespaceName}.{uiName}";
            
            Debug.Log($"[UIManagement] 开始等待编译完成: {typeName}");
            Debug.Log($"[UIManagement] 预制体路径: {prefabPath}");
            if (!string.IsNullOrEmpty(templateClassName))
            {
                Debug.Log($"[UIManagement] 需要替换模板组件: {templateClassName}");
            }
            
            // 先尝试立即绑定（如果类型已存在）
            if (TryAttachScriptImmediately(prefabPath, typeName, templateClassName))
            {
                Debug.Log($"[UIManagement] 类型已存在，成功立即绑定");
                return;
            }
            
            // 类型不存在，保存到 SessionState 等待程序集重载后绑定
            Debug.Log($"[UIManagement] 类型尚未加载，保存到待绑定列表");
            AddPendingAttachment(prefabPath, typeName, templateClassName);
        }
        
        /// <summary>
        /// 添加待绑定项
        /// </summary>
        private void AddPendingAttachment(string prefabPath, string typeName, string templateClassName = null)
        {
            // 从 SessionState 读取现有列表
            var json = SessionState.GetString(PENDING_ATTACHMENTS_KEY, "");
            PendingAttachmentList list;
            
            if (string.IsNullOrEmpty(json))
            {
                list = new PendingAttachmentList();
            }
            else
            {
                try
                {
                    list = JsonUtility.FromJson<PendingAttachmentList>(json);
                    if (list == null || list.items == null)
                    {
                        list = new PendingAttachmentList();
                    }
                }
                catch
                {
                    list = new PendingAttachmentList();
                }
            }
            
            // 添加新项
            list.items.Add(new PendingAttachment { prefabPath = prefabPath, typeName = typeName, templateClassName = templateClassName });
            
            // 保存回 SessionState
            json = JsonUtility.ToJson(list);
            SessionState.SetString(PENDING_ATTACHMENTS_KEY, json);
            
            Debug.Log($"[UIManagement] 已保存待绑定项，当前共有 {list.items.Count} 个");
            Debug.Log($"[UIManagement] SessionState 数据: {json}");
        }
        
        /// <summary>
        /// 获取并清空待绑定列表
        /// </summary>
        private static List<PendingAttachment> GetAndClearPendingAttachments()
        {
            var json = SessionState.GetString(PENDING_ATTACHMENTS_KEY, "");
            SessionState.EraseString(PENDING_ATTACHMENTS_KEY);
            
            if (string.IsNullOrEmpty(json))
            {
                return new List<PendingAttachment>();
            }
            
            try
            {
                var list = JsonUtility.FromJson<PendingAttachmentList>(json);
                return list?.items ?? new List<PendingAttachment>();
            }
            catch
            {
                return new List<PendingAttachment>();
            }
        }
        
        /// <summary>
        /// 尝试立即绑定脚本
        /// </summary>
        private bool TryAttachScriptImmediately(string prefabPath, string typeName, string templateClassName = null)
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            System.Type scriptType = null;
            
            foreach (var assembly in assemblies)
            {
                scriptType = assembly.GetType(typeName);
                if (scriptType != null) break;
            }
            
            if (scriptType != null)
            {
                AttachScriptToPrefab(prefabPath, typeName, templateClassName);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 程序集重载后的回调（编辑器启动和每次编译后都会执行）
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnAfterAssemblyReload()
        {
            // 从 SessionState 读取待绑定列表
            var pendingList = GetAndClearPendingAttachments();
            
            if (pendingList == null || pendingList.Count == 0)
            {
                return;
            }
            
            Debug.Log($"[UIManagement] 程序集已重载，开始处理待绑定列表，数量: {pendingList.Count}");
            
            // 延迟一帧执行，确保所有程序集完全加载
            EditorApplication.delayCall += () =>
            {
                // 处理所有待绑定的脚本
                var processedCount = 0;
                var failedCount = 0;
                
                foreach (var item in pendingList)
                {
                    Debug.Log($"[UIManagement] 正在处理: {item.typeName}");
                    
                    var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                    System.Type scriptType = null;
                    
                    foreach (var assembly in assemblies)
                    {
                        scriptType = assembly.GetType(item.typeName);
                        if (scriptType != null)
                        {
                            Debug.Log($"[UIManagement] 在程序集 {assembly.GetName().Name} 中找到类型");
                            break;
                        }
                    }
                    
                    if (scriptType != null)
                    {
                        try
                        {
                            Debug.Log($"[UIManagement] 加载预制体: {item.prefabPath}");
                            var prefabRoot = PrefabUtility.LoadPrefabContents(item.prefabPath);
                            var existingComponent = prefabRoot.GetComponent(scriptType);
                            
                            if (existingComponent == null)
                            {
                                MonoBehaviour templateComponent = null;
                                
                                // 如果有模板类名，先查找模板组件
                                if (!string.IsNullOrEmpty(item.templateClassName))
                                {
                                    var components = prefabRoot.GetComponents<MonoBehaviour>();
                                    foreach (var comp in components)
                                    {
                                        if (comp != null && comp.GetType().Name == item.templateClassName)
                                        {
                                            templateComponent = comp;
                                            Debug.Log($"[UIManagement] 找到模板组件: {item.templateClassName}");
                                            break;
                                        }
                                    }
                                }
                                
                                // 添加新组件
                                var newComponent = prefabRoot.AddComponent(scriptType) as MonoBehaviour;
                                
                                // 如果找到模板组件，复制字段数据
                                if (templateComponent != null && newComponent != null)
                                {
                                    Debug.Log($"[UIManagement] 正在复制字段数据从 {item.templateClassName} 到 {item.typeName}");
                                    EditorUtility.CopySerialized(templateComponent, newComponent);
                                    Debug.Log($"[UIManagement] 字段数据复制完成");
                                    
                                    // 删除模板组件
                                    Object.DestroyImmediate(templateComponent);
                                    Debug.Log($"[UIManagement] 已删除模板组件: {item.templateClassName}");
                                }
                                
                                PrefabUtility.SaveAsPrefabAsset(prefabRoot, item.prefabPath);
                                Debug.Log($"[UIManagement] ✓ 成功绑定脚本 {item.typeName} 到预制体");
                                processedCount++;
                            }
                            else
                            {
                                Debug.Log($"[UIManagement] 预制体已存在组件 {item.typeName}");
                                processedCount++;
                            }
                            
                            PrefabUtility.UnloadPrefabContents(prefabRoot);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[UIManagement] ✗ 绑定脚本失败 {item.typeName}: {ex.Message}");
                            failedCount++;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[UIManagement] ✗ 程序集重载后仍找不到类型: {item.typeName}");
                        failedCount++;
                    }
                }
                
                Debug.Log($"[UIManagement] 待绑定列表处理完成 - 成功: {processedCount}, 失败: {failedCount}");
            };
        }
        
        /// <summary>
        /// 绑定脚本到Prefab
        /// </summary>
        private void AttachScriptToPrefab(string prefabPath, string typeName, string templateClassName = null)
        {
            try
            {
                Debug.Log($"[UIManagement] 尝试绑定脚本: {typeName} 到 {prefabPath}");
                
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                System.Type scriptType = null;
                
                foreach (var assembly in assemblies)
                {
                    scriptType = assembly.GetType(typeName);
                    if (scriptType != null)
                    {
                        Debug.Log($"[UIManagement] 在程序集 {assembly.GetName().Name} 中找到类型: {typeName}");
                        break;
                    }
                }
                
                if (scriptType == null)
                {
                    Debug.LogError($"[UIManagement] 找不到脚本类型: {typeName}，请检查：\n" +
                                   $"1. 脚本是否编译成功\n" +
                                   $"2. 命名空间和类名是否正确\n" +
                                   $"3. 脚本文件路径: {prefabPath}");
                    return;
                }
                
                var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                var existingComponent = prefabRoot.GetComponent(scriptType);
                
                if (existingComponent == null)
                {
                    MonoBehaviour templateComponent = null;
                    
                    // 如果有模板类名，先查找并保存模板组件
                    if (!string.IsNullOrEmpty(templateClassName))
                    {
                        var components = prefabRoot.GetComponents<MonoBehaviour>();
                        foreach (var comp in components)
                        {
                            if (comp != null && comp.GetType().Name == templateClassName)
                            {
                                templateComponent = comp;
                                Debug.Log($"[UIManagement] 找到模板组件: {templateClassName}");
                                break;
                            }
                        }
                    }
                    
                    // 添加新组件
                    var newComponent = prefabRoot.AddComponent(scriptType) as MonoBehaviour;
                    
                    // 如果找到模板组件，复制字段数据
                    if (templateComponent != null && newComponent != null)
                    {
                        Debug.Log($"[UIManagement] 正在复制字段数据从 {templateClassName} 到 {typeName}");
                        EditorUtility.CopySerialized(templateComponent, newComponent);
                        Debug.Log($"[UIManagement] 字段数据复制完成");
                        
                        // 删除模板组件
                        Object.DestroyImmediate(templateComponent);
                        Debug.Log($"[UIManagement] 已删除模板组件: {templateClassName}");
                    }
                    
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    Debug.Log($"[UIManagement] 成功绑定脚本 {typeName} 到预制体");
                }
                else
                {
                    Debug.Log($"[UIManagement] 预制体已存在组件 {typeName}，跳过绑定");
                }
                
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManagement] 绑定脚本失败: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 从Prefab移除脚本组件
        /// </summary>
        private void RemoveScriptFromPrefab(GameObject prefab, string typeName)
        {
            try
            {
                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                Debug.Log($"[UIManagement] 尝试从预制体移除脚本: {typeName}");
                
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                System.Type scriptType = null;
                
                foreach (var assembly in assemblies)
                {
                    scriptType = assembly.GetType(typeName);
                    if (scriptType != null) break;
                }
                
                if (scriptType == null)
                {
                    Debug.LogWarning($"[UIManagement] 未找到类型 {typeName}，可能已被删除或未编译");
                    return;
                }
                
                var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                var component = prefabRoot.GetComponent(scriptType);
                
                if (component != null)
                {
                    Object.DestroyImmediate(component, true);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    Debug.Log($"[UIManagement] 成功从预制体移除脚本 {typeName}");
                }
                else
                {
                    Debug.Log($"[UIManagement] 预制体上不存在组件 {typeName}");
                }
                
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManagement] 移除脚本失败: {ex.Message}");
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
        
        /// <summary>
        /// 将绝对路径或Assets路径转换为相对于项目根目录的相对路径
        /// </summary>
        private string ConvertToRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";
            
            // 如果是绝对路径，尝试转换为相对路径
            if (Path.IsPathRooted(path))
            {
                var projectPath = Directory.GetCurrentDirectory();
                if (path.StartsWith(projectPath))
                {
                    path = path.Substring(projectPath.Length + 1);
                }
            }
            
            // 统一使用正斜杠
            return path.Replace("\\", "/");
        }
        
        /// <summary>
        /// 创建新的UI预制体
        /// </summary>
        private void CreateNewUIPrefab()
        {
            // 步骤1: 生成默认名称
            var defaultName = GenerateDefaultUIName();
            
            // 步骤2: 获取默认目录
            var defaultDirectory = _settings != null && !string.IsNullOrEmpty(_settings.UIPrefabCreationDefaultPath)
                ? _settings.UIPrefabCreationDefaultPath
                : "Assets/Game/Resources/UI";
            
            // 确保默认目录存在
            if (!Directory.Exists(defaultDirectory))
            {
                Directory.CreateDirectory(defaultDirectory);
            }
            
            // 步骤3: 显示创建对话框
            var dialogData = UICreateDialog.Show(defaultName, defaultDirectory, _config?.LayerDefinitions);
            
            if (dialogData == null || !dialogData.Confirmed)
            {
                return; // 用户取消
            }
            
            // 步骤4: 验证名称
            if (!ValidateUIName(dialogData.UIName, out string errorMessage))
            {
                EditorUtility.DisplayDialog("名称无效", errorMessage, "确定");
                return;
            }
            
            // 步骤5: 验证目录
            if (string.IsNullOrEmpty(dialogData.SaveDirectory))
            {
                EditorUtility.DisplayDialog("路径错误", "必须指定保存目录", "确定");
                return;
            }
            
            // 确保保存目录存在
            if (!Directory.Exists(dialogData.SaveDirectory))
            {
                Directory.CreateDirectory(dialogData.SaveDirectory);
            }
            
            // 步骤6: 验证模板
            if (string.IsNullOrEmpty(dialogData.TemplatePath))
            {
                EditorUtility.DisplayDialog("错误", "未选择模板", "确定");
                return;
            }
            
            var uiName = dialogData.UIName;
            var layerName = dialogData.LayerName;
            var relativePath = Path.Combine(dialogData.SaveDirectory, $"{uiName}.prefab");
            
            // 步骤7: 加载模板
            var templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dialogData.TemplatePath);
            
            if (templatePrefab == null)
            {
                EditorUtility.DisplayDialog("错误", $"未找到UI模板：{dialogData.TemplatePath}", "确定");
                return;
            }
            
            // 步骤8: 实例化模板并应用配置
            var instance = PrefabUtility.InstantiatePrefab(templatePrefab) as GameObject;
            if (instance == null)
            {
                EditorUtility.DisplayDialog("错误", "实例化模板失败", "确定");
                return;
            }
            
            // 步骤9: 解除与模板的prefab关联（彻底断开，变成普通GameObject）
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            
            // 重命名
            instance.name = uiName;
            
            // 步骤10: 应用Canvas Scaler设置
            var canvasScaler = instance.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (canvasScaler != null && _config != null)
            {
                canvasScaler.referenceResolution = new Vector2(
                    _config.ReferenceResolutionWidth,
                    _config.ReferenceResolutionHeight
                );
                canvasScaler.matchWidthOrHeight = _config.MatchWidthOrHeight;
            }
            
            // 步骤11: 保存为全新的预制体（包含模板脚本，稍后会替换）
            var newPrefab = PrefabUtility.SaveAsPrefabAsset(instance, relativePath);
            
            // 删除场景中的实例
            Object.DestroyImmediate(instance);
            
            if (newPrefab != null)
            {
                // 刷新资源数据库
                AssetDatabase.Refresh();
                
                // 步骤12: 确定脚本路径（相对于项目根目录）
                var logicFilePath = Path.Combine(_logicOutputPath, $"{uiName}.cs");
                var logicScriptPath = ConvertToRelativePath(logicFilePath);
                
                // 步骤13: 先更新配置（包含脚本路径）
                UpdateUIConfig(uiName, relativePath, layerName, logicScriptPath);
                
                // 步骤14: 生成脚本（支持从模板代码复制）
                // 提取模板类名用于后续删除
                string templateClassName = null;
                if (!string.IsNullOrEmpty(dialogData.TemplateCodePath))
                {
                    templateClassName = Path.GetFileNameWithoutExtension(dialogData.TemplateCodePath);
                }
                GenerateUIScriptsFromTemplate(newPrefab, uiName, relativePath, dialogData.TemplateCodePath, templateClassName);
                
                // 步骤15: 延迟刷新列表
                _needRefresh = true;
                
                // 高亮显示新创建的预制体
                EditorGUIUtility.PingObject(newPrefab);
                Selection.activeObject = newPrefab;
                
                _statusMessage = $"✓ UI预制体 {uiName} 创建成功（含脚本）";
                Debug.Log($"[UIManagement] UI预制体创建成功: {relativePath}");
            }
            else
            {
                EditorUtility.DisplayDialog("失败", "创建预制体失败", "确定");
            }
        }
        
        /// <summary>
        /// 生成UI脚本并绑定到预制体
        /// </summary>
        private void GenerateUIScripts(GameObject prefab, string uiName, string prefabPath)
        {
            try
            {
                // 生成代码
                var logicCode = UICodeTemplate.GenerateLogicCode(uiName, _namespace);
                
                // 确保目录存在
                if (!Directory.Exists(_logicOutputPath))
                {
                    Directory.CreateDirectory(_logicOutputPath);
                }
                
                var logicFilePath = Path.Combine(_logicOutputPath, $"{uiName}.cs");
                
                // 写入文件
                File.WriteAllText(logicFilePath, logicCode);
                
                Debug.Log($"[UIManagement] {uiName} 脚本已生成");
                
                // 刷新并等待编译
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                
                // 强制同步IDE项目文件，使IDE能立即识别新文件
                SyncIDEProjectFiles();
                
                // 等待编译完成后绑定脚本到预制体
                WaitForCompilationAndAttach(prefab, uiName, _namespace);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManagement] {uiName} 脚本生成失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 移除预制体上的模板UI脚本组件
        /// </summary>
        private void RemoveTemplateScriptComponent(GameObject instance, string templateCodePath)
        {
            try
            {
                // 从模板代码路径提取类名
                var templateClassName = Path.GetFileNameWithoutExtension(templateCodePath);
                
                if (string.IsNullOrEmpty(templateClassName))
                {
                    Debug.LogWarning($"[UIManagement] 无法从模板代码路径提取类名: {templateCodePath}");
                    return;
                }
                
                // 获取所有MonoBehaviour组件
                var components = instance.GetComponents<MonoBehaviour>();
                
                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    // 检查组件的类型名是否匹配模板类名
                    var componentTypeName = component.GetType().Name;
                    
                    if (componentTypeName == templateClassName)
                    {
                        Debug.Log($"[UIManagement] 移除模板脚本组件: {componentTypeName}");
                        Object.DestroyImmediate(component);
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManagement] 移除模板脚本组件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从模板代码生成新的UI代码（替换类名）
        /// </summary>
        private string GenerateCodeFromTemplate(string templateCodePath, string newUIName, string namespaceName)
        {
            try
            {
                // 读取模板代码
                var templateCode = File.ReadAllText(templateCodePath);
                
                // 提取模板类名（查找 public class XXX）
                var classNameMatch = System.Text.RegularExpressions.Regex.Match(
                    templateCode, 
                    @"public\s+class\s+(\w+)\s*:"
                );
                
                if (!classNameMatch.Success)
                {
                    Debug.LogWarning($"[UIManagement] 无法从模板代码中提取类名，使用默认模板");
                    return null;
                }
                
                var templateClassName = classNameMatch.Groups[1].Value;
                
                // 替换类名（使用单词边界确保完整匹配）
                var newCode = System.Text.RegularExpressions.Regex.Replace(
                    templateCode,
                    @"\b" + templateClassName + @"\b",
                    newUIName
                );
                
                // 替换命名空间
                newCode = System.Text.RegularExpressions.Regex.Replace(
                    newCode,
                    @"namespace\s+[\w\.]+",
                    $"namespace {namespaceName}"
                );
                
                return newCode;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManagement] 从模板代码生成失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 生成UI脚本（支持从模板代码复制）并绑定到预制体
        /// </summary>
        private void GenerateUIScriptsFromTemplate(GameObject prefab, string uiName, string prefabPath, string templateCodePath, string templateClassName)
        {
            try
            {
                string logicCode;
                
                // 如果有模板代码，从模板复制
                if (!string.IsNullOrEmpty(templateCodePath) && File.Exists(templateCodePath))
                {
                    logicCode = GenerateCodeFromTemplate(templateCodePath, uiName, _namespace);
                    
                    // 如果模板代码生成失败，回退到默认模板
                    if (string.IsNullOrEmpty(logicCode))
                    {
                        Debug.LogWarning($"[UIManagement] 使用默认模板生成 {uiName} 脚本");
                        logicCode = UICodeTemplate.GenerateLogicCode(uiName, _namespace);
                        templateClassName = null; // 没有模板组件需要删除
                    }
                    else
                    {
                        Debug.Log($"[UIManagement] {uiName} 脚本已从模板复制");
                    }
                }
                else
                {
                    // 没有模板代码，使用默认模板
                    logicCode = UICodeTemplate.GenerateLogicCode(uiName, _namespace);
                    Debug.Log($"[UIManagement] {uiName} 脚本使用默认模板生成");
                    templateClassName = null; // 没有模板组件需要删除
                }
                
                // 确保目录存在
                if (!Directory.Exists(_logicOutputPath))
                {
                    Directory.CreateDirectory(_logicOutputPath);
                }
                
                var logicFilePath = Path.Combine(_logicOutputPath, $"{uiName}.cs");
                
                // 写入文件
                File.WriteAllText(logicFilePath, logicCode);
                
                // 刷新并等待编译
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                
                // 强制同步IDE项目文件，使IDE能立即识别新文件
                SyncIDEProjectFiles();
                
                // 等待编译完成后绑定脚本到预制体，并传递模板类名用于删除旧组件
                WaitForCompilationAndAttach(prefab, uiName, _namespace, templateClassName);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManagement] {uiName} 脚本生成失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 强制同步IDE项目文件
        /// </summary>
        private void SyncIDEProjectFiles()
        {
            try
            {
                // 方法1: 使用CodeEditor API（Unity 2020+推荐方法）
                var codeEditorType = System.Type.GetType("UnityEditor.CodeEditor.CodeEditor,UnityEditor");
                if (codeEditorType != null)
                {
                    var currentEditorProperty = codeEditorType.GetProperty("CurrentEditor", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (currentEditorProperty != null)
                    {
                        var currentEditor = currentEditorProperty.GetValue(null);
                        if (currentEditor != null)
                        {
                            var syncMethod = currentEditor.GetType().GetMethod("SyncAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            if (syncMethod != null)
                            {
                                syncMethod.Invoke(currentEditor, null);
                                Debug.Log("[UIManagement] IDE项目文件已同步（CodeEditor.SyncAll）");
                                return;
                            }
                        }
                    }
                }
                
                // 方法2: 使用SyncVS（较旧的Unity版本）
                var syncVS = System.Type.GetType("UnityEditor.SyncVS,UnityEditor");
                if (syncVS != null)
                {
                    var syncSolution = syncVS.GetMethod("SyncSolution", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (syncSolution != null)
                    {
                        syncSolution.Invoke(null, null);
                        Debug.Log("[UIManagement] IDE项目文件已同步（SyncVS.SyncSolution）");
                        return;
                    }
                }
                
                // 方法3: 强制重新导入，触发IDE更新
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                
                // 方法4: 触摸.csproj文件，更新修改时间，强制IDE重新加载
                TouchCSharpProjectFiles();
                
                Debug.Log("[UIManagement] 已强制刷新资源数据库并更新项目文件时间戳");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UIManagement] IDE项目同步失败，可能需要手动刷新IDE: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 触摸C#项目文件，更新修改时间，强制IDE重新加载
        /// </summary>
        private void TouchCSharpProjectFiles()
        {
            try
            {
                var projectRoot = Directory.GetCurrentDirectory();
                var csprojFiles = Directory.GetFiles(projectRoot, "*.csproj", SearchOption.TopDirectoryOnly);
                
                foreach (var csprojFile in csprojFiles)
                {
                    File.SetLastWriteTime(csprojFile, System.DateTime.Now);
                }
                
                if (csprojFiles.Length > 0)
                {
                    Debug.Log($"[UIManagement] 已更新 {csprojFiles.Length} 个.csproj文件时间戳");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UIManagement] 更新.csproj文件时间戳失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 生成默认UI名称
        /// </summary>
        private string GenerateDefaultUIName()
        {
            // 查找当前所有UI名称中的数字后缀
            int maxNumber = 0;
            
            foreach (var prefabInfo in _prefabInfos)
            {
                var name = prefabInfo.UIName;
                
                // 尝试匹配 UI_XXX 格式
                if (name.StartsWith("UI_"))
                {
                    var numberPart = name.Substring(3);
                    if (int.TryParse(numberPart, out int number))
                    {
                        if (number > maxNumber)
                        {
                            maxNumber = number;
                        }
                    }
                }
            }
            
            // 生成新名称
            return $"UI_{(maxNumber + 1):D3}";
        }
        
        /// <summary>
        /// 验证UI名称
        /// </summary>
        private bool ValidateUIName(string name, out string errorMessage)
        {
            errorMessage = "";
            
            // 检查是否为空
            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "UI名称不能为空";
                return false;
            }
            
            // 检查是否符合C#命名规范
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                errorMessage = "UI名称只能包含字母、数字、下划线，且不能以数字开头";
                return false;
            }
            
            // 检查是否与现有UI重名
            var existingUI = _prefabInfos.FirstOrDefault(p => p.UIName == name);
            if (existingUI != null)
            {
                errorMessage = $"UI名称 '{name}' 已存在";
                return false;
            }
            
            return true;
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
            public bool ConfigExists;
            
            public string LogicFilePath;
            
            public string LayerName;
            public UIInstanceStrategy InstanceStrategy = UIInstanceStrategy.Singleton;
            public UICacheStrategy CacheStrategy = UICacheStrategy.AlwaysCache;
            public bool Preload = false;
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
    
    /// <summary>
    /// 待绑定项（可序列化）
    /// </summary>
    [System.Serializable]
    public class PendingAttachment
    {
        public string prefabPath;
        public string typeName;
        public string templateClassName; // 需要删除的模板组件类名
    }
    
    /// <summary>
    /// 待绑定列表（可序列化）
    /// </summary>
    [System.Serializable]
    public class PendingAttachmentList
    {
        public List<PendingAttachment> items = new List<PendingAttachment>();
    }
}
#endif

