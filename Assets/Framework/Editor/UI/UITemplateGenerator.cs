#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI模板生成器
    /// 用于创建默认的UI模板预制体
    /// </summary>
    public static class UITemplateGenerator
    {
        private const string TEMPLATE_PATH = "Assets/Framework/Editor/UI/Template/DefaultUITemplate.prefab";
        
        /// <summary>
        /// 生成默认UI模板（内部使用，自动静默生成）
        /// </summary>
        public static void GenerateDefaultTemplate()
        {
            // 创建Canvas GameObject
            var canvasGo = new GameObject("UITemplate");
            
            // 添加Canvas组件
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // 添加CanvasScaler组件
            var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1280, 720);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            // 添加GraphicRaycaster组件
            canvasGo.AddComponent<GraphicRaycaster>();
            
            // 确保目录存在
            var directory = System.IO.Path.GetDirectoryName(TEMPLATE_PATH);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // 保存为Prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGo, TEMPLATE_PATH);
            
            // 删除场景中的临时对象
            Object.DestroyImmediate(canvasGo);
            
            // 刷新资源数据库
            AssetDatabase.Refresh();
            
            if (prefab != null)
            {
                Debug.Log($"[UITemplateGenerator] 默认UI模板已自动创建: {TEMPLATE_PATH}");
            }
            else
            {
                Debug.LogError("[UITemplateGenerator] 创建默认UI模板失败");
            }
        }
        
        /// <summary>
        /// 检查模板是否存在
        /// </summary>
        public static bool TemplateExists()
        {
            return System.IO.File.Exists(TEMPLATE_PATH);
        }
        
        /// <summary>
        /// 获取模板路径
        /// </summary>
        public static string GetTemplatePath()
        {
            return TEMPLATE_PATH;
        }
        
        /// <summary>
        /// 确保模板存在（不存在则自动创建）
        /// </summary>
        public static void EnsureTemplateExists()
        {
            if (!TemplateExists())
            {
                Debug.Log("[UITemplateGenerator] 模板不存在，正在自动创建...");
                GenerateDefaultTemplate();
            }
        }
    }
}
#endif

