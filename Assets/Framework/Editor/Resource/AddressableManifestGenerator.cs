#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Framework.Core.Resource;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.Resource
{
    /// <summary>
    /// Addressable 资源清单生成器
    /// 扫描项目中所有 Addressable 资源并生成清单文件
    /// </summary>
    public class AddressableManifestGenerator : EditorWindow
    {
        private const string MANIFEST_PATH = "Assets/Generations/Resources/AddressableResourceManifest.asset";
        private const string MANIFEST_RESOURCES_PATH = "AddressableResourceManifest";

        [MenuItem("Framework/资源管理/生成 Addressable 清单")]
        static void ShowWindow()
        {
            var window = GetWindow<AddressableManifestGenerator>("Addressable 清单生成器");
            window.minSize = new Vector2(500, 400);
        }

        private Vector2 _scrollPosition;
        private string _lastGeneratedTime = "未生成";
        private int _lastResourceCount = 0;

        void OnGUI()
        {
            GUILayout.Label("Addressable 资源清单生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "此工具会扫描项目中所有 Addressable 资源，生成资源清单文件。\n" +
                "清单文件将被放置在 Framework/Resources/ 下，供运行时快速查询。",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // 显示清单信息
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("清单信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("清单路径:", MANIFEST_PATH);
            EditorGUILayout.LabelField("最后生成时间:", _lastGeneratedTime);
            EditorGUILayout.LabelField("资源数量:", _lastResourceCount.ToString());
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 生成按钮
            if (GUILayout.Button("生成资源清单", GUILayout.Height(40)))
            {
                GenerateManifest();
            }

            EditorGUILayout.Space();

            // 查看按钮
            if (GUILayout.Button("查看清单文件", GUILayout.Height(30)))
            {
                ViewManifest();
            }

            if (GUILayout.Button("清空清单", GUILayout.Height(30)))
            {
                ClearManifest();
            }
        }

        /// <summary>
        /// 生成清单
        /// </summary>
        void GenerateManifest()
        {
            try
            {
                // 检查 Addressables 是否安装
                var addressablesSettingsType = Type.GetType(
                    "UnityEditor.AddressableAssets.Settings.AddressableAssetSettings, Unity.Addressables.Editor"
                );

                if (addressablesSettingsType == null)
                {
                    EditorUtility.DisplayDialog(
                        "Addressables 未安装",
                        "检测到 Addressables 未安装。\n\n" +
                        "请通过 Package Manager 安装 Addressables 包后再使用此功能。",
                        "确定"
                    );
                    return;
                }

                // 获取 Addressables Settings
                var getSettingsMethod = addressablesSettingsType.GetMethod(
                    "get_DefaultObject",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );

                var settings = getSettingsMethod?.Invoke(null, null);

                if (settings == null)
                {
                    EditorUtility.DisplayDialog(
                        "Addressables 未初始化",
                        "Addressables 系统未初始化。\n\n" +
                        "请先打开 Addressables Groups 窗口:\n" +
                        "Window → Asset Management → Addressables → Groups",
                        "确定"
                    );
                    return;
                }

                // 创建或加载清单
                var manifest = AssetDatabase.LoadAssetAtPath<AddressableResourceManifest>(MANIFEST_PATH);
                if (manifest == null)
                {
                    // 确保目录存在
                    string directory = Path.GetDirectoryName(MANIFEST_PATH);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    manifest = CreateInstance<AddressableResourceManifest>();
                    AssetDatabase.CreateAsset(manifest, MANIFEST_PATH);
                }

                manifest.Clear();

                // 获取所有 Addressable Groups
                var groupsProperty = addressablesSettingsType.GetProperty("groups");
                var groups = groupsProperty.GetValue(settings) as System.Collections.IList;

                int totalResources = 0;

                // 遍历所有分组
                foreach (var group in groups)
                {
                    if (group == null)
                        continue;

                    var groupType = group.GetType();
                    var groupName = groupType.GetProperty("Name")?.GetValue(group) as string;

                    // 获取分组中的条目
                    var entriesProperty = groupType.GetProperty("entries");
                    var entries = entriesProperty?.GetValue(group) as System.Collections.IList;

                    if (entries == null)
                        continue;

                    // 遍历条目
                    foreach (var entry in entries)
                    {
                        if (entry == null)
                            continue;

                        var entryType = entry.GetType();
                        var address = entryType.GetProperty("address")?.GetValue(entry) as string;
                        var guid = entryType.GetProperty("guid")?.GetValue(entry) as string;

                        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(guid))
                            continue;

                        // 获取物理路径
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

                        // 添加到清单
                        manifest.AddResource(
                            address,
                            assetType?.Name ?? "Unknown",
                            guid,
                            assetPath,
                            groupName ?? "Default"
                        );

                        totalResources++;
                    }
                }

                // 设置生成时间
                manifest.SetGeneratedTime();

                // 保存清单
                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                _lastGeneratedTime = manifest.generatedTime;
                _lastResourceCount = totalResources;

                Debug.Log($"[AddressableManifestGenerator] 清单生成完成！");
                Debug.Log($"  - 清单路径: {MANIFEST_PATH}");
                Debug.Log($"  - 资源数量: {totalResources}");
                Debug.Log($"  - 生成时间: {manifest.generatedTime}");

                EditorUtility.DisplayDialog(
                    "清单生成成功",
                    $"成功生成 Addressable 资源清单！\n\n" +
                    $"资源数量: {totalResources}\n" +
                    $"清单路径: {MANIFEST_PATH}\n\n" +
                    $"清单已放置在 Resources 目录下，运行时将自动加载。",
                    "确定"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableManifestGenerator] 生成清单失败: {e}");
                EditorUtility.DisplayDialog("生成失败", $"清单生成失败:\n{e.Message}", "确定");
            }
        }

        /// <summary>
        /// 查看清单
        /// </summary>
        void ViewManifest()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<AddressableResourceManifest>(MANIFEST_PATH);
            if (manifest != null)
            {
                Selection.activeObject = manifest;
                EditorGUIUtility.PingObject(manifest);

                _lastGeneratedTime = manifest.generatedTime;
                _lastResourceCount = manifest.totalCount;
            }
            else
            {
                EditorUtility.DisplayDialog("清单不存在", "请先生成清单", "确定");
            }
        }

        /// <summary>
        /// 清空清单
        /// </summary>
        void ClearManifest()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<AddressableResourceManifest>(MANIFEST_PATH);
            if (manifest != null)
            {
                if (EditorUtility.DisplayDialog("确认清空", "确定要清空清单吗？", "确定", "取消"))
                {
                    manifest.Clear();
                    EditorUtility.SetDirty(manifest);
                    AssetDatabase.SaveAssets();

                    _lastGeneratedTime = "已清空";
                    _lastResourceCount = 0;

                    Debug.Log("[AddressableManifestGenerator] 清单已清空");
                }
            }
        }

        void OnEnable()
        {
            // 加载当前清单信息
            var manifest = AssetDatabase.LoadAssetAtPath<AddressableResourceManifest>(MANIFEST_PATH);
            if (manifest != null)
            {
                _lastGeneratedTime = manifest.generatedTime;
                _lastResourceCount = manifest.totalCount;
            }
        }
    }

    /// <summary>
    /// 自动生成清单（构建时触发）
    /// </summary>
    public class AddressableManifestBuildProcessor
    {
        [MenuItem("Framework/资源管理/自动生成清单（构建时）")]
        static void AutoGenerateOnBuild()
        {
            Debug.Log("[AddressableManifestBuildProcessor] 开始自动生成清单...");
            
            var generator = EditorWindow.GetWindow<AddressableManifestGenerator>(false, "Addressable 清单生成器", false);
            generator.Close();
            
            // 直接调用生成逻辑
            GenerateManifestStatic();
        }

        static void GenerateManifestStatic()
        {
            try
            {
                var addressablesSettingsType = Type.GetType(
                    "UnityEditor.AddressableAssets.Settings.AddressableAssetSettings, Unity.Addressables.Editor"
                );

                if (addressablesSettingsType == null)
                {
                    Debug.LogWarning("[AddressableManifestBuildProcessor] Addressables 未安装，跳过清单生成");
                    return;
                }

                var getSettingsMethod = addressablesSettingsType.GetMethod(
                    "get_DefaultObject",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );

                var settings = getSettingsMethod?.Invoke(null, null);
                if (settings == null)
                {
                    Debug.LogWarning("[AddressableManifestBuildProcessor] Addressables 未初始化，跳过清单生成");
                    return;
                }

                var manifest = AssetDatabase.LoadAssetAtPath<AddressableResourceManifest>(
                    "Assets/Generations/Resources/AddressableResourceManifest.asset"
                );
                
                if (manifest == null)
                {
                    string directory = "Assets/Generations/Resources";
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    manifest = ScriptableObject.CreateInstance<AddressableResourceManifest>();
                    AssetDatabase.CreateAsset(manifest, "Assets/Generations/Resources/AddressableResourceManifest.asset");
                }

                manifest.Clear();

                var groupsProperty = addressablesSettingsType.GetProperty("groups");
                var groups = groupsProperty.GetValue(settings) as System.Collections.IList;

                int totalResources = 0;

                foreach (var group in groups)
                {
                    if (group == null) continue;

                    var groupType = group.GetType();
                    var groupName = groupType.GetProperty("Name")?.GetValue(group) as string;
                    var entriesProperty = groupType.GetProperty("entries");
                    var entries = entriesProperty?.GetValue(group) as System.Collections.IList;

                    if (entries == null) continue;

                    foreach (var entry in entries)
                    {
                        if (entry == null) continue;

                        var entryType = entry.GetType();
                        var address = entryType.GetProperty("address")?.GetValue(entry) as string;
                        var guid = entryType.GetProperty("guid")?.GetValue(entry) as string;

                        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(guid))
                            continue;

                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

                        manifest.AddResource(
                            address,
                            assetType?.Name ?? "Unknown",
                            guid,
                            assetPath,
                            groupName ?? "Default"
                        );

                        totalResources++;
                    }
                }

                manifest.SetGeneratedTime();
                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssets();

                Debug.Log($"[AddressableManifestBuildProcessor] 清单生成完成，共 {totalResources} 个资源");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableManifestBuildProcessor] 生成清单失败: {e}");
            }
        }
    }
}
#endif

