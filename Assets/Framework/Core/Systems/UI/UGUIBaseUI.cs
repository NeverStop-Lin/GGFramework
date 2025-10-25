using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// 基于UGUI的BaseUI实现
    /// 提供UI对象的基础管理功能
    /// </summary>
    public abstract class UGUIBaseUI : BaseUI
    {
        /// <summary>
        /// UI游戏对象
        /// </summary>
        protected GameObject UIObject;
        
        /// <summary>
        /// UI的Canvas组件
        /// </summary>
        protected Canvas Canvas;
        
        /// <summary>
        /// 获取UI的层级索引（基于Canvas的sortingOrder�?
        /// </summary>
        public override int GetIndex()
        {
            return Canvas != null ? Canvas.sortingOrder : 0;
        }
        
        /// <summary>
        /// 设置UI的层级索引（通过修改Canvas的sortingOrder�?
        /// </summary>
        public override void SetIndex(int i)
        {
            if (Canvas != null)
            {
                Canvas.sortingOrder = i;
            }
        }
        
        /// <summary>
        /// 创建UI时调用，子类需要实现CreateUIObject来创建或加载UI预制�?
        /// </summary>
        protected override void OnCreate(params object[] args)
        {
            CreateUIObject();
            
            // 自动获取Canvas组件
            if (UIObject != null && Canvas == null)
            {
                Canvas = UIObject.GetComponent<Canvas>();
                if (Canvas == null)
                {
                    Canvas = UIObject.GetComponentInChildren<Canvas>();
                }
            }
        }
        
        /// <summary>
        /// 子类需要实现此方法来创建或加载UI预制�?
        /// 通常从Resources或Addressables加载预制体并实例�?
        /// </summary>
        protected abstract void CreateUIObject();
        
        /// <summary>
        /// 显示UI时调�?
        /// </summary>
        protected override void OnShow(params object[] args)
        {
            if (UIObject != null)
            {
                UIObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// 隐藏UI时调�?
        /// </summary>
        protected override void OnHide(params object[] args)
        {
            if (UIObject != null)
            {
                UIObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 销毁UI时调�?
        /// </summary>
        protected override void OnDestroy(params object[] args)
        {
            if (UIObject != null)
            {
                Object.Destroy(UIObject);
                UIObject = null;
            }
        }
    }
}


