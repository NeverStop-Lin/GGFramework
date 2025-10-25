using System;


namespace Framework.Core
{
    [Serializable]
    public class IDayData<T>
    {
        public long Time;
        public T Data;
    }

    public class DayCacheObserver<T> : CacheObserver<IDayData<T>>, IValueObserver<T>
    {

        new public Actions<T, T> OnChange { get; } = new Actions<T, T>();
        new public T Value
        {
            get
            {

                TryReset();
                return base.Value.Data;
            }
            set
            {
                TryReset();
                var newValue = base.Value;
                newValue.Data = value;
                base.Value = newValue;
            }
        }
        Func<T, bool> _onReset = null;

        public void Initialize(string cacheKey, T value, Func<T, bool> onReset)
        {
            _onReset = onReset;
            Initialize(cacheKey, new IDayData<T>
            {
                Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Data = value
            });
            base.OnChange.Add((v) =>
            {
                OnChange.Invoke(v.Data);
            });
            OnChange.AutoInvoke(() => new object[]
            {
                Value, Value
            });
        }

        protected override void OnInitialize(IDayData<T> value)
        {
            base.OnInitialize(value);
            TryReset();
        }

        public void TryReset()
        {
            if ((_onReset?.Invoke(base.Value.Data) ?? false) == false)
            {
                return;
            }

            if (base.Value.Time == 0)
            {
                base.Value.Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            if (base.Value.Time.IsToday()) return;
            Reset();
            base.Value.Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

    }
}