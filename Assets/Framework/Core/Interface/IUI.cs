

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
        /// 显示UI
        /// </summary>
        UiLifeCycle<T> Show<T>(params object[] args) where T : IBaseUI, new();
        
        /// <summary>
        /// 隐藏UI
        /// </summary>
        Task<object> Hide<T>(params object[] args);
        
        /// <summary>
        /// 隐藏UI（通过Type）
        /// </summary>
        Task<object> Hide(Type uiType, params object[] args);
        
        #endregion
        
        #region 实例管理
        
        /// <summary>
        /// 销毁指定UI
        /// </summary>
        Task DestroyUI<T>() where T : IBaseUI;
        
        /// <summary>
        /// 销毁所有UI
        /// </summary>
        Task DestroyAllUI();
        
        /// <summary>
        /// 获取UI实例
        /// </summary>
        T GetUI<T>() where T : IBaseUI;
        
        /// <summary>
        /// 检查UI是否正在显示
        /// </summary>
        bool IsShowing<T>() where T : IBaseUI;
        
        /// <summary>
        /// 获取所有显示中的UI
        /// </summary>
        List<IBaseUI> GetAllShowingUIs();
        
        #endregion
        
        #region 栈管理
        
        /// <summary>
        /// 推入UI栈（显示UI）
        /// </summary>
        UiLifeCycle<T> PushUI<T>(params object[] args) where T : IBaseUI, new();
        
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
        /// 预加载UI
        /// </summary>
        Task PreloadUI<T>() where T : IBaseUI, new();
        
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