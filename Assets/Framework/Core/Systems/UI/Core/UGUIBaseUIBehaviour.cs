using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.Scripts;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// 基于UGUI的MonoBehaviour UI实现
    /// 提供UI对象的基础管理功能
    /// </summary>
    public abstract class UGUIBaseUIBehaviour : BaseUIBehaviour
    {
        #region 组件
        
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
        
        #region Unity生命周期
        
        /// <summary>
        /// Unity Awake钩子
        /// </summary>
        protected override void Awake()
        {
            base.Awake(); // 初始化Pipeline
            
            // 获取组件
            Canvas = GetComponent<Canvas>();
            if (Canvas == null)
            {
                Canvas = GetComponentInChildren<Canvas>();
            }
            
            RectTransform = GetComponent<RectTransform>();
            
            // 获取配置
            _config = GetFinalConfig();
            
            // 绑定组件（自动生成的代码会重写此方法）
            BindComponents();
        }
        
        /// <summary>
        /// Unity OnEnable钩子
        /// 当GameObject被激活时自动调用
        /// </summary>
        protected virtual void OnEnable()
        {
            // 注册事件
            RegisterEvents();
        }
        
        /// <summary>
        /// Unity OnDisable钩子
        /// 当GameObject被禁用时自动调用
        /// </summary>
        protected virtual void OnDisable()
        {
            // 注销事件
            UnregisterEvents();
        }
        
        #endregion
        
        #region 配置系统
        
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
        
        #region 生命周期（重写BaseUIBehaviour的方法）
        
        /// <summary>
        /// 创建UI时调用
        /// </summary>
        protected override void OnCreate(params object[] args)
        {
            try
            {
                // MonoBehaviour版本中，GameObject已经存在
                // 不需要加载Prefab，只需要初始化
                
                // 根据配置初始化Attachments
                InitializeAttachmentsByConfig();
                
                FrameworkLogger.Info($"[UGUI] UI创建成功: {GetType().Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"[UGUI] UI创建失败: {GetType().Name}, {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        /// <summary>
        /// 显示UI时调用
        /// </summary>
        protected override void OnShow(params object[] args)
        {
            // GameObject.SetActive在DoShow中已经调用
            // 这里不需要再次调用
            
            FrameworkLogger.Info($"[UGUI] UI显示: {GetType().Name}");
        }
        
        /// <summary>
        /// 隐藏UI时调用
        /// </summary>
        protected override void OnHide(params object[] args)
        {
            // GameObject.SetActive在DoHide中会调用
            // 这里不需要再次调用
            
            FrameworkLogger.Info($"[UGUI] UI隐藏: {GetType().Name}");
        }
        
        /// <summary>
        /// UI销毁时的清理工作（重写父类的Pipeline钩子）
        /// 注意：这个方法通过Pipeline系统调用，不是Unity的OnDestroy
        /// Unity的OnDestroy()在BaseUIBehaviour中处理
        /// </summary>
        protected sealed override void OnDestroy(params object[] args)
        {
            // GameObject.Destroy在DoDestroy中会调用
            // 这里只需要清理资源
            CleanupResources();
            
            FrameworkLogger.Info($"[UGUI] UI销毁: {GetType().Name}");
        }
        
        /// <summary>
        /// 清理资源（避免方法名与Unity消息冲突）
        /// </summary>
        private void CleanupResources()
        {
            Canvas = null;
            RectTransform = null;
        }
        
        #endregion
        
        #region Attachment初始化
        
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
        
        #endregion
        
        #region 组件绑定（子类重写）
        
        /// <summary>
        /// 绑定组件（自动生成的代码会重写此方法）
        /// </summary>
        protected virtual void BindComponents()
        {
            // 默认不执行任何操作
            // 自动生成的Binding代码会重写此方法
        }
        
        #endregion
        
        #region 事件管理（子类重写）
        
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
        /// <param name="path">相对于当前GameObject的路径</param>
        /// <returns>找到的组件</returns>
        /// <exception cref="Exception">找不到节点或组件时抛出异常</exception>
        protected T FindComponent<T>(string path) where T : Component
        {
            // 查找节点
            var trans = transform.Find(path);
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
            var trans = transform.Find(path);
            return trans?.GetComponent<T>();
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
    }
}

