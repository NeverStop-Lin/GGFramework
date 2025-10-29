#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using Framework.Core;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI项目配置代码生成器
    /// 将配置数据生成为 C# 代码文件
    /// </summary>
    public static class UIProjectConfigCodeGenerator
    {
        /// <summary>
        /// 生成配置代码文件
        /// </summary>
        /// <param name="config">配置数据</param>
        /// <param name="outputPath">输出路径（相对于Assets）</param>
        /// <param name="namespace">命名空间</param>
        public static void GenerateCode(UIProjectConfig config, string outputPath, string @namespace = "Framework.Core")
        {
            if (config == null)
            {
                Debug.LogError("[UIProjectConfigCodeGenerator] 配置为空");
                return;
            }
            
            // 确保输出目录存在
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 生成代码
            var code = GenerateCodeContent(config, @namespace);
            
            // 写入文件
            File.WriteAllText(outputPath, code, Encoding.UTF8);
            
            // 刷新资源数据库
            AssetDatabase.Refresh();
            
            Debug.Log($"[UIProjectConfigCodeGenerator] 配置代码已生成: {outputPath}");
        }
        
        /// <summary>
        /// 生成代码内容
        /// </summary>
        private static string GenerateCodeContent(UIProjectConfig config, string @namespace)
        {
            var sb = new StringBuilder();
            
            // 文件头
            sb.AppendLine("// ========================================");
            sb.AppendLine("// 自动生成的UI项目配置数据");
            sb.AppendLine("// 警告: 请勿手动修改此文件，所有修改将在重新生成时丢失");
            sb.AppendLine("// ========================================");
            sb.AppendLine();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Framework.Core;");
            sb.AppendLine();
            sb.AppendLine($"namespace {@namespace}");
            sb.AppendLine("{");
            
            // 类定义 - partial class
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// UI项目配置数据（自动生成）");
            sb.AppendLine("    /// 通过 partial 方法实现配置注入");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static partial class UIProjectConfigData");
            sb.AppendLine("    {");
            
            // 实现分辨率 partial 方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 提供外部分辨率配置");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        static partial void GetResolutionExternal(ref int width, ref int height, ref float match)");
            sb.AppendLine("        {");
            sb.AppendLine($"            width = {config.ReferenceResolutionWidth};");
            sb.AppendLine($"            height = {config.ReferenceResolutionHeight};");
            sb.AppendLine($"            match = {config.MatchWidthOrHeight}f;");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 实现层级定义 partial 方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 提供外部层级定义配置");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        static partial void GetLayerDefinitionsExternal(ref List<UILayerDefinition> layers)");
            sb.AppendLine("        {");
            sb.AppendLine("            layers = new List<UILayerDefinition>");
            sb.AppendLine("            {");
            
            foreach (var layer in config.LayerDefinitions)
            {
                sb.AppendLine("                new UILayerDefinition");
                sb.AppendLine("                {");
                sb.AppendLine($"                    LayerName = \"{EscapeString(layer.LayerName)}\",");
                sb.AppendLine($"                    BaseSortingOrder = {layer.BaseSortingOrder},");
                sb.AppendLine($"                    Description = \"{EscapeString(layer.Description)}\"");
                sb.AppendLine("                },");
            }
            
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 实现UI配置 partial 方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 提供外部UI实例配置");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        static partial void GetUIConfigsExternal(ref List<UIInstanceConfig> configs)");
            sb.AppendLine("        {");
            sb.AppendLine("            configs = new List<UIInstanceConfig>");
            sb.AppendLine("            {");
            
            foreach (var uiConfig in config.UIConfigs)
            {
                sb.AppendLine("                new UIInstanceConfig");
                sb.AppendLine("                {");
                sb.AppendLine($"                    UIName = \"{EscapeString(uiConfig.UIName)}\",");
                sb.AppendLine($"                    ResourcePath = \"{EscapeString(uiConfig.ResourcePath)}\",");
                sb.AppendLine($"                    LayerName = \"{EscapeString(uiConfig.LayerName)}\",");
                sb.AppendLine($"                    CacheStrategy = UICacheStrategy.{uiConfig.CacheStrategy},");
                sb.AppendLine($"                    Preload = {uiConfig.Preload.ToString().ToLower()},");
                sb.AppendLine($"                    InstanceStrategy = UIInstanceStrategy.{uiConfig.InstanceStrategy},");
                sb.AppendLine($"                    LogicScriptPath = \"{EscapeString(uiConfig.LogicScriptPath)}\"");
                sb.AppendLine("                },");
            }
            
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            
            // 类结束
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 转义字符串（处理引号、换行等）
        /// </summary>
        private static string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return "";
                
            return str.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r");
        }
    }
}
#endif

