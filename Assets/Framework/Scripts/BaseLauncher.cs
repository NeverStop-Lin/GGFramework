using UnityEngine;

namespace Framework.Scripts
{
    /// <summary>
    /// 游戏启动器基�?
    /// 项目可以继承此类实现自己的启动逻辑
    /// </summary>
    public class BaseLauncher : MonoBehaviour
    {
        void Start()
        {
            FrameworkInitialize();
            BusinessInitialize();
        }

        /// <summary>
        /// 框架初始�?
        /// 在此方法中进行框架相关的初始�?
        /// </summary>
        protected virtual void FrameworkInitialize() { }

        /// <summary>
        /// 业务初始�?
        /// 在此方法中进行游戏业务逻辑的初始化
        /// </summary>
        protected virtual void BusinessInitialize() { }
    }
}