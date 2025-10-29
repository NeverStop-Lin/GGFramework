#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.Core
{
    /// <summary>
    /// 框架配置管理菜单
    /// </summary>
    public static class FrameworkSettingsMenu
    {
        [MenuItem("Framework/设置/打开配置索引", false, 1)]
        public static void OpenSettingsIndex()
        {
            var index = FrameworkSettingsIndex.Instance;
            if (index == null)
            {
                if (EditorUtility.DisplayDialog(
                    "配置索引不存在",
                    "配置索引文件尚未创建\n是否现在创建？",
                    "创建",
                    "取消"))
                {
                    index = FrameworkSettingsIndex.GetOrCreate();
                    Selection.activeObject = index;
                    EditorGUIUtility.PingObject(index);
                }
            }
            else
            {
                Selection.activeObject = index;
                EditorGUIUtility.PingObject(index);
            }
        }
        
        [MenuItem("Framework/设置/重置所有配置", false, 2)]
        public static void ResetAllSettings()
        {
            if (!FrameworkSettingsIndex.Exists())
            {
                EditorUtility.DisplayDialog("提示", "配置索引文件不存在，无需重置", "确定");
                return;
            }
            
            if (EditorUtility.DisplayDialog(
                "确认重置",
                "确定要删除配置索引文件吗？\n\n" +
                "这将清除所有工具的配置关联\n" +
                "下次打开工具时需要重新配置\n\n" +
                "注意：这不会删除实际的配置文件（如UIManagerSettings.asset），\n" +
                "只是删除索引关联",
                "确定删除",
                "取消"))
            {
                if (FrameworkSettingsIndex.DeleteIndex())
                {
                    EditorUtility.DisplayDialog("成功", "配置索引已删除\n下次打开工具时将重新引导配置", "确定");
                }
            }
        }
        
        // 已移除：Excel 生成器配置的直达入口，统一改为通过 Excel 导表工具窗口进入
        
        [MenuItem("Framework/设置/验证配置完整性", false, 20)]
        public static void ValidateSettings()
        {
            var index = FrameworkSettingsIndex.Instance;
            if (index == null)
            {
                EditorUtility.DisplayDialog("验证失败", "配置索引文件不存在", "确定");
                return;
            }
            
            var errorMessages = new System.Collections.Generic.List<string>();
            
            // 验证UI管理器配置
            if (!index.ValidateUIManagerSettings(out string uiError))
            {
                errorMessages.Add($"UI管理器: {uiError}");
            }
            else
            {
                var uiSettings = index.UIManagerSettings;
                if (!uiSettings.Validate(out string detailError))
                {
                    errorMessages.Add($"UI管理器: {detailError}");
                }
            }
            
            // 验证Excel生成器配置
            if (!index.ValidateExcelGeneratorSettings(out string excelError))
            {
                errorMessages.Add($"Excel生成器: {excelError}");
            }
            else
            {
                var excelSettings = index.ExcelGeneratorSettings;
                if (!excelSettings.Validate(out string detailError))
                {
                    errorMessages.Add($"Excel生成器: {detailError}");
                }
            }
            
            if (errorMessages.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "配置验证失败",
                    "发现以下配置问题：\n\n" + string.Join("\n", errorMessages),
                    "确定");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "验证成功",
                    "所有配置验证通过 ✓\n\n" +
                    "• UI管理器配置完整\n" +
                    "• Excel生成器配置完整",
                    "确定");
            }
        }
    }
}
#endif

