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
    /// 框架核心入口
    /// 提供基础设施层核心系统的统一访问接口
    /// </summary>
    public class GGF : MonoInstaller
    {
        static DiContainer Container;

        // ========== 核心系统访问接口 ==========


        /// <summary>UI系统</summary>
        public static IUI UI => Container?.Resolve<IUI>();

        /// <summary>配置系统</summary>
        public static IConfigs Config => Container?.Resolve<IConfigs>();

        /// <summary>观察者系统</summary>
        public static IObservers Observer => Container?.Resolve<IObservers>();

        /// <summary>定时器系统</summary>
        public static TimerCenter Timer => Container?.Resolve<TimerCenter>();

        /// <summary>存储系统</summary>
        public static IStorage Storage => Container?.Resolve<IStorage>();

        /// <summary>资源管理系统</summary>
        public static IResource Resource => Container?.Resolve<IResource>();


        // ========== 通用模块 ==========

        /// <summary>音效模块</summary>
        public static SoundModule Sound => Container?.Resolve<SoundModule>();

        /// <summary>数据分析上报模块</summary>
        public static AnalyticsModule Analytics => Container?.Resolve<AnalyticsModule>();

        /// <summary>广告模块</summary>
        public static AdvertiseModule Advertise => Container?.Resolve<AdvertiseModule>();

        /// <summary>用户管理模块</summary>
        public static UserModule User => Container?.Resolve<UserModule>();

        /// <summary>通用UI组件模块</summary>
        public static CommonUIModule CommonUI => Container?.Resolve<CommonUIModule>();

        /// <summary>对象池模块</summary>
        public static PoolModule Pool => Container?.Resolve<PoolModule>();


        public override void InstallBindings()
        {

            Container = base.Container;
        }

        void FixedUpdate()
        {
            Timer?.OnFixedUpdate();
        }

        void Update()
        {
            Timer?.OnUpdate(Time.deltaTime);
        }
    }
}