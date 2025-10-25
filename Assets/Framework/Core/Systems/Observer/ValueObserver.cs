
namespace Framework.Core
{
    public class ValueObserver<T> : BaseObserver<T>, IValueObserver<T>
    {
        protected T _value;
        public T Value
        {
            get { return _value; }
            set
            {
                T oldValue = _value;
                _value = value;
                Notify(_value, oldValue);
            }
        }
        public void Initialize(T value) { OnInitialize(value); }

        protected virtual void OnInitialize(T value)
        {
            _value = value;

            OnChange.AutoInvoke(() => new object[]
            {
                Value, Value
            });
        }
    }
}