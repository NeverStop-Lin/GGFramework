


namespace Framework.Core
{
    public interface IValueObserver<T> : IObserver<T>
    {

        public T Value { get; set; }

        public void Notify() { OnChange.Invoke(Value, null); }

    }
}