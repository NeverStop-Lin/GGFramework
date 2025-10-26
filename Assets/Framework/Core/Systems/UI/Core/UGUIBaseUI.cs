using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.Scripts;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// 基于UGUI的BaseUI实现
    /// 提供UI对象的基础管理功能
    /// </summary>
    public abstract class UGUIBaseUI : BaseUI
    {
        #region 字段
        
        /// <summary>
        /// UI游戏对象
        /// </summary>
        protected GameObject UIObject;
        
        /// <summary>
        /// UI的Canvas组件
        /// </summary>
        protected Canvas Canvas;
        
        /// <summary>
        /// UI的RectTransform组件
        /// </summary>
        protected RectTransform RectTransform;
        
        /// <summary>
        /// UI配置
        /// </summary>
        private UIConfig _config;
        
        #endregion
        
        #region 配置
        
        /// <summary>
        /// 创建UI配置（子类重写以提供自定义配置）
        /// </summary>
        /// <returns>UI配置</returns>
        protected virtual UIConfig CreateUIConfig()
        {
            return new UIConfig
            {
                UIType = UIType.Main,
                AlignType = UIAlignType.Center,
                CacheStrategy = UICacheStrategy.AlwaysCache
            };
        }
        
        /// <summary>
        /// 获取最终配置（合并代码配置和Manifest配置）
        /// </summary>
        private UIConfig GetFinalConfig()
        {
            // 代码配置
            var codeConfig = CreateUIConfig();
            
            // 从UIManifest获取运行时配置
            var manifestConfig = UIManifestManager.GetConfig(GetType());
            
            // 融合（运行时配置优先）
            return UIConfigMerger.Merge(codeConfig, manifestConfig);
        }
        
        #endregion
        
        #region 生命周期
        
        /// <summary>
        /// 创建UI时调用
        /// </summary>
        protected override async void OnCreate(params object[] args)
        {
            try
            {
                // 获取配置
                _config = GetFinalConfig();
                
                // 自动加载资源
                if (!string.IsNullOrEmpty(_config.ResourcePath))
                {
                    await LoadUIResourceAsync();
                }
                else
                {
                    // 兼容旧代码：手动创建
                    CreateUIObject();
                }
                
                // 如果UIObject创建成功，初始化组件
                if (UIObject != null)
                {
                    InitializeComponents();
                }
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UGUI] UI创建失败: {GetType().Name}, {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        /// <summary>
        /// 异步加载UI资源
        /// </summary>
        private async Task LoadUIResourceAsync()
        {
            try
            {
                // 使用框架的资源系统加载
                var prefab = await GridFramework.Resource.LoadAsync<GameObject>(_config.ResourcePath);
                
                if (prefab == null)
                {
                    throw new Exception($"无法加载UI资源: {_config.ResourcePath}");
                }
                
                // 实例化到UIRoot下
                var parent = UIRootManager.GetOrCreateUIRoot();
                UIObject = UnityEngine.Object.Instantiate(prefab, parent);
                UIObject.name = GetType().Name; // 设置名称为UI类名
                
                FrameworkLogger.Info($"[UGUI] UI资源加载成功: {GetType().Name} <- {_config.ResourcePath}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UGUI] UI资源加载失败: {GetType().Name}, 路径: {_config.ResourcePath}, 错误: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 获取Canvas组件
            Canvas = UIObject.GetComponent<Canvas>();
            if (Canvas == null)
            {
                Canvas = UIObject.GetComponentInChildren<Canvas>();
            }
            
            // 获取RectTransform
            RectTransform = UIObject.GetComponent<RectTransform>();
            
            // 绑定组件（由自动生成的代码重写）
            BindComponents();
            
            // 根据配置初始化Attachments
            InitializeAttachmentsByConfig();
        }
        
        /// <summary>
        /// 根据配置初始化Attachments
        /// </summary>
        private void InitializeAttachmentsByConfig()
        {
            if (_config == null) return;
            
            var attachments = new List<UIAttachment>();
            
            // 根据配置添加Attachment
            if (_config.UseMask)
            {
                // TODO: 添加MaskUIAttachment
                // attachments.Add(new MaskUIAttachment(_config.MaskColor));
            }
            
            if (_config.UseAnimation && _config.AnimationType != UIAnimationType.None && _config.AnimationType != UIAnimationType.Custom)
            {
                // TODO: 根据AnimationType创建对应的动画Attachment
                // attachments.Add(CreateAnimationAttachment(_config.AnimationType, _config.AnimationDuration));
            }
            
            // 如果有自定义Attachment，让子类添加
            OnAttachmentInitialize(attachments);
        }
        
        /// <summary>
        /// 手动创建UI对象（兼容模式，子类可重写）
        /// </summary>
        protected virtual void CreateUIObject()
        {
            // 默认不执行任何操作
            // 子类可以重写此方法来手动创建UI
        }
        
        /// <summary>
        /// 绑定组件（自动生成的代码会重写此方法）
        /// </summary>
        protected virtual void BindComponents()
        {
            // 默认不执行任何操作
            // 自动生成的Binding代码会重写此方法
        }
        
        /// <summary>
        /// 显示UI时调用
        /// </summary>
        protected override void OnShow(params object[] args)
        {
            if (UIObject != null)
            {
                UIObject.SetActive(true);
            }
            
            // 注册事件
            RegisterEvents();
        }
        
        /// <summary>
        /// 隐藏UI时调用
        /// </summary>
        protected override void OnHide(params object[] args)
        {
            // 注销事件
            UnregisterEvents();
            
            if (UIObject != null)
            {
                UIObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 销毁UI时调用
        /// </summary>
        protected override void OnDestroy(params object[] args)
        {
            if (UIObject != null)
            {
                UnityEngine.Object.Destroy(UIObject);
                UIObject = null;
                Canvas = null;
                RectTransform = null;
            }
        }
        
        #endregion
        
        #region 层级管理
        
        /// <summary>
        /// 获取UI的层级索引（基于Canvas的sortingOrder）
        /// </summary>
        public override int GetIndex()
        {
            return Canvas != null ? Canvas.sortingOrder : 0;
        }
        
        /// <summary>
        /// 设置UI的层级索引（通过修改Canvas的sortingOrder）
        /// </summary>
        public override void SetIndex(int i)
        {
            if (Canvas != null)
            {
                Canvas.sortingOrder = i;
            }
        }
        
        #endregion
        
        #region 事件管理
        
        /// <summary>
        /// 注册事件（自动生成的代码会重写此方法）
        /// </summary>
        protected virtual void RegisterEvents()
        {
            // 默认不执行任何操作
            // 自动生成的Binding代码会重写此方法
        }
        
        /// <summary>
        /// 注销事件（自动生成的代码会重写此方法）
        /// </summary>
        protected virtual void UnregisterEvents()
        {
            // 默认不执行任何操作
            // 自动生成的Binding代码会重写此方法
        }
        
        #endregion
        
        #region 组件查找
        
        /// <summary>
        /// 查找组件（记录完整路径，精确查找）
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="path">相对于UIObject的路径</param>
        /// <returns>找到的组件</returns>
        /// <exception cref="Exception">找不到节点或组件时抛出异常</exception>
        protected T FindComponent<T>(string path) where T : Component
        {
            if (UIObject == null)
            {
                throw new Exception($"[UGUI] UIObject为空，无法查找组件: {path} in {GetType().Name}");
            }
            
            // 查找节点
            var trans = UIObject.transform.Find(path);
            if (trans == null)
            {
                throw new Exception($"[UGUI] 找不到节点: {path} in {GetType().Name}");
            }
            
            // 获取组件
            var component = trans.GetComponent<T>();
            if (component == null)
            {
                throw new Exception($"[UGUI] 找不到组件: {typeof(T).Name} at {path} in {GetType().Name}");
            }
            
            return component;
        }
        
        /// <summary>
        /// 尝试查找组件（不抛异常，找不到返回null）
        /// </summary>
        protected T TryFindComponent<T>(string path) where T : Component
        {
            if (UIObject == null)
            {
                return null;
            }
            
            var trans = UIObject.transform.Find(path);
            return trans?.GetComponent<T>();
        }
        
        #endregion
    }
}


