using System;

namespace Framework.Core
{
    public interface IObservers
    {
        public IValueObserver<T> Cache<T>(string key, T value);
        public IValueObserver<T> Value<T>(T value);
        public IValueObserver<T> DayCache<T>(string key, T value, Func<T, bool> onReset);
    }
}