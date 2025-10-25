using Framework.Scripts;

namespace Framework.Scripts
{
    /// <summary>
    /// 游戏启动器示例
    /// 项目可以基于 BaseLauncher 创建自己的启动器
    /// </summary>
    public partial class Launcher : BaseLauncher
    {
        protected override void FrameworkInitialize()
        {
            base.FrameworkInitialize();
            // 在此添加框架初始化逻辑
        }

        protected override void BusinessInitialize()
        {
            base.BusinessInitialize();
            // 在此添加业务初始化逻辑
        }
    }
}