using System;
using System.Collections.Generic;
using Zenject;

namespace Framework.Core
{
    public class ConfigCenter : IConfigs
    {
        private readonly Dictionary<Type, object> _configs = new Dictionary<Type, object>();
        
        [Inject]
        private IResource _resource;
        
        // 初始化标记
        private bool _initialized = false;

        public T Get<T>() where T : IConfig, new()
        {
            // 首次调用时初始化BaseConfig的资源加载器
            if (!_initialized)
            {
                BaseConfig<object>.SetResourceLoader(_resource);
                _initialized = true;
            }
            
            var key = typeof(T);
            if (_configs.TryGetValue(key, out var config)) return (T)config;

            config = new T();
            _configs[key] = config;

            return (T)config;
        }
    }
}