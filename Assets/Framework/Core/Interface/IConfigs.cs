

namespace Framework.Core
{
    public interface IConfigs
    {
        public T Get<T>() where T : IConfig, new();
    }
}