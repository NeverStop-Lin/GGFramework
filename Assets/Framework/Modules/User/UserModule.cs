using Framework.Core;
using Zenject;

namespace Framework.Modules.User
{
    /// <summary>
    /// 用户管理模块
    /// 提供用户数据和物品管理功�?
    /// </summary>
    public class UserModule
    {
        [Inject] private IConfigs _configs;
        [Inject] private IObservers _observers;
        
        // 项目可以根据需求扩展此模块
        // 例如：用户信息管理、物品背包系统、货币管理等
    }
}