#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Framework.Core;
using System.Collections.Generic;

namespace Framework.Editor.UI
{
    /// <summary>
    /// 层级配置Tab
    /// 管理UI层级定义
    /// </summary>
    public class LayerConfigTab
    {
        private UIProjectConfig _config;
        private List<UILayerDefinition> _editingLayers = new List<UILayerDefinition>();
        
        private string _newLayerName = "";
        private int _newBaseSortingOrder = 0;
        private string _newDescription = "";
        
        public void OnEnable()
        {
            LoadConfig();
        }
        
        public void OnGUI()
        {
            EditorGUILayout.LabelField("UI层级配置", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (_config == null)
            {
                EditorGUILayout.HelpBox("未找到配置文件，请在设置Tab中创建或选择配置文件", MessageType.Warning);
                return;
            }
            
            DrawLayerList();
            EditorGUILayout.Space(10);
            DrawAddLayer();
        }
        
        public void OnDisable()
        {
        }
        
        private void LoadConfig()
        {
            // 获取配置（不需要每次都Reload，只在必要时重新加载）
            _config = UIProjectConfigManager.GetConfig();
            
            if (_config != null)
            {
                // 克隆层级列表用于编辑
                _editingLayers.Clear();
                foreach (var layer in _config.LayerDefinitions)
                {
                    _editingLayers.Add(layer.Clone());
                }
            }
        }
        
        private void DrawLayerList()
        {
            EditorGUILayout.LabelField("现有层级", EditorStyles.boldLabel);
            
            if (_editingLayers.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无层级定义，请添加", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical("box");
            
            // 表头
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("层级名称", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("基础SortingOrder", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("说明", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            // 按BaseSortingOrder排序显示
            _editingLayers.Sort((a, b) => a.BaseSortingOrder.CompareTo(b.BaseSortingOrder));
            
            for (int i = 0; i < _editingLayers.Count; i++)
            {
                var layer = _editingLayers[i];
                
                EditorGUILayout.BeginHorizontal("box");
                
                // 层级名称
                layer.LayerName = EditorGUILayout.TextField(layer.LayerName, GUILayout.Width(150));
                
                // 基础SortingOrder
                layer.BaseSortingOrder = EditorGUILayout.IntField(layer.BaseSortingOrder, GUILayout.Width(150));
                
                // 说明
                layer.Description = EditorGUILayout.TextField(layer.Description);
                
                // 删除按钮
                if (GUILayout.Button("删除", GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("确认删除", 
                        $"确定要删除层级 '{layer.LayerName}' 吗？", "删除", "取消"))
                    {
                        _editingLayers.RemoveAt(i);
                        SaveChanges();
                        break;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            
            // 保存按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("保存更改", GUILayout.Width(120), GUILayout.Height(30)))
            {
                SaveChanges();
            }
            
            if (GUILayout.Button("重新加载", GUILayout.Width(120), GUILayout.Height(30)))
            {
                LoadConfig();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawAddLayer()
        {
            EditorGUILayout.LabelField("添加新层级", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // 层级名称
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("层级名称:", GUILayout.Width(120));
            _newLayerName = EditorGUILayout.TextField(_newLayerName);
            EditorGUILayout.EndHorizontal();
            
            // 基础SortingOrder
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("基础SortingOrder:", GUILayout.Width(120));
            _newBaseSortingOrder = EditorGUILayout.IntField(_newBaseSortingOrder);
            EditorGUILayout.EndHorizontal();
            
            // 说明
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("说明:", GUILayout.Width(120));
            _newDescription = EditorGUILayout.TextField(_newDescription);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 添加按钮
            if (GUILayout.Button("添加层级", GUILayout.Height(30)))
            {
                AddLayer();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void AddLayer()
        {
            if (string.IsNullOrWhiteSpace(_newLayerName))
            {
                EditorUtility.DisplayDialog("错误", "层级名称不能为空", "确定");
                return;
            }
            
            // 检查是否重复
            if (_editingLayers.Exists(l => l.LayerName == _newLayerName))
            {
                EditorUtility.DisplayDialog("错误", $"层级名称 '{_newLayerName}' 已存在", "确定");
                return;
            }
            
            var newLayer = new UILayerDefinition
            {
                LayerName = _newLayerName,
                BaseSortingOrder = _newBaseSortingOrder,
                Description = _newDescription
            };
            
            _editingLayers.Add(newLayer);
            _config.AddOrUpdateLayer(newLayer);
            
            // 清空输入
            _newLayerName = "";
            _newBaseSortingOrder = 0;
            _newDescription = "";
            
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
        }
        
        private void SaveChanges()
        {
            if (_config == null)
            {
                EditorUtility.DisplayDialog("错误", "配置文件未加载", "确定");
                return;
            }
            
            // 清空现有层级
            _config.LayerDefinitions.Clear();
            
            // 添加编辑后的层级
            foreach (var layer in _editingLayers)
            {
                _config.AddOrUpdateLayer(layer);
            }
            
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("成功", "层级配置已保存", "确定");
        }
    }
}
#endif

