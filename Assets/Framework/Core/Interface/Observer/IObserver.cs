



namespace Framework.Core
{
    public interface IObserver<T>
    {
        public Actions<T, T> OnChange { get; }
    }
}