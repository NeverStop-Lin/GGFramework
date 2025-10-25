using Framework.Core;
using Framework.Modules.Advertise;
using Framework.Modules.Analytics;
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
    public class GridFramework : MonoBehaviour
    {
        public static GridFramework Instance;

        // ========== 核心系统访问接口 ==========

        /// <summary>依赖注入容器</summary>
        public static DiContainer Container => Instance?._container;

        /// <summary>UI系统</summary>
        public static IUI UI => Instance?._ui;

        /// <summary>配置系统</summary>
        public static IConfigs Config => Instance?._config;

        /// <summary>观察者系�?/summary>
        public static IObservers Observer => Instance?._observers;

        /// <summary>定时器系�?/summary>
        public static TimerCenter Timer => Instance?._timer;

        /// <summary>存储系统</summary>
        public static IStorage Storage => Instance?._storage;




        // ========== 依赖注入 ==========

        [Inject] private DiContainer _container;
        [Inject] private IStorage _storage;
        [Inject] private IUI _ui;
        [Inject] private TimerCenter _timer;
        [Inject] private IObservers _observers;
        [Inject] private IConfigs _config;



        // ========== 通用模块 ==========

        /// <summary>音效模块</summary>
        public static SoundModule Sound => Instance?._sound;

        /// <summary>数据分析上报模块</summary>
        public static AnalyticsModule Analytics => Instance?._analytics;

        /// <summary>广告模块</summary>
        public static AdvertiseModule Advertise => Instance?._advertise;

        /// <summary>用户管理模块</summary>
        public static UserModule User => Instance?._user;

        /// <summary>通用UI组件模块</summary>
        public static CommonUIModule CommonUI => Instance?._commonUI;

        // ========== 依赖注入 ==========
        [Inject] private SoundModule _sound;
        [Inject] private AnalyticsModule _analytics;
        [Inject] private AdvertiseModule _advertise;
        [Inject] private UserModule _user;
        [Inject] private CommonUIModule _commonUI;


        void Awake()
        {
            Instance = this;
        }

        void FixedUpdate()
        {
            _timer?.OnFixedUpdate();
        }

        void Update()
        {
            _timer?.OnUpdate(Time.deltaTime);
        }
    }
}