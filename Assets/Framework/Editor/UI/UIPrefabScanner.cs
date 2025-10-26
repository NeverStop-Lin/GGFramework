#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI Prefab扫描器
    /// 负责扫描Prefab，识别标记的组件
    /// </summary>
    public class UIPrefabScanner
    {
        /// <summary>
        /// 扫描结果
        /// </summary>
        public class ScanResult
        {
            /// <summary>
            /// 扫描到的组件列表
            /// </summary>
            public List<UIComponentInfo> Components = new List<UIComponentInfo>();
            
            /// <summary>
            /// 错误列表
            /// </summary>
            public List<string> Errors = new List<string>();
            
            /// <summary>
            /// 警告列表
            /// </summary>
            public List<string> Warnings = new List<string>();
            
            /// <summary>
            /// 是否有错误
            /// </summary>
            public bool HasErrors => Errors.Count > 0;
            
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool IsSuccess => !HasErrors;
        }
        
        // 标记前缀
        private const string MARKER_PREFIX = "@";
        
        // 支持的组件类型映射（直接使用typeof，不需要Type.GetType解析）
        private static readonly Dictionary<string, Type> ComponentTypeMap = new Dictionary<string, Type>
        {
            { "Button", typeof(Button) },
            { "Text", typeof(Text) },
            { "TextTMP", typeof(TextMeshProUGUI) },
            { "Image", typeof(Image) },
            { "Input", typeof(InputField) },
            { "Toggle", typeof(Toggle) },
            { "Slider", typeof(Slider) },
            { "Transform", typeof(Transform) },
            { "GameObject", typeof(GameObject) }
        };
        
        /// <summary>
        /// 扫描Prefab
        /// </summary>
        public static ScanResult ScanPrefab(GameObject prefab)
        {
            var result = new ScanResult();
            
            if (prefab == null)
            {
                result.Errors.Add("Prefab为空");
                return result;
            }
            
            Debug.Log($"[UIPrefabScanner] 开始扫描Prefab: {prefab.name}");
            Debug.Log($"[UIPrefabScanner] Prefab类型: {prefab.GetType()}");
            Debug.Log($"[UIPrefabScanner] 是否是Prefab资产: {UnityEditor.PrefabUtility.IsPartOfPrefabAsset(prefab)}");
            
            // 递归扫描所有子节点
            ScanTransform(prefab.transform, "", result);
            
            Debug.Log($"[UIPrefabScanner] 扫描完成，找到 {result.Components.Count} 个组件，{result.Errors.Count} 个错误");
            
            return result;
        }
        
        /// <summary>
        /// 递归扫描Transform
        /// </summary>
        private static void ScanTransform(Transform trans, string parentPath, ScanResult result)
        {
            var nodeName = trans.name;
            
            // 计算当前节点的完整路径
            var currentPath = string.IsNullOrEmpty(parentPath) ? nodeName : $"{parentPath}/{nodeName}";
            
            // 检查是否是标记节点
            if (nodeName.StartsWith(MARKER_PREFIX))
            {
                ProcessMarkedNode(trans, currentPath, result);
            }
            
            // 递归扫描子节点
            for (int i = 0; i < trans.childCount; i++)
            {
                ScanTransform(trans.GetChild(i), currentPath, result);
            }
        }
        
        /// <summary>
        /// 处理标记节点
        /// </summary>
        private static void ProcessMarkedNode(Transform trans, string path, ScanResult result)
        {
            var nodeName = trans.name;
            
            Debug.Log($"[UIPrefabScanner] 处理标记节点: {nodeName} at {path}");
            
            // 解析标记：@ComponentType_ComponentName
            // 例如：@Button_Start -> ComponentType=Button, ComponentName=Start
            var match = Regex.Match(nodeName, @"^@(\w+)_(.+)$");
            
            if (!match.Success)
            {
                Debug.LogError($"[UIPrefabScanner] 标记格式错误: {nodeName}");
                result.Errors.Add($"标记格式错误: {nodeName} at {path}\n应该是 @ComponentType_ComponentName 格式，例如：@Button_Start");
                return;
            }
            
            var componentType = match.Groups[1].Value;
            var componentName = match.Groups[2].Value;
            
            Debug.Log($"[UIPrefabScanner] 解析结果 - 类型: {componentType}, 名称: {componentName}");
            
            // 检查是否是支持的组件类型（直接从Map获取Type对象）
            if (!ComponentTypeMap.TryGetValue(componentType, out var type))
            {
                Debug.LogError($"[UIPrefabScanner] 不支持的组件类型: {componentType}");
                result.Errors.Add($"不支持的组件类型: {componentType} in {nodeName} at {path}\n支持的类型: {string.Join(", ", ComponentTypeMap.Keys)}");
                return;
            }
            
            Debug.Log($"[UIPrefabScanner] 从Map获取的类型: {type.FullName}");
            
            // 验证组件是否存在
            Component component = null;
            
            Debug.Log($"[UIPrefabScanner] 开始验证组件...");
            
            if (componentType == "GameObject")
            {
                // GameObject不需要验证组件
                Debug.Log($"[UIPrefabScanner] GameObject类型，无需验证组件");
            }
            else if (componentType == "Transform")
            {
                component = trans;
                Debug.Log($"[UIPrefabScanner] Transform类型，直接使用节点的Transform");
            }
            else
            {
                Debug.Log($"[UIPrefabScanner] 尝试从节点获取组件: {type.Name}");
                component = trans.GetComponent(type);
                Debug.Log($"[UIPrefabScanner] GetComponent({type.Name})结果: {(component != null ? "✅找到" : "❌null")}");
                
                // 如果找不到，输出节点上所有组件（用于调试）
                if (component == null)
                {
                    var allComponents = trans.GetComponents<Component>();
                    Debug.LogWarning($"[UIPrefabScanner] ⚠️ 节点 {nodeName} 上实际存在的所有组件({allComponents.Length}个):");
                    foreach (var c in allComponents)
                    {
                        Debug.LogWarning($"  - {c.GetType().FullName}");
                    }
                }
                else
                {
                    Debug.Log($"[UIPrefabScanner] ✅ 成功找到组件: {component.GetType().Name}");
                }
            }
            
            // 检查组件是否存在
            if (componentType != "GameObject" && componentType != "Transform" && component == null)
            {
                Debug.LogError($"[UIPrefabScanner] 组件验证失败: {componentType} in {nodeName}");
                result.Errors.Add($"找不到组件: {componentType} in {nodeName} at {path}\n节点存在但没有 {componentType} 组件");
                return;
            }
            
            Debug.Log($"[UIPrefabScanner] 组件验证成功: {componentType}");
            
            // 生成字段名：_startButton
            var fieldName = GenerateFieldName(componentName, componentType);
            
            // 创建组件信息
            var info = new UIComponentInfo
            {
                ComponentName = componentName,
                ComponentTypeName = componentType,
                FullTypeName = type.FullName,
                Path = path,
                OriginalName = nodeName,
                FieldName = fieldName,
                IsButton = componentType == "Button",
                EventHandlerName = GenerateEventHandlerName(componentName)
            };
            
            Debug.Log($"[UIPrefabScanner] ✅ 组件信息创建成功: {info}");
            
            result.Components.Add(info);
        }
        
        /// <summary>
        /// 生成字段名称
        /// </summary>
        private static string GenerateFieldName(string componentName, string componentType)
        {
            // 转换为驼峰命名：Close_Panel -> ClosePanel -> closePanel
            var camelName = ToCamelCase(componentName);
            
            // 添加类型后缀
            string suffix = "";
            switch (componentType)
            {
                case "Button":
                    suffix = "Button";
                    break;
                case "Text":
                case "TextTMP":
                    suffix = "Text";
                    break;
                case "Image":
                    suffix = "Image";
                    break;
                case "Input":
                    suffix = "Input";
                    break;
                case "Toggle":
                    suffix = "Toggle";
                    break;
                case "Slider":
                    suffix = "Slider";
                    break;
                case "Transform":
                    suffix = "Transform";
                    break;
                case "GameObject":
                    suffix = "GameObject";
                    break;
            }
            
            return $"_{camelName}{suffix}";
        }
        
        /// <summary>
        /// 生成事件处理方法名
        /// </summary>
        private static string GenerateEventHandlerName(string componentName)
        {
            // Close_Panel -> ClosePanelClick -> OnClosePanelClick
            var pascalName = ToPascalCase(componentName);
            return $"On{pascalName}Click";
        }
        
        /// <summary>
        /// 转换为驼峰命名（首字母小写）
        /// Close_Panel -> closePanel
        /// </summary>
        private static string ToCamelCase(string name)
        {
            var pascal = ToPascalCase(name);
            if (string.IsNullOrEmpty(pascal)) return "";
            return char.ToLower(pascal[0]) + pascal.Substring(1);
        }
        
        /// <summary>
        /// 转换为帕斯卡命名（首字母大写）
        /// close_panel -> ClosePanel
        /// </summary>
        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            
            // 按下划线分割
            var parts = name.Split('_');
            var result = "";
            
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                
                // 首字母大写
                result += char.ToUpper(part[0]) + part.Substring(1).ToLower();
            }
            
            return result;
        }
    }
}
#endif
