#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI模板生成器
    /// 用于创建默认的UI模板预制体和代码
    /// </summary>
    public static class UITemplateGenerator
    {
        private const string TEMPLATE_DIR = "Assets/Framework/UITemplates/DefaultUI";
        private const string TEMPLATE_PREFAB_PATH = "Assets/Framework/UITemplates/DefaultUI/DefaultUI.prefab";
        private const string TEMPLATE_CODE_PATH = "Assets/Framework/UITemplates/DefaultUI/DefaultUI.cs";
        
        /// <summary>
        /// 生成默认UI模板（内部使用，自动静默生成）
        /// </summary>
        public static void GenerateDefaultTemplate()
        {
            // 确保目录存在
            if (!System.IO.Directory.Exists(TEMPLATE_DIR))
            {
                System.IO.Directory.CreateDirectory(TEMPLATE_DIR);
            }
            
            // 创建Canvas GameObject
            var canvasGo = new GameObject("DefaultUI");
            
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
            
            // 生成模板代码
            var templateCode = GenerateTemplateCode();
            System.IO.File.WriteAllText(TEMPLATE_CODE_PATH, templateCode);
            
            // 刷新资源数据库，等待编译
            AssetDatabase.Refresh();
            
            // 保存为Prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGo, TEMPLATE_PREFAB_PATH);
            
            // 删除场景中的临时对象
            Object.DestroyImmediate(canvasGo);
            
            // 再次刷新资源数据库
            AssetDatabase.Refresh();
            
            if (prefab != null)
            {
                Debug.Log($"[UITemplateGenerator] 默认UI模板已自动创建: {TEMPLATE_PREFAB_PATH}");
            }
            else
            {
                Debug.LogError("[UITemplateGenerator] 创建默认UI模板失败");
            }
        }
        
        /// <summary>
        /// 生成模板代码
        /// </summary>
        private static string GenerateTemplateCode()
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Framework.Core;");
            sb.AppendLine();
            sb.AppendLine("namespace Framework.UI.Templates");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// DefaultUI 模板");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public class DefaultUI : UIBehaviour");
            sb.AppendLine("    {");
            sb.AppendLine("        protected override void OnCreate(params object[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        protected override void OnShow(params object[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        protected override void OnHide(params object[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private void Update()");
            sb.AppendLine("        {");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 检查模板是否存在
        /// </summary>
        public static bool TemplateExists()
        {
            return System.IO.File.Exists(TEMPLATE_PREFAB_PATH);
        }
        
        /// <summary>
        /// 获取模板路径
        /// </summary>
        public static string GetTemplatePath()
        {
            return TEMPLATE_PREFAB_PATH;
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

