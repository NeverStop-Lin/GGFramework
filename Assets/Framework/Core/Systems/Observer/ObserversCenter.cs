using System;
using System.Collections.Generic;
using Zenject;

namespace Framework.Core
{
    public class ObserversCenter : IObservers
    {
        [Inject]
        DiContainer _container;

        Dictionary<string, object> _cacheObservers = new Dictionary<string, object>();

        public IValueObserver<T> Cache<T>(string key, T value)
        {
            if (_cacheObservers.TryGetValue(key, out var observer)) return observer as IValueObserver<T>;
            var newObs
                = _container.Resolve<PlaceholderFactory<Type, Observer>>().Create(typeof(CacheObserver<T>)) as
                    CacheObserver<T>;
            newObs?.Initialize(key, value);
            _cacheObservers[key] = newObs;
            return newObs;
        }

        public IValueObserver<T> Value<T>(T value)
        {
            var observer
                = _container.Resolve<PlaceholderFactory<Type, Observer>>().Create(typeof(ValueObserver<T>)) as
                    ValueObserver<T>;
            observer?.Initialize(value);
            return observer;
        }
        public IValueObserver<T> DayCache<T>(string key, T value, Func<T, bool> onReset = null)
        {
            var observer
                = _container.Resolve<PlaceholderFactory<Type, Observer>>().Create(typeof(DayCacheObserver<T>)) as
                    DayCacheObserver<T>;
            observer?.Initialize(key, value, onReset);
            var result = (IValueObserver<T>)observer;
            return result;
        }

    }
}