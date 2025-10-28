using System;
using Framework.Scripts;
using UnityEngine;
using Zenject;

namespace Framework.Core
{
    /// <summary>
    /// UI工厂
    /// 负责创建MonoBehaviour UI实例
    /// </summary>
    public class UIFactory : IFactory<Type, IBaseUI>
    {
        private readonly DiContainer _container;
        private readonly IResource _resource;
        
        public UIFactory(DiContainer container, IResource resource)
        {
            _container = container;
            _resource = resource;
        }
        
        public IBaseUI Create(Type uiType)
        {
            // 获取UI配置
            var config = UIProjectConfigManager.GetUIInstanceConfig(uiType);
            
            // 严格模式：必须配置UI
            if (config == null)
            {
                throw new InvalidOperationException(
                    $"[UIFactory] UI 未配置: {uiType.Name}\n" +
                    $"请在 UIProjectConfig 中添加该 UI 的配置\n" +
                    $"路径: Assets/Resources/Framework/Configs/UIProjectConfig.asset");
            }
            
            // 严格模式：必须配置Prefab路径
            if (string.IsNullOrEmpty(config.ResourcePath))
            {
                throw new InvalidOperationException(
                    $"[UIFactory] UI 缺少 Prefab 路径: {uiType.Name}\n" +
                    $"请在 UIProjectConfig 中配置 ResourcePath 字段\n" +
                    $"例如: UI/MainMenuUI");
            }
            
            // 从Prefab创建UI（唯一方式）
            return CreateFromPrefab(uiType, config.ResourcePath);
        }
        
        /// <summary>
        /// 从Prefab创建UI
        /// </summary>
        private IBaseUI CreateFromPrefab(Type uiType, string resourcePath)
        {
            try
            {
                // 使用框架的资源系统加载Prefab
                var prefab = _resource.Load<GameObject>(resourcePath);
                if (prefab == null)
                {
                    throw new Exception($"无法加载UI Prefab: {resourcePath}");
                }
                
                // 获取UIRoot
                var uiRoot = UIRootManager.GetOrCreateUIRoot();
                
                // 实例化Prefab（Zenject自动注入）
                var go = _container.InstantiatePrefab(prefab, uiRoot);
                go.name = uiType.Name; // 设置名称为UI类名
                
                // 确保 Canvas 启用 overrideSorting，让 CanvasScaler 等组件正常工作
                var canvas = go.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.overrideSorting = true;
                }
                
                // 获取UI组件
                var ui = go.GetComponent(uiType) as IBaseUI;
                if (ui == null)
                {
                    // Prefab上没有UI组件是严重的配置错误，应该抛出异常
                    // 而不是静默地尝试修复，这会掩盖实际问题
                    UnityEngine.Object.Destroy(go);
                    throw new Exception($"Prefab上缺少UI组件 {uiType.Name}，请在Prefab上添加该脚本组件");
                }
                
                FrameworkLogger.Info($"[UIFactory] 从Prefab创建UI成功: {uiType.Name} <- {resourcePath}");
                
                return ui;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

