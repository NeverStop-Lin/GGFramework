namespace Framework.Core
{
    public abstract class BaseClass<T>
    {

        protected T Options { get; private set; }

        public void Initialize(T options)
        {
            Options = options;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }
    }

}