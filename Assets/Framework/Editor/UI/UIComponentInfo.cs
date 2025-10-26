#if UNITY_EDITOR
using System;

namespace Framework.Editor.UI
{
    /// <summary>
    /// UI组件信息
    /// 记录从Prefab中扫描出的组件信息
    /// </summary>
    [Serializable]
    public class UIComponentInfo
    {
        /// <summary>
        /// 组件名称（去掉@前缀和类型前缀）
        /// 例如：@Button_Start -> Start
        /// </summary>
        public string ComponentName;
        
        /// <summary>
        /// 组件类型名称（Button、Text、Image等）
        /// </summary>
        public string ComponentTypeName;
        
        /// <summary>
        /// 组件的完整类型（含命名空间）
        /// 例如：UnityEngine.UI.Button
        /// </summary>
        public string FullTypeName;
        
        /// <summary>
        /// 组件在Prefab中的完整路径
        /// 例如：Panel/Buttons/@Button_Start
        /// </summary>
        public string Path;
        
        /// <summary>
        /// 原始节点名称（带@前缀）
        /// 例如：@Button_Start
        /// </summary>
        public string OriginalName;
        
        /// <summary>
        /// 生成的字段名称
        /// 例如：_startButton
        /// </summary>
        public string FieldName;
        
        /// <summary>
        /// 是否是Button（需要生成事件绑定）
        /// </summary>
        public bool IsButton;
        
        /// <summary>
        /// 生成的事件处理方法名
        /// 例如：OnStartClick
        /// </summary>
        public string EventHandlerName;
        
        public override string ToString()
        {
            return $"{ComponentTypeName} {FieldName} @ {Path}";
        }
    }
}
#endif
