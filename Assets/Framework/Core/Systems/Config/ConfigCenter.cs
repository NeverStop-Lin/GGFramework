using System;
using System.Collections.Generic;


namespace Framework.Core
{
    public class ConfigCenter : IConfigs
    {
        private readonly Dictionary<Type, object> _configs = new Dictionary<Type, object>();

        public T Get<T>() where T : IConfig, new()
        {
            var key = typeof(T);
            if (_configs.TryGetValue(key, out var config)) return (T)config;

            config = new T();
            _configs[key] = config;

            return (T)config;
        }

    }
}