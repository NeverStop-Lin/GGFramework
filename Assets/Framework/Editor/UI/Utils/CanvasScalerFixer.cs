#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Framework.Core;

namespace Framework.Editor.UI
{
    /// <summary>
    /// Canvas Scaler修复工具
    /// 用于检查和修复UI预制体的Canvas Scaler配置，确保与项目设置一致
    /// </summary>
    public static class CanvasScalerFixer
    {
        /// <summary>
        /// 检查预制体的Canvas Scaler配置是否与项目设置一致
        /// </summary>
        public static bool CheckCanvasScaler(GameObject prefab, out string errorMessage)
        {
            errorMessage = null;
            
            if (prefab == null)
            {
                errorMessage = "Prefab为空";
                return false;
            }
            
            // 获取项目配置
            var config = UIProjectConfigManager.GetConfig();
            if (config == null)
            {
                errorMessage = "未找到UI项目配置";
                return false;
            }
            
            // 查找Canvas Scaler
            var canvasScaler = prefab.GetComponentInChildren<CanvasScaler>();
            if (canvasScaler == null)
            {
                errorMessage = "未找到Canvas Scaler组件";
                return false;
            }
            
            // 检查配置是否一致
            bool isConsistent = true;
            var issues = new System.Collections.Generic.List<string>();
            
            // 检查UI Scale Mode
            if (canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                issues.Add($"UI Scale Mode不正确: {canvasScaler.uiScaleMode}，应为 ScaleWithScreenSize");
                isConsistent = false;
            }
            
            // 检查参考分辨率
            var refResolution = canvasScaler.referenceResolution;
            if (Mathf.Abs(refResolution.x - config.ReferenceResolutionWidth) > 0.1f ||
                Mathf.Abs(refResolution.y - config.ReferenceResolutionHeight) > 0.1f)
            {
                issues.Add($"参考分辨率不一致: {refResolution.x}x{refResolution.y}，应为 {config.ReferenceResolutionWidth}x{config.ReferenceResolutionHeight}");
                isConsistent = false;
            }
            
            // 检查匹配模式
            if (Mathf.Abs(canvasScaler.matchWidthOrHeight - config.MatchWidthOrHeight) > 0.01f)
            {
                issues.Add($"屏幕匹配模式不一致: {canvasScaler.matchWidthOrHeight}，应为 {config.MatchWidthOrHeight}");
                isConsistent = false;
            }
            
            if (!isConsistent)
            {
                errorMessage = string.Join("\n", issues);
            }
            
            return isConsistent;
        }
        
        /// <summary>
        /// 修复预制体的Canvas Scaler配置
        /// </summary>
        public static bool FixCanvasScaler(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[CanvasScalerFixer] Prefab为空");
                return false;
            }
            
            // 获取项目配置
            var config = UIProjectConfigManager.GetConfig();
            if (config == null)
            {
                Debug.LogError("[CanvasScalerFixer] 未找到UI项目配置");
                return false;
            }
            
            try
            {
                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                
                // 查找或创建Canvas Scaler
                var canvasScaler = prefabRoot.GetComponentInChildren<CanvasScaler>();
                var canvas = prefabRoot.GetComponent<Canvas>();
                
                // 如果没有Canvas，尝试在子物体中查找
                if (canvas == null)
                {
                    canvas = prefabRoot.GetComponentInChildren<Canvas>();
                }
                
                if (canvas == null)
                {
                    Debug.LogWarning($"[CanvasScalerFixer] {prefab.name} 未找到Canvas组件");
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                    return false;
                }
                
                // 如果没有Canvas Scaler，添加一个
                if (canvasScaler == null)
                {
                    canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();
                    Debug.Log($"[CanvasScalerFixer] {prefab.name} 添加Canvas Scaler组件");
                }
                
                // 应用配置
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(
                    config.ReferenceResolutionWidth,
                    config.ReferenceResolutionHeight
                );
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = config.MatchWidthOrHeight;
                canvasScaler.referencePixelsPerUnit = 100f;
                
                // 保存修改
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                
                Debug.Log($"[CanvasScalerFixer] {prefab.name} Canvas Scaler配置已修复: {config.ReferenceResolutionWidth}x{config.ReferenceResolutionHeight}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CanvasScalerFixer] 修复失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 批量修复多个预制体的Canvas Scaler配置
        /// </summary>
        public static int BatchFixCanvasScaler(GameObject[] prefabs, bool showProgress = true)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return 0;
            }
            
            int successCount = 0;
            
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (showProgress)
                {
                    EditorUtility.DisplayProgressBar(
                        "修复Canvas Scaler",
                        $"正在处理: {prefabs[i].name} ({i + 1}/{prefabs.Length})",
                        (float)(i + 1) / prefabs.Length
                    );
                }
                
                if (FixCanvasScaler(prefabs[i]))
                {
                    successCount++;
                }
            }
            
            if (showProgress)
            {
                EditorUtility.ClearProgressBar();
            }
            
            return successCount;
        }
        
        /// <summary>
        /// 获取Canvas Scaler当前配置的描述信息
        /// </summary>
        public static string GetCanvasScalerInfo(GameObject prefab)
        {
            if (prefab == null)
            {
                return "Prefab为空";
            }
            
            var canvasScaler = prefab.GetComponentInChildren<CanvasScaler>();
            if (canvasScaler == null)
            {
                return "未找到Canvas Scaler组件";
            }
            
            return $"模式: {canvasScaler.uiScaleMode}\n" +
                   $"参考分辨率: {canvasScaler.referenceResolution.x}x{canvasScaler.referenceResolution.y}\n" +
                   $"屏幕匹配: {canvasScaler.matchWidthOrHeight}";
        }
    }
}
#endif

