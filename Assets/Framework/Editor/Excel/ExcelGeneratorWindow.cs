#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExcelDataReader;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.Excel
{
    /// <summary>
    /// Excel导表工具主窗口
    /// 提供可视化配置界面、Excel文件预览和生成文件管理
    /// </summary>
    public class ExcelGeneratorWindow : EditorWindow
    {
        private ExcelGeneratorSettings _settings;
        private Vector2 _configScrollPosition;
        private Vector2 _excelListScrollPosition;
        private Vector2 _detailsScrollPosition;
        
        private List<ExcelFileInfo> _excelFiles = new List<ExcelFileInfo>();
        private List<string> _generatedJsonFiles = new List<string>();
        private List<string> _generatedCSharpFiles = new List<string>();
        
        // Excel多选状态
        private Dictionary<string, bool> _excelCheckStates = new Dictionary<string, bool>();
        
        // 配置验证错误信息
        private string _excelRootError;
        private string _jsonOutputError;
        private string _csharpOutputError;
        private string _namespaceError;
        
        // EditorPrefs键名
        private const string EditorPrefsKeyPrefix = "ExcelGenerator_CheckState_";
        
        [MenuItem("Framework/Excel/打开Excel导表工具")]
        public static void ShowWindow()
        {
            // 确保配置索引存在
            if (!Core.FrameworkSettingsIndex.Exists())
            {
                Core.FrameworkSettingsIndex.GetOrCreate();
            }
            
            // 检查Excel生成器配置是否存在
            var settings = ExcelGeneratorSettings.Instance;
            if (settings == null)
            {
                // 配置不存在，显示欢迎窗口
                Core.SettingsWelcomeWindow.ShowExcelGeneratorWelcome();
                return;
            }
            
            // 直接打开主窗口（无论是否初始化完成）
            var window = GetWindow<ExcelGeneratorWindow>("Excel导表工具");
            // 设置合适的最小尺寸：固定宽度布局
            window.minSize = new Vector2(920, 400);
            window.maxSize = new Vector2(920, 800);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadSettings();
            RefreshAllLists();
        }
        
        private void LoadSettings()
        {
            _settings = ExcelGeneratorSettings.Instance;
        }
        
        private void OnGUI()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("配置未加载，请检查配置", MessageType.Error);
                if (GUILayout.Button("重新检查配置"))
                {
                    LoadSettings();
                }
                return;
            }
            
            // 验证配置
            ValidateConfiguration();
            
            // 整体边距
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical();
            
            // 固定宽度布局
            const float bottomHeight = 155f; // 底部固定高度
            const float topLeftWidth = 250f; // 上方左侧固定宽度
            const float topRightWidth = 550; // 上方右侧固定宽度 (1.5 × 200)
            const float bottomLeftWidth = 650f; // 底部左侧固定宽度 (500 × 0.7)
            const float bottomRightWidth = 150f; // 底部右侧固定宽度 (500 × 0.3)
            
            // === 上框：自适应高度，占剩余空间 ===
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            {
                // 左：200px（Excel文件列表）
                EditorGUILayout.BeginVertical(GUILayout.Width(topLeftWidth), GUILayout.ExpandHeight(true));
                DrawExcelFileList(topLeftWidth);
                EditorGUILayout.EndVertical();
                
                // 右：300px（导出配置信息）
                EditorGUILayout.BeginVertical(GUILayout.Width(topRightWidth), GUILayout.ExpandHeight(true));
                DrawSelectedExcelDetails(topRightWidth);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            
            // === 下框：固定高度155px ===
            var bottomBoxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(3, 3, 3, 3)
            };
            
            EditorGUILayout.BeginVertical(bottomBoxStyle, GUILayout.Height(bottomHeight));
            EditorGUILayout.BeginHorizontal();
            {
                // 左：350px（配置设置）
                EditorGUILayout.BeginVertical(GUILayout.Width(bottomLeftWidth));
                DrawConfigSection(bottomLeftWidth);
                EditorGUILayout.EndVertical();
                
                // 垂直分隔线
                EditorGUILayout.Space(4);
                DrawVerticalSeparator();
                EditorGUILayout.Space(4);
                
                // 右：150px（操作按钮）
                EditorGUILayout.BeginVertical(GUILayout.Width(bottomRightWidth));
                DrawStatisticsAndActions(bottomRightWidth);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            // 关闭整体边距
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);
        }
        
        /// <summary>
        /// 绘制选中Excel的导出信息（右上区域）
        /// 显示所有选中Excel的配置，每一条横向排列
        /// </summary>
        private void DrawSelectedExcelDetails(float availableWidth)
        {
            var boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(3, 3, 3, 3)
            };
            
            // 自动扩展填充可用宽度
            EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));
            
            // 获取所有选中的Excel
            var selectedExcels = _excelFiles.Where(e => _excelCheckStates.ContainsKey(e.FilePath) && _excelCheckStates[e.FilePath]).ToList();
            
            // 标题
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };
            EditorGUILayout.LabelField("导出配置信息", titleStyle);
            
            // 分隔线
            DrawSeparator();
            
            // 列表标题（表头）- 始终显示
            // 减去padding和边距，计算表格实际可用宽度
            float tableWidth = availableWidth - 12; // 减去padding (3*2) 和滚动条预留空间 (6)
            DrawConfigListHeader(tableWidth);
            
            // 滚动列表
            _detailsScrollPosition = EditorGUILayout.BeginScrollView(_detailsScrollPosition, GUILayout.ExpandHeight(true));
            
            if (selectedExcels.Count == 0)
            {
                // 未选中时显示空内容区域
                EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
            else
            {
                // 遍历所有选中的Excel
                foreach (var excel in selectedExcels)
                {
                    // 遍历该Excel的所有配置
                    foreach (var config in excel.Configs)
                    {
                        DrawConfigListItem(config, excel.FileName, excel.FilePath, tableWidth);
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制配置列表的表头
        /// </summary>
        private void DrawConfigListHeader(float availableWidth)
        {
            var headerStyle = new GUIStyle("box")
            {
                padding = new RectOffset(6, 6, 6, 6),
                margin = new RectOffset(0, 0, 0, 3)
            };
            
            GUI.backgroundColor = new Color(0.28f, 0.28f, 0.32f);
            EditorGUILayout.BeginHorizontal(headerStyle, GUILayout.Height(32));
            GUI.backgroundColor = Color.white;
            
            var headerTextStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.7f, 0.8f, 0.9f) },
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            
            // 列标题（固定宽度，平均分配）
            float separatorWidth = 8f;
            float contentWidth = availableWidth - 12; // 减去padding
            float totalSeparatorWidth = separatorWidth * 4; // 4个分隔符
            float columnWidth = (contentWidth - totalSeparatorWidth) / 5f; // 5列平均分配
            
            float configNameWidth = columnWidth;  // 配置名称
            float remarkWidth = columnWidth;      // 备注
            float excelWidth = columnWidth;       // 所属Excel
            float jsonWidth = columnWidth;        // JSON文件
            float csharpWidth = columnWidth;      // C#文件
            
            EditorGUILayout.LabelField("配置名称", headerTextStyle, GUILayout.Width(configNameWidth));
            EditorGUILayout.LabelField("|", GUILayout.Width(separatorWidth));
            EditorGUILayout.LabelField("备注", headerTextStyle, GUILayout.Width(remarkWidth));
            EditorGUILayout.LabelField("|", GUILayout.Width(separatorWidth));
            EditorGUILayout.LabelField("所属Excel", headerTextStyle, GUILayout.Width(excelWidth));
            EditorGUILayout.LabelField("|", GUILayout.Width(separatorWidth));
            EditorGUILayout.LabelField("JSON文件", headerTextStyle, GUILayout.Width(jsonWidth));
            EditorGUILayout.LabelField("|", GUILayout.Width(separatorWidth));
            EditorGUILayout.LabelField("C#文件", headerTextStyle, GUILayout.Width(csharpWidth));
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制单条配置信息（横向布局）
        /// 格式：配置名称 | 备注 | 所属Excel | JSON对象引用 | C#对象引用
        /// </summary>
        private void DrawConfigListItem(ConfigInfo config, string excelFileName, string excelFilePath, float availableWidth)
        {
            var itemStyle = new GUIStyle("box")
            {
                padding = new RectOffset(6, 6, 6, 6),
                margin = new RectOffset(0, 0, 0, 3)
            };
            
            var originalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.25f, 0.25f, 0.28f);
            
            EditorGUILayout.BeginHorizontal(itemStyle, GUILayout.Height(32));
            
            GUI.backgroundColor = originalBackgroundColor;
            
            // 列宽（固定宽度，与表头一致）
            float separatorWidth = 8f;
            float contentWidth = availableWidth - 12; // 减去padding
            float totalSeparatorWidth = separatorWidth * 4; // 4个分隔符
            float columnWidth = (contentWidth - totalSeparatorWidth) / 5f; // 5列平均分配
            
            float configNameWidth = columnWidth;  // 配置名称
            float remarkWidth = columnWidth;      // 备注
            float excelWidth = columnWidth;       // 所属Excel
            float jsonWidth = columnWidth;        // JSON文件
            float csharpWidth = columnWidth;      // C#文件
            
            // 配置名称（深绿色，最醒目）
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.4f, 0.9f, 0.5f) },
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(config.Name, nameStyle, GUILayout.Width(configNameWidth));
            
            // 分隔符
            var separatorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("|", separatorStyle, GUILayout.Width(separatorWidth));
            
            // 备注（淡绿色，不抢眼）
            var remarkStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.55f, 0.7f, 0.6f) },
                fontSize = 11,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(config.Remark, remarkStyle, GUILayout.Width(remarkWidth));
            
            // 分隔符
            EditorGUILayout.LabelField("|", separatorStyle, GUILayout.Width(separatorWidth));
            
            // 所属Excel（去掉扩展名，淡绿色）
            var excelNameWithoutExt = Path.GetFileNameWithoutExtension(excelFileName);
            var excelStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.55f, 0.7f, 0.6f) },
                fontSize = 11,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(excelNameWithoutExt, excelStyle, GUILayout.Width(excelWidth));
            
            // 分隔符
            EditorGUILayout.LabelField("|", separatorStyle, GUILayout.Width(separatorWidth));
            
            // JSON对象引用
            var jsonPath = FindGeneratedFile(_generatedJsonFiles, config.Name);
            var jsonAsset = string.IsNullOrEmpty(jsonPath) ? null : LoadAssetFromPath(jsonPath);
            EditorGUILayout.ObjectField(jsonAsset, typeof(UnityEngine.Object), false, GUILayout.Width(jsonWidth));
            
            // 分隔符
            EditorGUILayout.LabelField("|", separatorStyle, GUILayout.Width(separatorWidth));
            
            // C#对象引用
            var csPath = FindGeneratedFile(_generatedCSharpFiles, config.Name);
            var csAsset = string.IsNullOrEmpty(csPath) ? null : LoadAssetFromPath(csPath);
            EditorGUILayout.ObjectField(csAsset, typeof(UnityEngine.Object), false, GUILayout.Width(csharpWidth));
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 查找生成的文件
        /// </summary>
        private string FindGeneratedFile(List<string> files, string configName)
        {
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.Equals(configName, StringComparison.OrdinalIgnoreCase) || 
                    fileName.Equals($"{configName}Configs", StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 从路径加载资源
        /// </summary>
        private UnityEngine.Object LoadAssetFromPath(string fullPath)
        {
            var projectPath = Path.GetFullPath(Application.dataPath + "/..");
            var relativePath = fullPath;
            
            if (fullPath.StartsWith(projectPath))
            {
                relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length);
                relativePath = relativePath.Replace("\\", "/");
            }
            
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
        }
        
        /// <summary>
        /// 绘制操作按钮（右下区域）
        /// </summary>
        private void DrawStatisticsAndActions(float availableWidth)
        {
            EditorGUILayout.BeginVertical();
            
            // 标题（与左侧配置设置的标题对应）
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };
            EditorGUILayout.LabelField("操作", titleStyle);
            
            // 分隔线
            DrawSeparator();
            
            EditorGUILayout.Space(5);
            
            // 刷新列表按钮（自适应宽度）
            var refreshButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            if (GUILayout.Button("刷新列表", refreshButtonStyle, GUILayout.Height(28), GUILayout.ExpandWidth(true)))
            {
                RefreshAllLists();
            }
            
            EditorGUILayout.Space(6);
            
            // 导出选中按钮（绿色，自适应宽度）
            var exportSelectedButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("导出选中", exportSelectedButtonStyle, GUILayout.Height(28), GUILayout.ExpandWidth(true)))
            {
                ExportAllConfigs();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(6);
            
            // 快速导出按钮（绿色，自适应宽度）- 导出所有Excel
            var exportButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("快速导出", exportButtonStyle, GUILayout.Height(32), GUILayout.ExpandWidth(true)))
            {
                QuickExportAll();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制水平分隔线
        /// </summary>
        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// 绘制垂直分隔线
        /// </summary>
        private void DrawVerticalSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(1), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
        
        
        /// <summary>
        /// 验证配置
        /// 只有当最终值（包括默认值）也为空时才显示错误
        /// 正常情况下有默认值，不会显示红字
        /// </summary>
        private void ValidateConfiguration()
        {
            _excelRootError = null;
            _jsonOutputError = null;
            _csharpOutputError = null;
            _namespaceError = null;
            
            // 验证Excel根目录（检查最终值，包括默认值）
            var excelRootPath = _settings.ExcelRootPath;
            if (string.IsNullOrEmpty(excelRootPath))
            {
                _excelRootError = "⚠ 必填项：请设置Excel根目录";
            }
            
            // 验证JSON输出目录（检查最终值，包括默认值）
            var jsonOutputPath = _settings.JsonOutputPath;
            if (string.IsNullOrEmpty(jsonOutputPath))
            {
                _jsonOutputError = "⚠ 必填项：请设置JSON输出目录";
            }
            
            // 验证C#输出目录（检查最终值，包括默认值）
            var csharpOutputPath = _settings.CSharpOutputPath;
            if (string.IsNullOrEmpty(csharpOutputPath))
            {
                _csharpOutputError = "⚠ 必填项：请设置C#输出目录";
            }
            
            // 验证命名空间（检查最终值，包括默认值）
            var defaultNamespace = _settings.DefaultNamespace;
            if (string.IsNullOrEmpty(defaultNamespace))
            {
                _namespaceError = "⚠ 必填项：请设置默认命名空间";
            }
        }
        
        /// <summary>
        /// 绘制错误提示
        /// 显示红色文字，不闪烁，不加粗
        /// </summary>
        private void DrawErrorHint(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) return;
            
            var errorStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.red }
            };
            
            EditorGUILayout.LabelField(errorMessage, errorStyle);
        }
        
        /// <summary>
        /// 绘制配置区域
        /// </summary>
        private void DrawConfigSection(float availableWidth)
        {
            EditorGUILayout.BeginVertical();
            
            // 标题
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };
            EditorGUILayout.LabelField("配置设置", titleStyle);
            
            // 分隔线
            DrawSeparator();
            
            // 保存原始标签宽度
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 85f; // 标签固定85px
            
            _configScrollPosition = EditorGUILayout.BeginScrollView(_configScrollPosition);
            
            // 按钮宽度固定
            float buttonBrowseWidth = 50f; // 浏览按钮
            float buttonOpenWidth = 50f; // 打开按钮
            
            // Excel根目录（支持项目外路径）
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var newExcelPath = EditorGUILayout.TextField("Excel根目录", _settings.ExcelRootPath);
            if (EditorGUI.EndChangeCheck())
            {
                _settings.ExcelRootPath = newExcelPath;
                RefreshExcelList();
            }
            if (GUILayout.Button("浏览", GUILayout.Width(buttonBrowseWidth)))
            {
                BrowseExcelRootFolder();
            }
            if (GUILayout.Button("打开", GUILayout.Width(buttonOpenWidth)))
            {
                OpenDirectory(_settings.ExcelRootPath);
            }
            EditorGUILayout.EndHorizontal();
            DrawErrorHint(_excelRootError);
            EditorGUILayout.Space(5);
            
            // JSON输出目录
            // 如果字段为null，但默认目录存在，则自动关联
            if (_settings.JsonOutputFolder == null)
            {
                string defaultPath = Core.FrameworkDefaultPaths.ExcelJsonOutputFolder.TrimEnd('/');
                if (AssetDatabase.IsValidFolder(defaultPath))
                {
                    _settings.JsonOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultPath);
                    EditorUtility.SetDirty(_settings);
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var newJsonFolder = EditorGUILayout.ObjectField("JSON输出目录", _settings.JsonOutputFolder, typeof(DefaultAsset), false, GUILayout.ExpandWidth(true)) as DefaultAsset;
            if (EditorGUI.EndChangeCheck())
            {
                // 如果清空，恢复默认路径
                if (newJsonFolder == null)
                {
                    string defaultPath = Core.FrameworkDefaultPaths.ExcelJsonOutputFolder;
                    newJsonFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultPath);
                    EditorUtility.DisplayDialog("提示", $"已恢复默认路径：{defaultPath}", "确定");
                }
                _settings.JsonOutputFolder = newJsonFolder;
                RefreshJsonList();
            }
            if (GUILayout.Button("浏览", GUILayout.Width(buttonBrowseWidth)))
            {
                BrowseFolder(_settings.JsonOutputPath, (path) => 
                {
                    _settings.JsonOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                    RefreshJsonList();
                });
            }
            if (GUILayout.Button("打开", GUILayout.Width(buttonOpenWidth)))
            {
                OpenDirectory(_settings.JsonOutputPath);
            }
            EditorGUILayout.EndHorizontal();
            DrawErrorHint(_jsonOutputError);
            EditorGUILayout.Space(5);
            
            // C#输出目录
            // 如果字段为null，但默认目录存在，则自动关联
            if (_settings.CSharpOutputFolder == null)
            {
                string defaultPath = Core.FrameworkDefaultPaths.ExcelCSharpOutputFolder.TrimEnd('/');
                if (AssetDatabase.IsValidFolder(defaultPath))
                {
                    _settings.CSharpOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultPath);
                    EditorUtility.SetDirty(_settings);
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var newCSharpFolder = EditorGUILayout.ObjectField("C#输出目录", _settings.CSharpOutputFolder, typeof(DefaultAsset), false, GUILayout.ExpandWidth(true)) as DefaultAsset;
            if (EditorGUI.EndChangeCheck())
            {
                // 如果清空，恢复默认路径
                if (newCSharpFolder == null)
                {
                    string defaultPath = Core.FrameworkDefaultPaths.ExcelCSharpOutputFolder;
                    newCSharpFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultPath);
                    EditorUtility.DisplayDialog("提示", $"已恢复默认路径：{defaultPath}", "确定");
                }
                _settings.CSharpOutputFolder = newCSharpFolder;
                RefreshCSharpList();
            }
            if (GUILayout.Button("浏览", GUILayout.Width(buttonBrowseWidth)))
            {
                BrowseFolder(_settings.CSharpOutputPath, (path) => 
                {
                    _settings.CSharpOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                    RefreshCSharpList();
                });
            }
            if (GUILayout.Button("打开", GUILayout.Width(buttonOpenWidth)))
            {
                OpenDirectory(_settings.CSharpOutputPath);
            }
            EditorGUILayout.EndHorizontal();
            DrawErrorHint(_csharpOutputError);
            EditorGUILayout.Space(5);
            
            // 默认命名空间
            EditorGUI.BeginChangeCheck();
            var newNamespace = EditorGUILayout.TextField("默认命名空间", _settings.DefaultNamespace);
            if (EditorGUI.EndChangeCheck())
            {
                _settings.DefaultNamespace = newNamespace;
            }
            DrawErrorHint(_namespaceError);
            
            EditorGUILayout.EndScrollView();
            
            // 恢复原始标签宽度
            EditorGUIUtility.labelWidth = originalLabelWidth;
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制Excel文件列表（左上区域）
        /// </summary>
        private void DrawExcelFileList(float availableWidth)
        {
            var boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(3, 3, 3, 3)
            };
            
            EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));
            
            // 标题栏：带全选/反选按钮
            EditorGUILayout.BeginHorizontal();
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };
            EditorGUILayout.LabelField($"Excel文件列表 ({_excelFiles.Count})", titleStyle);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("全选", GUILayout.Width(50), GUILayout.Height(22)))
            {
                SelectAllExcels(true);
            }
            if (GUILayout.Button("反选", GUILayout.Width(50), GUILayout.Height(22)))
            {
                SelectAllExcels(false);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 分隔线
            DrawSeparator();
            
            if (_excelFiles.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到Excel文件\n请确认Excel根目录配置正确", MessageType.Info);
            }
            else
            {
                _excelListScrollPosition = EditorGUILayout.BeginScrollView(_excelListScrollPosition);
                
                // 使用副本遍历，避免在遍历时修改集合
                foreach (var excelFile in _excelFiles.ToList())
                {
                    DrawExcelFileItem(excelFile, availableWidth);
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 全选/反选所有Excel
        /// </summary>
        private void SelectAllExcels(bool select)
        {
            foreach (var excel in _excelFiles)
            {
                _excelCheckStates[excel.FilePath] = select;
                SaveCheckState(excel.FilePath, select); // 保存状态
            }
        }
        
        /// <summary>
        /// 绘制单个Excel文件项
        /// </summary>
        private void DrawExcelFileItem(ExcelFileInfo info,float availableWidth)
        {
            // 确保复选框状态已初始化
            if (!_excelCheckStates.ContainsKey(info.FilePath))
            {
                _excelCheckStates[info.FilePath] = false;
            }
            
            var isChecked = _excelCheckStates[info.FilePath];
            
            // 根据勾选状态设置不同的背景色
            var itemStyle = new GUIStyle("box")
            {
                padding = new RectOffset(6, 6, 8, 8),
                margin = new RectOffset(0, 0, 0, 1)
            };
            
            // 勾选时的背景色
            var originalBackgroundColor = GUI.backgroundColor;
            if (isChecked)
            {
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.7f, 0.5f); // 淡蓝色背景
            }
            
            EditorGUILayout.BeginHorizontal(itemStyle, GUILayout.Height(65));
            
            GUI.backgroundColor = originalBackgroundColor;
            
            // 复选框（垂直居中）
            EditorGUILayout.BeginVertical(GUILayout.Width(18));
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            var newChecked = EditorGUILayout.Toggle(isChecked, GUILayout.Width(18));
            if (EditorGUI.EndChangeCheck())
            {
                _excelCheckStates[info.FilePath] = newChecked;
                SaveCheckState(info.FilePath, newChecked); // 保存状态
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(4);
            
            // 文件信息（两行）
            EditorGUILayout.BeginVertical();
            
            // 第一行：文件名 + 按钮（对齐）
            EditorGUILayout.BeginHorizontal();
            
            // 文件名（去掉扩展名）- 自动换行不截断
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(info.FileName);
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = isChecked ? new Color(1f, 1f, 1f) : new Color(0.9f, 0.9f, 1f) },
                fontSize = 13,
                fontStyle = isChecked ? FontStyle.Bold : FontStyle.Normal,
                wordWrap = true
            };
            float nameWidth = availableWidth - 100;
            EditorGUILayout.LabelField(fileNameWithoutExt, nameStyle, GUILayout.Width(nameWidth));
            
            GUILayout.FlexibleSpace();
            
            // 导出按钮
            if (GUILayout.Button("导出", GUILayout.Width(42), GUILayout.Height(20)))
            {
                ExportSingleExcel(info);
            }
            
            EditorGUILayout.Space(1);
            
            // 打开按钮
            if (GUILayout.Button("打开", GUILayout.Width(42), GUILayout.Height(20)))
            {
                OpenExcelFile(info.FilePath);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 第一行和第二行之间的间隔
            EditorGUILayout.Space(4);
            
            // 第二行：配置名称（绿色小字）
            if (info.Configs.Count > 0)
            {
                var configNames = info.Configs.Select(c => c.Name);
                var configText = string.Join(", ", configNames);
                var configStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.4f, 0.9f, 0.5f) },
                    fontSize = 10
                };
                EditorGUILayout.LabelField(configText, configStyle);
            }
            else
            {
                var emptyStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                    fontSize = 10
                };
                EditorGUILayout.LabelField("无可导出配置", emptyStyle);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        
        /// <summary>
        /// 刷新所有列表
        /// </summary>
        private void RefreshAllLists()
        {
            RefreshExcelList();
            RefreshJsonList();
            RefreshCSharpList();
        }
        
        /// <summary>
        /// 刷新Excel文件列表
        /// </summary>
        private void RefreshExcelList()
        {
            _excelFiles.Clear();
            
            if (_settings == null) return;
            
            var excelRoot = _settings.ExcelRootPath;
            if (string.IsNullOrEmpty(excelRoot)) return;
            
            // 将相对路径转换为绝对路径
            var fullPath = excelRoot;
            if (excelRoot.StartsWith("Assets/"))
            {
                fullPath = Path.Combine(Application.dataPath, excelRoot.Substring("Assets/".Length));
            }
            else if (excelRoot.StartsWith("Assets/../"))
            {
                fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", excelRoot.Substring("Assets/../".Length)));
            }
            
            if (!Directory.Exists(fullPath)) return;
            
            foreach (var file in Directory.GetFiles(fullPath, "*.xlsx", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file).StartsWith("~")) continue;
                
                var info = new ExcelFileInfo
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    ModifiedTime = File.GetLastWriteTime(file),
                    Configs = ParseExcelConfigs(file)
                };
                _excelFiles.Add(info);
            }
            
            // 初始化勾选状态
            InitializeCheckStates();
        }
        
        /// <summary>
        /// 初始化Excel勾选状态
        /// 从EditorPrefs加载，如果没有保存则默认全选
        /// </summary>
        private void InitializeCheckStates()
        {
            foreach (var excel in _excelFiles)
            {
                var key = GetCheckStateKey(excel.FilePath);
                
                // 如果之前没有保存过状态，默认为true（全选）
                if (!_excelCheckStates.ContainsKey(excel.FilePath))
                {
                    _excelCheckStates[excel.FilePath] = EditorPrefs.GetBool(key, true);
                }
            }
        }
        
        /// <summary>
        /// 获取勾选状态存储的键名
        /// </summary>
        private string GetCheckStateKey(string filePath)
        {
            // 使用文件名作为键，避免路径变化导致状态丢失
            var fileName = Path.GetFileName(filePath);
            return EditorPrefsKeyPrefix + fileName;
        }
        
        /// <summary>
        /// 保存勾选状态到EditorPrefs
        /// </summary>
        private void SaveCheckState(string filePath, bool isChecked)
        {
            var key = GetCheckStateKey(filePath);
            EditorPrefs.SetBool(key, isChecked);
        }
        
        /// <summary>
        /// 刷新JSON文件列表
        /// </summary>
        private void RefreshJsonList()
        {
            _generatedJsonFiles.Clear();
            
            if (_settings == null) return;
            
            var folder = _settings.JsonOutputPath;
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
            
            _generatedJsonFiles = Directory.GetFiles(folder, "*.json").ToList();
        }
        
        /// <summary>
        /// 刷新C#文件列表
        /// </summary>
        private void RefreshCSharpList()
        {
            _generatedCSharpFiles.Clear();
            
            if (_settings == null) return;
            
            var folder = _settings.CSharpOutputPath;
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
            
            _generatedCSharpFiles = Directory.GetFiles(folder, "*.cs").ToList();
        }
        
        /// <summary>
        /// 解析Excel配置名称和备注
        /// </summary>
        private List<ConfigInfo> ParseExcelConfigs(string excelPath)
        {
            var configs = new List<ConfigInfo>();
            
            try
            {
                using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    do
                    {
                        if (reader.Name.StartsWith("#"))
                        {
                            // Sheet名格式：#ConfigName#备注
                            var parts = reader.Name.TrimStart('#').Split('#');
                            var configInfo = new ConfigInfo
                            {
                                Name = parts.Length > 0 ? parts[0] : "",
                                Remark = parts.Length > 1 ? parts[1] : ""
                            };
                            configs.Add(configInfo);
                        }
                    } while (reader.NextResult());
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"解析Excel文件失败: {excelPath}\n{ex.Message}");
            }
            
            return configs;
        }
        
        /// <summary>
        /// 打开Excel文件
        /// </summary>
        private void OpenExcelFile(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("打开失败", $"无法打开Excel文件:\n{ex.Message}", "确定");
            }
        }
        
        /// <summary>
        /// 打开目录
        /// </summary>
        private void OpenDirectory(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                EditorUtility.DisplayDialog("提示", "目录路径为空", "确定");
                return;
            }
            
            // 转换相对路径为绝对路径
            var fullPath = directoryPath;
            if (directoryPath.StartsWith("Assets/"))
            {
                fullPath = Path.Combine(Application.dataPath, directoryPath.Substring("Assets/".Length));
            }
            else if (directoryPath.StartsWith("Assets/../"))
            {
                fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", directoryPath.Substring("Assets/../".Length)));
            }
            
            if (!Directory.Exists(fullPath))
            {
                var result = EditorUtility.DisplayDialog("目录不存在", 
                    $"目录不存在:\n{fullPath}\n\n是否创建该目录？", 
                    "创建", "取消");
                    
                if (result)
                {
                    try
                    {
                        Directory.CreateDirectory(fullPath);
                        EditorUtility.DisplayDialog("成功", "目录创建成功！", "确定");
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.DisplayDialog("创建失败", $"无法创建目录:\n{ex.Message}", "确定");
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            
            try
            {
                // 在文件浏览器中打开目录内部
                System.Diagnostics.Process.Start(fullPath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("打开失败", $"无法打开目录:\n{ex.Message}", "确定");
            }
        }
        
        /// <summary>
        /// 聚焦资源
        /// </summary>
        private void FocusAsset(string assetPath)
        {
            // 转换为项目相对路径
            var projectPath = Path.GetFullPath(Application.dataPath + "/..");
            var relativePath = assetPath;
            
            if (assetPath.StartsWith(projectPath))
            {
                relativePath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                relativePath = relativePath.Replace("\\", "/");
            }
            
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }
        
        /// <summary>
        /// 浏览Excel根目录（支持项目外路径）
        /// </summary>
        private void BrowseExcelRootFolder()
        {
            var currentPath = _settings.ExcelRootPath ?? "Assets";
            var selectedPath = EditorUtility.OpenFolderPanel("选择Excel根目录", currentPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // 始终转换为相对路径
                var projectPath = Path.GetFullPath(Application.dataPath + "/..");
                projectPath = projectPath.Replace("\\", "/");
                selectedPath = selectedPath.Replace("\\", "/");
                
                if (selectedPath.StartsWith(projectPath))
                {
                    // 项目内路径：转换为 Assets/... 格式
                    var dataPath = Application.dataPath.Replace("\\", "/");
                    if (selectedPath.Length >= dataPath.Length)
                    {
                        selectedPath = "Assets" + selectedPath.Substring(dataPath.Length);
                    }
                    else
                    {
                        // 如果选择的是项目根或更上层，使用相对路径
                        var relativePath = Path.GetRelativePath(projectPath, selectedPath);
                        relativePath = relativePath.Replace("\\", "/");
                        selectedPath = "Assets/../" + relativePath + "/";
                    }
                }
                else
                {
                    // 项目外路径：转换为相对路径 Assets/../Excel/ 格式
                    var relativePath = Path.GetRelativePath(projectPath, selectedPath);
                    relativePath = relativePath.Replace("\\", "/");
                    selectedPath = "Assets/../" + relativePath + "/";
                }
                
                _settings.ExcelRootPath = selectedPath;
                RefreshExcelList();
            }
        }
        
        /// <summary>
        /// 浏览文件夹（仅项目内）
        /// </summary>
        private void BrowseFolder(string currentPath, System.Action<string> onSelected)
        {
            var selectedPath = EditorUtility.OpenFolderPanel("选择目录", currentPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                var projectPath = Path.GetFullPath(Application.dataPath + "/..");
                if (selectedPath.StartsWith(projectPath))
                {
                    selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    selectedPath = selectedPath.Replace("\\", "/");
                    onSelected?.Invoke(selectedPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请选择项目内的目录", "确定");
                }
            }
        }
        
        /// <summary>
        /// 检查配置是否完整
        /// 检查最终值（包括默认值）
        /// </summary>
        private bool IsConfigurationValid(out string errorMessage)
        {
            errorMessage = null;
            
            // 检查Excel根目录（包括默认值）
            var excelRootPath = _settings.ExcelRootPath;
            if (string.IsNullOrEmpty(excelRootPath))
            {
                errorMessage = "Excel根目录未配置";
                return false;
            }
            
            // 检查JSON输出目录（包括默认值）
            var jsonOutputPath = _settings.JsonOutputPath;
            if (string.IsNullOrEmpty(jsonOutputPath))
            {
                errorMessage = "JSON输出目录未配置";
                return false;
            }
            
            // 检查C#输出目录（包括默认值）
            var csharpOutputPath = _settings.CSharpOutputPath;
            if (string.IsNullOrEmpty(csharpOutputPath))
            {
                errorMessage = "C#输出目录未配置";
                return false;
            }
            
            // 检查命名空间（包括默认值）
            var defaultNamespace = _settings.DefaultNamespace;
            if (string.IsNullOrEmpty(defaultNamespace))
            {
                errorMessage = "默认命名空间未配置";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 导出所有配置
        /// 如果有选中的Excel，则只导出选中的；否则导出全部
        /// </summary>
        private void ExportAllConfigs()
        {
            // 检查配置完整性
            if (!IsConfigurationValid(out string errorMessage))
            {
                EditorUtility.DisplayDialog("配置不完整", $"无法导出配置:\n{errorMessage}\n\n请在配置区域完成必填项的设置", "确定");
                return;
            }
            
            // 统计选中的Excel数量
            var selectedExcels = _excelFiles.Where(e => _excelCheckStates.ContainsKey(e.FilePath) && _excelCheckStates[e.FilePath]).ToList();
            
            string confirmMessage;
            if (selectedExcels.Count > 0)
            {
                confirmMessage = $"确定要导出选中的 {selectedExcels.Count} 个Excel配置吗？";
            }
            else
            {
                confirmMessage = $"未选中任何Excel，确定要导出所有 {_excelFiles.Count} 个Excel配置吗？";
            }
            
            if (!EditorUtility.DisplayDialog("确认导出", confirmMessage, "确定", "取消"))
            {
                return;
            }
            
            try
            {
                if (selectedExcels.Count > 0)
                {
                    // 只导出选中的Excel
                    ExportSelectedExcels(selectedExcels);
                }
                else
                {
                    // 导出所有Excel
                    ExcelConfigGenerator.GenerateAll();
                }
                
                // 刷新列表
                RefreshAllLists();
                
                EditorUtility.DisplayDialog("导出成功", $"成功导出 {(selectedExcels.Count > 0 ? selectedExcels.Count : _excelFiles.Count)} 个Excel配置", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("导出失败", $"导出配置失败:\n{ex.Message}", "确定");
                Debug.LogError(ex);
            }
        }
        
        /// <summary>
        /// 快速导出所有Excel（不管选中状态）
        /// </summary>
        private void QuickExportAll()
        {
            // 检查配置完整性
            if (!IsConfigurationValid(out string errorMessage))
            {
                EditorUtility.DisplayDialog("配置不完整", $"无法导出配置:\n{errorMessage}\n\n请在配置区域完成必填项的设置", "确定");
                return;
            }
            
            try
            {
                // 直接导出所有Excel（内部会显示成功弹窗）
                ExcelConfigGenerator.GenerateAll();
                
                // 刷新列表
                RefreshAllLists();
            }
            catch (Exception ex)
            {
                // GenerateAll内部已处理错误弹窗，这里只记录日志
                Debug.LogError(ex);
            }
        }
        
        /// <summary>
        /// 导出选中的Excel文件
        /// </summary>
        private void ExportSelectedExcels(List<ExcelFileInfo> selectedExcels)
        {
            foreach (var excel in selectedExcels)
            {
                try
                {
                    Debug.Log($"正在导出: {excel.FileName}");
                    // 调用ExcelConfigGenerator的单个文件导出功能
                    ExcelConfigGenerator.GenerateFromFile(excel.FilePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"导出 {excel.FileName} 失败: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 导出单个Excel文件
        /// </summary>
        private void ExportSingleExcel(ExcelFileInfo excelInfo)
        {
            // 检查配置完整性
            if (!IsConfigurationValid(out string errorMessage))
            {
                EditorUtility.DisplayDialog("配置不完整", $"无法导出配置:\n{errorMessage}\n\n请在配置区域完成必填项的设置", "确定");
                return;
            }
            
            if (!EditorUtility.DisplayDialog("确认导出", $"确定要导出 {excelInfo.FileName} 吗？", "确定", "取消"))
            {
                return;
            }
            
            try
            {
                Debug.Log($"正在导出: {excelInfo.FileName}");
                ExcelConfigGenerator.GenerateFromFile(excelInfo.FilePath);
                
                // 刷新列表
                RefreshAllLists();
                
                EditorUtility.DisplayDialog("导出成功", $"成功导出 {excelInfo.FileName}", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("导出失败", $"导出配置失败:\n{ex.Message}", "确定");
                Debug.LogError(ex);
            }
        }
        
        /// <summary>
        /// 配置信息
        /// </summary>
        private class ConfigInfo
        {
            public string Name;        // 配置名称
            public string Remark;      // 备注（从Sheet名解析）
        }
        
        /// <summary>
        /// Excel文件信息
        /// </summary>
        private class ExcelFileInfo
        {
            public string FilePath;
            public string FileName;
            public DateTime ModifiedTime;
            public List<ConfigInfo> Configs = new List<ConfigInfo>();
        }
    }
}
#endif

