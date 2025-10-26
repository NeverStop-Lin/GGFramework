using System;
using System.Threading.Tasks;
using Framework.Scripts;
using UnityEngine;
using Zenject;

namespace Framework.Core
{
    /// <summary>
    /// UI基类（MonoBehaviour + UGUI）
    /// 提供生命周期管理、UGUI组件绑定
    /// </summary>
    public abstract class UIBehaviour : MonoBehaviour, IBaseUI
    {
        #region 依赖注入

        [Inject]
        protected IUI Center;

        #endregion

        #region UGUI组件

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
        private UIInstanceConfig _config;

        /// <summary>
        /// UI层级名称
        /// </summary>
        public string LayerName { get; set; } = "Main";

        #endregion

        #region Unity生命周期

        /// <summary>
        /// Unity Awake钩子（MonoBehaviour创建时）
        /// 子类重写时必须调用base.Awake()
        /// </summary>
        protected virtual void Awake()
        {
            // 获取UGUI组件
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
        /// Unity OnEnable钩子（GameObject激活时）
        /// </summary>
        protected virtual void OnEnable()
        {
            // 注册事件
            RegisterEvents();
        }

        /// <summary>
        /// Unity OnDisable钩子（GameObject禁用时）
        /// </summary>
        protected virtual void OnDisable()
        {
            // 注销事件
            UnregisterEvents();
        }

        /// <summary>
        /// Unity OnDestroy钩子（GameObject销毁时）
        /// </summary>
        protected virtual void OnDestroy()
        {
            // Unity直接销毁GameObject时，执行清理
            if (gameObject != null)
            {
                OnRemove();
            }
        }

        #endregion

        #region IBaseUI接口实现

        /// <summary>
        /// 初始化UI
        /// </summary>
        public void Initialize()
        {
            // MonoBehaviour版本中，大部分初始化在Awake中完成
            // 此方法保留用于兼容性
        }

        /// <summary>
        /// 执行Create
        /// </summary>
        public async Task<object> DoCreate(params object[] args)
        {
            OnCreate(args);
            return await Task.FromResult<object>(null);
        }

        /// <summary>
        /// 执行Show
        /// </summary>
        public async Task<object> DoShow(params object[] args)
        {
            // 激活GameObject
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            OnShow(args);
            return await Task.FromResult<object>(null);
        }

        /// <summary>
        /// 执行Show动画
        /// </summary>
        public async Task<object> DoShowAnim(params object[] args)
        {
            await PlayShowAnimation();
            OnShowAnim(args);
            return await Task.FromResult<object>(null);
        }

        /// <summary>
        /// 执行Hide
        /// </summary>
        public async Task<object> DoHide(params object[] args)
        {
            OnHide(args);
            return await Task.FromResult<object>(null);
        }

        /// <summary>
        /// 执行Hide动画
        /// </summary>
        public async Task<object> DoHideAnim(params object[] args)
        {
            await PlayHideAnimation();
            OnHideAnim(args);

            // 禁用GameObject
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            return await Task.FromResult<object>(null);
        }

        /// <summary>
        /// 执行Destroy
        /// </summary>
        public async Task<object> DoDestroy(params object[] args)
        {
            OnRemove(args);

            Canvas = null;
            RectTransform = null;

            // 销毁GameObject
            if (gameObject != null)
            {
                Destroy(gameObject);
            }

            return await Task.FromResult<object>(null);
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 隐藏当前UI
        /// </summary>
        public Task<object> Hide(params object[] args)
        {
            return Center.Hide(this.GetType(), args);
        }

        #endregion

        #region 配置系统

        /// <summary>
        /// 获取UI配置（从UIProjectConfig读取）
        /// </summary>
        private UIInstanceConfig GetFinalConfig()
        {
            var config = UIProjectConfigManager.GetUIInstanceConfig(GetType());

            // 从配置中获取层级名称
            if (config != null)
            {
                LayerName = config.LayerName;
            }

            return config;
        }

        #endregion

        #region 子类重写方法

        /// <summary>
        /// UI创建时调用
        /// </summary>
        protected virtual void OnCreate(params object[] args) { }

        /// <summary>
        /// UI显示时调用
        /// </summary>
        protected virtual void OnShow(params object[] args) { }

        /// <summary>
        /// 播放显示动画（子类可覆盖自定义动画）
        /// </summary>
        protected virtual Task PlayShowAnimation()
        {
            // 默认无动画，立即完成
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示动画播放完成时调用
        /// </summary>
        protected virtual void OnShowAnim(params object[] args) { }

        /// <summary>
        /// UI隐藏时调用
        /// </summary>
        protected virtual void OnHide(params object[] args) { }
        /// <summary>
        /// 播放隐藏动画（子类可覆盖自定义动画）
        /// </summary>
        protected virtual Task PlayHideAnimation()
        {
            // 默认无动画，立即完成
            return Task.CompletedTask;
        }

        /// <summary>
        /// 隐藏动画播放完成时调用
        /// </summary>
        protected virtual void OnHideAnim(params object[] args) { }

        /// <summary>
        /// UI移除时调用
        /// 子类可以重写此方法来清理资源和执行业务逻辑
        /// </summary>
        protected virtual void OnRemove(params object[] args) { }

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
                throw new Exception($"[UI] 找不到节点: {path} in {GetType().Name}");
            }

            // 获取组件
            var component = trans.GetComponent<T>();
            if (component == null)
            {
                throw new Exception($"[UI] 找不到组件: {typeof(T).Name} at {path} in {GetType().Name}");
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
        public virtual int GetIndex()
        {
            return Canvas != null ? Canvas.sortingOrder : 0;
        }

        /// <summary>
        /// 设置UI的层级索引（通过修改Canvas的sortingOrder）
        /// </summary>
        public virtual void SetIndex(int i)
        {
            if (Canvas != null)
            {
                Canvas.sortingOrder = i;
            }
        }

        #endregion
    }
}
