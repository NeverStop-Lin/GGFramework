namespace Framework.Core
{

    /// <summary>
    /// 存储接口抽象
    /// </summary>
    public interface IStorage
    {
        public void Save<T>(string key, T data);
        public T Load<T>(string key, T defaultValue = default);
        public bool HasKey(string key);
    }
}