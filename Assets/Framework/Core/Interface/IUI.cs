

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Core
{
    /// <summary>
    /// UI管理接口
    /// 提供UI的显示、隐藏、销毁等核心功能
    /// </summary>
    public interface IUI
    {
        #region 基础API
        
        /// <summary>
        /// 显示UI（单例或默认实例）
        /// </summary>
        UiLifeCycle<T> Show<T>(params object[] args) where T : IBaseUI;
        
        /// <summary>
        /// 显示UI的指定实例（多实例模式）
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="args">传递给UI的参数</param>
        UiLifeCycle<T> ShowInstance<T>(string instanceId, params object[] args) where T : IBaseUI;
        
        /// <summary>
        /// 隐藏UI（单例或默认实例）
        /// </summary>
        Task<object> Hide<T>(params object[] args) where T : IBaseUI;
        
        /// <summary>
        /// 隐藏UI的指定实例（多实例模式）
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="args">传递给UI的参数</param>
        Task<object> HideInstance<T>(string instanceId, params object[] args) where T : IBaseUI;
        
        /// <summary>
        /// 隐藏UI（通过Type）
        /// </summary>
        Task<object> Hide(Type uiType, params object[] args);
        
        /// <summary>
        /// 隐藏UI（通过Type和实例ID）
        /// </summary>
        Task<object> Hide(Type uiType, string instanceId, params object[] args);
        
        #endregion
        
        #region 实例管理
        
        /// <summary>
        /// 移除指定UI（单例或默认实例）
        /// </summary>
        Task RemoveUI<T>() where T : IBaseUI;
        
        /// <summary>
        /// 移除指定UI（支持多实例）
        /// </summary>
        Task RemoveUI<T>(string instanceId) where T : IBaseUI;
        
        /// <summary>
        /// 移除指定UI（通过Type）
        /// </summary>
        Task RemoveUI(Type uiType);
        
        /// <summary>
        /// 移除指定UI（通过Type和实例ID）
        /// </summary>
        Task RemoveUI(Type uiType, string instanceId);
        
        /// <summary>
        /// 移除指定类型的所有实例
        /// </summary>
        Task RemoveAllInstancesOf<T>() where T : IBaseUI;
        
        /// <summary>
        /// 移除所有UI
        /// </summary>
        Task RemoveAllUI();
        
        /// <summary>
        /// 获取UI实例（单例或默认实例）
        /// </summary>
        T GetUI<T>() where T : IBaseUI;
        
        /// <summary>
        /// 获取UI实例（支持多实例）
        /// </summary>
        T GetUI<T>(string instanceId) where T : IBaseUI;
        
        /// <summary>
        /// 检查UI是否正在显示（单例或是否有任何实例显示）
        /// </summary>
        bool IsShowing<T>() where T : IBaseUI;
        
        /// <summary>
        /// 检查UI实例是否正在显示（支持多实例）
        /// </summary>
        bool IsShowing<T>(string instanceId) where T : IBaseUI;
        
        /// <summary>
        /// 获取所有显示中的UI
        /// </summary>
        List<IBaseUI> GetAllShowingUIs();
        
        #endregion
        
        #region 栈管理
        
        /// <summary>
        /// 推入UI栈（显示UI）
        /// </summary>
        UiLifeCycle<T> PushUI<T>(params object[] args) where T : IBaseUI;
        
        /// <summary>
        /// 弹出栈顶UI（返回功能）
        /// </summary>
        Task<IBaseUI> PopUI();
        
        /// <summary>
        /// 获取UI栈
        /// </summary>
        List<IBaseUI> GetUIStack();
        
        /// <summary>
        /// 清空UI栈
        /// </summary>
        Task ClearUIStack();
        
        #endregion
        
        #region 批量操作
        
        /// <summary>
        /// 隐藏所有UI（可排除）
        /// </summary>
        Task HideAll(params Type[] except);
        
        #endregion
        
        #region 预加载
        
        /// <summary>
        /// 预加载UI（预加载Prefab资源）
        /// </summary>
        Task PreloadUI<T>() where T : IBaseUI;
        
        #endregion
        
        #region 层级管理
        
        /// <summary>
        /// 设置UI层级
        /// </summary>
        void SetUILayer<T>(int layer) where T : IBaseUI;
        
        /// <summary>
        /// 置顶UI
        /// </summary>
        void BringToFront<T>() where T : IBaseUI;
        
        #endregion
        
        #region 状态查询
        
        /// <summary>
        /// 获取UI状态
        /// </summary>
        UIRuntimeState GetUIState<T>() where T : IBaseUI;
        
        /// <summary>
        /// 获取UI数量
        /// </summary>
        int GetUICount();
        
        #endregion
    }
}