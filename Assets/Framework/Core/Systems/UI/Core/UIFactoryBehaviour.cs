using System;
using Framework.Scripts;
using UnityEngine;
using Zenject;

namespace Framework.Core
{
    /// <summary>
    /// MonoBehaviour版本的UI工厂
    /// 负责创建MonoBehaviour UI实例
    /// </summary>
    public class UIFactoryBehaviour : IFactory<Type, IBaseUI>
    {
        private readonly DiContainer _container;
        
        public UIFactoryBehaviour(DiContainer container)
        {
            _container = container;
        }
        
        public IBaseUI Create(Type uiType)
        {
            // 获取UI配置
            var config = UIManifestManager.GetConfig(uiType);
            
            // 方式1: 从Prefab创建（推荐）
            if (config != null && !string.IsNullOrEmpty(config.ResourcePath))
            {
                return CreateFromPrefab(uiType, config.ResourcePath);
            }
            
            // 方式2: 动态创建（用于没有Prefab的UI）
            return CreateDynamic(uiType);
        }
        
        /// <summary>
        /// 从Prefab创建UI
        /// </summary>
        private IBaseUI CreateFromPrefab(Type uiType, string resourcePath)
        {
            try
            {
                // 加载Prefab
                var prefab = Resources.Load<GameObject>(resourcePath);
                if (prefab == null)
                {
                    throw new Exception($"无法加载UI Prefab: {resourcePath}");
                }
                
                // 获取UIRoot
                var uiRoot = UIRootManager.GetOrCreateUIRoot();
                
                // 实例化Prefab（Zenject自动注入）
                var go = _container.InstantiatePrefab(prefab, uiRoot);
                go.name = uiType.Name; // 设置名称为UI类名
                
                // 获取UI组件
                var ui = go.GetComponent(uiType) as IBaseUI;
                if (ui == null)
                {
                    // Prefab上没有UI组件，动态添加
                    FrameworkLogger.Warn($"[UIFactory] Prefab上缺少UI组件，动态添加: {uiType.Name}");
                    ui = go.AddComponent(uiType) as IBaseUI;
                    
                    // 手动注入依赖
                    _container.Inject(ui);
                }
                
                FrameworkLogger.Info($"[UIFactory] 从Prefab创建UI成功: {uiType.Name} <- {resourcePath}");
                
                return ui;
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UIFactory] 从Prefab创建UI失败: {uiType.Name}, 路径: {resourcePath}, 错误: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 动态创建UI（没有Prefab时使用）
        /// </summary>
        private IBaseUI CreateDynamic(Type uiType)
        {
            try
            {
                // 获取UIRoot
                var uiRoot = UIRootManager.GetOrCreateUIRoot();
                
                // 创建新GameObject
                var go = new GameObject(uiType.Name);
                go.transform.SetParent(uiRoot, false);
                
                // 添加必要的组件
                var rectTransform = go.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
                
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                // 添加UI组件
                var ui = go.AddComponent(uiType) as IBaseUI;
                
                // Zenject注入
                _container.InjectGameObject(go);
                
                FrameworkLogger.Info($"[UIFactory] 动态创建UI成功: {uiType.Name}");
                
                return ui;
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UIFactory] 动态创建UI失败: {uiType.Name}, 错误: {ex.Message}");
                throw;
            }
        }
    }
}

