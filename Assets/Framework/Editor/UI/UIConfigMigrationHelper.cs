#if UNITY_EDITOR
using System.IO;
using Framework.Core;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI配置迁移辅助工具
    /// 用于更新现有UI配置，添加脚本路径信息
    /// </summary>
    public static class UIConfigMigrationHelper
    {
        [MenuItem("Tools/UI工具/更新配置-添加脚本路径")]
        public static void MigrateUIConfigWithScriptPaths()
        {
            var settings = UIManagerSettings.Instance;
            if (settings == null)
            {
                Debug.LogError("[UIConfigMigration] 未找到 UIManagerSettings");
                return;
            }
            
            // 获取当前配置
            var config = UIProjectConfigEditorHelper.GetConfig();
            if (config == null || config.UIConfigs == null)
            {
                Debug.LogError("[UIConfigMigration] 未找到UI配置");
                return;
            }
            
            int updatedCount = 0;
            int notFoundCount = 0;
            
            // 遍历所有UI配置，补充脚本路径
            foreach (var uiConfig in config.UIConfigs)
            {
                if (string.IsNullOrEmpty(uiConfig.LogicScriptPath))
                {
                    // 确定脚本路径
                    var logicFilePath = Path.Combine(settings.LogicScriptOutputPath, $"{uiConfig.UIName}.cs");
                    
                    // 检查文件是否存在
                    if (File.Exists(logicFilePath))
                    {
                        // 转换为相对路径
                        uiConfig.LogicScriptPath = ConvertToRelativePath(logicFilePath);
                        updatedCount++;
                        Debug.Log($"[UIConfigMigration] 已添加脚本路径: {uiConfig.UIName} -> {uiConfig.LogicScriptPath}");
                    }
                    else
                    {
                        notFoundCount++;
                        Debug.LogWarning($"[UIConfigMigration] 未找到脚本文件: {uiConfig.UIName} ({logicFilePath})");
                    }
                }
            }
            
            // 保存配置
            if (updatedCount > 0)
            {
                UIProjectConfigEditorHelper.SaveConfig(config);
                Debug.Log($"[UIConfigMigration] 配置更新完成: 已更新 {updatedCount} 个，未找到 {notFoundCount} 个");
                EditorUtility.DisplayDialog(
                    "配置更新完成", 
                    $"已为 {updatedCount} 个UI添加脚本路径信息\n未找到脚本文件: {notFoundCount} 个", 
                    "确定"
                );
            }
            else
            {
                Debug.Log("[UIConfigMigration] 没有需要更新的配置");
                EditorUtility.DisplayDialog("配置更新", "所有UI配置都已包含脚本路径信息", "确定");
            }
        }
        
        /// <summary>
        /// 将绝对路径或Assets路径转换为相对于项目根目录的相对路径
        /// </summary>
        private static string ConvertToRelativePath(string path)
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
    }
}
#endif

