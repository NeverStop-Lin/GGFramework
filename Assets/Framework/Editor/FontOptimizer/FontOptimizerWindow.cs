using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.FontOptimizer
{
    /// <summary>
    /// 字体优化工具 - 纯C#实现版本（无需Node.js）
    /// </summary>
    public class FontOptimizerWindow : EditorWindow
    {
        private string _sourcePath = "";
        private string _outputPath = "检测到的字符.txt";
        private bool _isProcessing = false;
        private string _logMessage = "";
        private Vector2 _scrollPosition;
        private HashSet<char> _detectedChars = new HashSet<char>();

        [MenuItem("Tools/字体优化工具（纯C#版）")]
        public static void ShowWindow()
        {
            var window = GetWindow<FontOptimizerWindow>("字体优化(C#)");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnEnable()
        {
            _sourcePath = Application.dataPath;
            _outputPath = Path.Combine(Application.dataPath, "../检测到的字符.txt");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("字体优化工具 - 文字提取器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "纯C#实现，无需Node.js。扫描项目提取所有使用的中文字符。\n" +
                "支持: C#代码、Unity场景、Prefab、文本资源",
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
                var path = EditorUtility.OpenFolderPanel("选择扫描路径", _sourcePath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _sourcePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("输出文件:", GUILayout.Width(80));
            _outputPath = EditorGUILayout.TextField(_outputPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.SaveFilePanel("保存文件", Path.GetDirectoryName(_outputPath), "检测到的字符.txt", "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    _outputPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 统计信息
            if (_detectedChars.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"检测到的字符数: {_detectedChars.Count}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("字符预览:", EditorStyles.miniLabel);
                
                var preview = string.Join("", _detectedChars.Take(100));
                if (_detectedChars.Count > 100) preview += "...";
                
                EditorGUILayout.TextArea(preview, GUILayout.Height(40));
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(_isProcessing);
            if (GUILayout.Button(_isProcessing ? "扫描中..." : "开始扫描", GUILayout.Height(35)))
            {
                StartScan();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_detectedChars.Count == 0);
            if (GUILayout.Button("导出结果", GUILayout.Height(35)))
            {
                ExportResult();
            }
            if (GUILayout.Button("复制到剪贴板", GUILayout.Height(35)))
            {
                CopyToClipboard();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 日志区域
            EditorGUILayout.LabelField("日志输出", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box", GUILayout.Height(250));
            EditorGUILayout.TextArea(_logMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            // 底部按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空日志"))
            {
                _logMessage = "";
                _detectedChars.Clear();
            }
            if (GUILayout.Button("打开输出目录"))
            {
                if (File.Exists(_outputPath))
                {
                    EditorUtility.RevealInFinder(_outputPath);
                }
                else
                {
                    EditorUtility.RevealInFinder(Path.GetDirectoryName(_outputPath));
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void StartScan()
        {
            _isProcessing = true;
            _detectedChars.Clear();
            _logMessage = $"[{DateTime.Now:HH:mm:ss}] 开始扫描...\n";
            _logMessage += $"扫描路径: {_sourcePath}\n\n";

            try
            {
                // 扫描C#文件
                ScanCSharpFiles();

                // 扫描文本资源
                ScanTextAssets();

                // 扫描场景和Prefab
                ScanUnityAssets();

                _logMessage += $"\n[{DateTime.Now:HH:mm:ss}] 扫描完成！\n";
                _logMessage += $"检测到 {_detectedChars.Count} 个不同的字符\n";
            }
            catch (Exception e)
            {
                _logMessage += $"\n[错误] {e.Message}\n";
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void ScanCSharpFiles()
        {
            var csFiles = Directory.GetFiles(_sourcePath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\Editor\\") && !f.Contains("/Editor/"))
                .ToList();

            _logMessage += $"[{DateTime.Now:HH:mm:ss}] 扫描 C# 文件: {csFiles.Count} 个\n";

            foreach (var file in csFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    ExtractChineseFromCSharp(content);
                }
                catch (Exception e)
                {
                    _logMessage += $"  [错误] {Path.GetFileName(file)}: {e.Message}\n";
                }
            }

            _logMessage += $"  完成，累计字符: {_detectedChars.Count}\n";
        }

        private void ScanTextAssets()
        {
            var textFiles = Directory.GetFiles(_sourcePath, "*.txt", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(_sourcePath, "*.json", SearchOption.AllDirectories))
                .ToList();

            _logMessage += $"[{DateTime.Now:HH:mm:ss}] 扫描文本资源: {textFiles.Count} 个\n";

            foreach (var file in textFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    ExtractChinese(content);
                }
                catch (Exception e)
                {
                    _logMessage += $"  [错误] {Path.GetFileName(file)}: {e.Message}\n";
                }
            }

            _logMessage += $"  完成，累计字符: {_detectedChars.Count}\n";
        }

        private void ScanUnityAssets()
        {
            var assetFiles = Directory.GetFiles(_sourcePath, "*.prefab", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(_sourcePath, "*.unity", SearchOption.AllDirectories))
                .ToList();

            _logMessage += $"[{DateTime.Now:HH:mm:ss}] 扫描 Unity 资源: {assetFiles.Count} 个\n";

            foreach (var file in assetFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    ExtractChinese(content);
                }
                catch (Exception e)
                {
                    _logMessage += $"  [错误] {Path.GetFileName(file)}: {e.Message}\n";
                }
            }

            _logMessage += $"  完成，累计字符: {_detectedChars.Count}\n";
        }

        private void ExtractChineseFromCSharp(string content)
        {
            // 移除注释
            content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);
            content = Regex.Replace(content, @"/\*[\s\S]*?\*/", "");

            // 移除Debug.Log
            content = Regex.Replace(content, @"Debug\.Log[A-Za-z]*\([^)]*\)", "");

            // 提取字符串字面量
            var matches = Regex.Matches(content, @"""([^""]*)""");
            foreach (Match match in matches)
            {
                ExtractChinese(match.Groups[1].Value);
            }
        }

        private void ExtractChinese(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            foreach (var c in text)
            {
                // 只保留中文字符
                if (c >= 0x4E00 && c <= 0x9FA5)
                {
                    _detectedChars.Add(c);
                }
            }
        }

        private void ExportResult()
        {
            try
            {
                var defaultChars = LoadDefaultChars();
                var allChars = defaultChars + string.Join("", _detectedChars.OrderBy(c => c));

                File.WriteAllText(_outputPath, allChars, Encoding.UTF8);

                _logMessage += $"\n[{DateTime.Now:HH:mm:ss}] 已导出到: {_outputPath}\n";
                _logMessage += $"包含字符数: {allChars.Length}\n";

                EditorUtility.DisplayDialog("成功", $"已导出 {allChars.Length} 个字符到:\n{_outputPath}", "确定");
                EditorUtility.RevealInFinder(_outputPath);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"导出失败: {e.Message}", "确定");
            }
        }

        private void CopyToClipboard()
        {
            var text = string.Join("", _detectedChars.OrderBy(c => c));
            GUIUtility.systemCopyBuffer = text;
            _logMessage += $"\n[{DateTime.Now:HH:mm:ss}] 已复制 {text.Length} 个字符到剪贴板\n";
            EditorUtility.DisplayDialog("成功", $"已复制 {text.Length} 个字符到剪贴板", "确定");
        }

        private string LoadDefaultChars()
        {
            // 默认字符（数字、字母、标点符号）
            return "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
                   "！＃＄％＆＇（）＊＋，－．／０１２３４５６７８９：；＜＝＞？＠" +
                   "ＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯＰＱＲＳＴＵＶＷＸＹＺ［＼］＾＿｀" +
                   "ａｂｃｄｅｆｇｈｉｊｋｌｍｎｏｐｑｒｓｔｕｖｗｘｙｚ｛｜｝～";
        }
    }
}

