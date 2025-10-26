using UnityEngine;
using UnityEditor;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI Prefab迁移工具
    /// 自动为Prefab添加UI组件，支持MonoBehaviour架构
    /// </summary>
    public class UIPrefabMigrationTool : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _log = "";
        
        [MenuItem("Tools/Framework/UI Prefab Migration Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<UIPrefabMigrationTool>("UI Prefab Migration");
            window.minSize = new Vector2(400, 300);
        }
        
        void OnGUI()
        {
            GUILayout.Label("UI Prefab迁移工具", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "此工具会自动为UI Prefab添加对应的UI组件（MonoBehaviour）\n" +
                "迁移前请确保代码已编译成功！",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // 单个迁移按钮
            if (GUILayout.Button("迁移 MainMenuUI.prefab", GUILayout.Height(30)))
            {
                MigratePrefab(
                    "Assets/Game/Resources/UI/MainMenuUI.prefab",
                    "Game.UI.MainMenuUI"
                );
            }
            
            if (GUILayout.Button("迁移 GameUI.prefab", GUILayout.Height(30)))
            {
                MigratePrefab(
                    "Assets/Game/Resources/UI/GameUI.prefab",
                    "Game.UI.GameUI"
                );
            }
            
            GUILayout.Space(10);
            
            // 批量迁移按钮
            if (GUILayout.Button("一键迁移所有UI Prefab", GUILayout.Height(40)))
            {
                _log = "";
                AddLog("开始批量迁移...\n");
                
                MigratePrefab("Assets/Game/Resources/UI/MainMenuUI.prefab", "Game.UI.MainMenuUI");
                MigratePrefab("Assets/Game/Resources/UI/GameUI.prefab", "Game.UI.GameUI");
                
                AddLog("\n✅ 批量迁移完成！");
                AddLog("请在Unity中运行游戏测试功能。");
            }
            
            GUILayout.Space(10);
            
            // 日志区域
            GUILayout.Label("迁移日志:", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
            EditorGUILayout.TextArea(_log, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("清空日志"))
            {
                _log = "";
            }
        }
        
        private void MigratePrefab(string prefabPath, string uiTypeFullName)
        {
            try
            {
                AddLog($"\n[开始] 迁移 {prefabPath}");
                
                // 加载Prefab
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    AddLog($"❌ 错误: 无法加载Prefab: {prefabPath}");
                    return;
                }
                
                // 获取UI类型
                var uiType = System.Type.GetType(uiTypeFullName);
                if (uiType == null)
                {
                    AddLog($"❌ 错误: 找不到UI类型: {uiTypeFullName}");
                    AddLog($"   提示: 请确保代码已编译成功！");
                    return;
                }
                
                // 检查是否已经有UI组件
                var existingComponent = prefab.GetComponent(uiType);
                if (existingComponent != null)
                {
                    AddLog($"⚠️ 警告: Prefab已有UI组件，跳过");
                    return;
                }
                
                // 添加UI组件
                var uiComponent = prefab.AddComponent(uiType);
                if (uiComponent == null)
                {
                    AddLog($"❌ 错误: 无法添加UI组件");
                    return;
                }
                AddLog($"  ✓ 添加UI组件: {uiType.Name}");
                
                // 确保有Canvas
                var canvas = prefab.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = prefab.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    AddLog($"  ✓ 添加Canvas组件");
                }
                else
                {
                    AddLog($"  ✓ Canvas已存在");
                }
                
                // 确保有GraphicRaycaster
                var raycaster = prefab.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster == null)
                {
                    prefab.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    AddLog($"  ✓ 添加GraphicRaycaster组件");
                }
                else
                {
                    AddLog($"  ✓ GraphicRaycaster已存在");
                }
                
                // 保存Prefab
                PrefabUtility.SavePrefabAsset(prefab);
                AddLog($"  ✓ 保存Prefab");
                
                AddLog($"✅ 成功: {System.IO.Path.GetFileName(prefabPath)} 迁移完成");
            }
            catch (System.Exception ex)
            {
                AddLog($"❌ 异常: {ex.Message}");
                Debug.LogError($"[UIPrefabMigrationTool] 迁移失败: {prefabPath}\n{ex}");
            }
        }
        
        private void AddLog(string message)
        {
            _log += message + "\n";
            Repaint();
        }
    }
}

