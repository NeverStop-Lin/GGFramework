using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Framework.Editor.FontOptimizer
{
    /// <summary>
    /// 字体优化工具 - Unity编辑器插件
    /// 功能：扫描项目文件，提取使用的文字，生成精简字体
    /// </summary>
    public class FontOptimizerEditor : EditorWindow
    {
        private string _sourcePath = "../../Client";
        private string _fontPath = "../../Tools/font-edit/font/Regular.ttf";
        private string _outputPath = "Assets/Resources";
        private bool _isProcessing = false;
        private string _logMessage = "";
        private Vector2 _scrollPosition;

        [MenuItem("Framework/字体优化/字体优化工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<FontOptimizerEditor>("字体优化");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 初始化默认路径
            _sourcePath = Application.dataPath.Replace("/Assets", "");
            _outputPath = "Assets/Resources";
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("字体优化工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "自动扫描项目中的所有文字，生成精简字体文件，大幅减小包体体积。\n" +
                "支持: C#代码、Excel配置、FairyGUI、Unity场景和Prefab",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // 配置区域
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("配置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("扫描路径:", GUILayout.Width(80));
            _sourcePath = EditorGUILayout.TextField(_sourcePath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("选择项目路径", _sourcePath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _sourcePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("原始字体:", GUILayout.Width(80));
            _fontPath = EditorGUILayout.TextField(_fontPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFilePanel("选择字体文件", Path.GetDirectoryName(_fontPath), "ttf,otf");
                if (!string.IsNullOrEmpty(path))
                {
                    _fontPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("输出路径:", GUILayout.Width(80));
            _outputPath = EditorGUILayout.TextField(_outputPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("选择输出路径", _outputPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    // 转换为相对路径
                    if (path.StartsWith(Application.dataPath))
                    {
                        _outputPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        _outputPath = path;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 操作按钮
            EditorGUI.BeginDisabledGroup(_isProcessing);
            if (GUILayout.Button(_isProcessing ? "优化中..." : "开始优化字体", GUILayout.Height(35)))
            {
                StartOptimization();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // 日志区域
            EditorGUILayout.LabelField("日志输出", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box", GUILayout.Height(200));
            EditorGUILayout.TextArea(_logMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            // 底部按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空日志"))
            {
                _logMessage = "";
            }
            if (GUILayout.Button("打开font-edit目录"))
            {
                OpenFontEditFolder();
            }
            if (GUILayout.Button("查看帮助文档"))
            {
                OpenHelpDocument();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void StartOptimization()
        {
            // 验证路径
            if (!Directory.Exists(_sourcePath))
            {
                EditorUtility.DisplayDialog("错误", "扫描路径不存在！", "确定");
                return;
            }

            if (!File.Exists(_fontPath))
            {
                EditorUtility.DisplayDialog("错误", "字体文件不存在！", "确定");
                return;
            }

            _isProcessing = true;
            _logMessage = $"[{DateTime.Now:HH:mm:ss}] 开始优化字体...\n";
            _logMessage += $"扫描路径: {_sourcePath}\n";
            _logMessage += $"字体文件: {_fontPath}\n";
            _logMessage += $"输出路径: {_outputPath}\n\n";

            // 检查 font-edit 工具是否存在
            var fontEditPath = Path.Combine(Application.dataPath, "../Tools/font-edit");
            if (!Directory.Exists(fontEditPath))
            {
                _logMessage += "[错误] font-edit 工具不存在！\n";
                _logMessage += "请确保 Tools/font-edit 目录存在。\n";
                _isProcessing = false;
                return;
            }

            // 更新配置文件
            UpdateConfigFile(fontEditPath);

            // 运行 npm start
            RunFontEditTool(fontEditPath);
        }

        private void UpdateConfigFile(string fontEditPath)
        {
            var configPath = Path.Combine(fontEditPath, "config.json");
            var config = $@"{{
    ""source"": ""{_sourcePath.Replace("\\", "/")}"",
    ""font"": ""{_fontPath.Replace("\\", "/")}"",
    ""font_output"": ""{Path.GetFullPath(_outputPath).Replace("\\", "/")}""
}}";

            File.WriteAllText(configPath, config);
            _logMessage += $"[{DateTime.Now:HH:mm:ss}] 配置文件已更新\n";
        }

        private void RunFontEditTool(string fontEditPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npm start",
                    WorkingDirectory = fontEditPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = startInfo };

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        _logMessage += $"[{DateTime.Now:HH:mm:ss}] {args.Data}\n";
                        Repaint();
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        _logMessage += $"[错误] {args.Data}\n";
                        Repaint();
                    }
                };

                process.Exited += (sender, args) =>
                {
                    _isProcessing = false;
                    _logMessage += $"\n[{DateTime.Now:HH:mm:ss}] 优化完成！\n";
                    _logMessage += "请检查输出目录的字体文件。\n";
                    AssetDatabase.Refresh();
                    Repaint();
                };

                process.EnableRaisingEvents = true;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _logMessage += $"[{DateTime.Now:HH:mm:ss}] 正在运行 font-edit 工具...\n";
            }
            catch (Exception e)
            {
                _logMessage += $"[错误] 运行失败: {e.Message}\n";
                _logMessage += "请确保已安装 Node.js 和 npm。\n";
                _isProcessing = false;
            }
        }

        private void OpenFontEditFolder()
        {
            var fontEditPath = Path.Combine(Application.dataPath, "../Tools/font-edit");
            if (Directory.Exists(fontEditPath))
            {
                var fullPath = Path.GetFullPath(fontEditPath);
                System.Diagnostics.Process.Start(fullPath);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "font-edit 目录不存在", "确定");
            }
        }

        private void OpenHelpDocument()
        {
            var helpPath = Path.Combine(Application.dataPath, "../Tools/工具说明文档.md");
            if (File.Exists(helpPath))
            {
                Application.OpenURL($"file://{helpPath}");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "帮助文档不存在", "确定");
            }
        }
    }
}

