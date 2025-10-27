#if UNITY_EDITOR
using System.Text;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI代码生成模板
    /// 负责生成Logic文件的代码
    /// </summary>
    public static class UICodeTemplate
    {
        /// <summary>
        /// 生成Logic文件代码（仅首次生成）
        /// </summary>
        public static string GenerateLogicCode(
            string uiName,
            string namespaceName)
        {
            var sb = new StringBuilder();
            
            // 文件头
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Framework.Core;");
            sb.AppendLine();
            
            // 命名空间
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {uiName} UI逻辑");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public class {uiName} : UIBehaviour");
            sb.AppendLine("    {");
            
            // 生命周期
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
            
            // 类结束
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
    }
}
#endif
