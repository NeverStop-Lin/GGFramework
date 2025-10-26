using System;
using Framework.Core;
using Framework.Core.Resource;
using Framework.Modules.Advertise;
using Framework.Modules.Analytics;
using Framework.Modules.Pool;
using Framework.Modules.Sound;
using Framework.Modules.UI;
using Framework.Modules.User;
using UnityEngine;
using Zenject;

namespace Framework.Scripts
{
    /// <summary>
    /// 基础设施层安装器
    /// 绑定框架核心系统
    /// </summary>
    [CreateAssetMenu(fileName = "FrameworkInstaller", menuName = "Framework/Installers/FrameworkInstaller")]
    public class FrameworkInstaller : ScriptableObjectInstaller<FrameworkInstaller>
    {
        public StorageOptions storage;

        public override void InstallBindings()
        {
            // 绑定配置选项
            Container.BindInstance(storage);

            // 绑定核心系统接口
            Container.Bind<IStorage>().To<StorageCenter>().AsSingle();
            Container.Bind<TimerCenter>().To<TimerCenter>().AsSingle();
            Container.Bind<IUI>().To<UICenter>().AsSingle();
            Container.Bind<IObservers>().To<ObserversCenter>().AsSingle();
            Container.Bind<IConfigs>().To<ConfigCenter>().AsSingle();
            Container.Bind<IResource>().To<ResourceCenter>().AsSingle();

            // 绑定通用模块
            Container.Bind<SoundModule>().To<SoundModule>().AsSingle();
            Container.Bind<AnalyticsModule>().To<AnalyticsModule>().AsSingle();
            Container.Bind<AdvertiseModule>().To<AdvertiseModule>().AsSingle();
            Container.Bind<UserModule>().To<UserModule>().AsSingle();
            Container.Bind<CommonUIModule>().To<CommonUIModule>().AsSingle();
            Container.Bind<PoolModule>().To<PoolModule>().AsSingle();

            // 绑定工厂
            // 使用新的MonoBehaviour工厂（UI类已迁移到MonoBehaviour）
            Container.BindFactory<Type, IBaseUI, PlaceholderFactory<Type, IBaseUI>>()
                .FromFactory<UIFactoryBehaviour>();

            Container.BindFactory<Type, Observer, PlaceholderFactory<Type, Observer>>()
                .FromFactory<ObserverFactory>();
        }
    }
}