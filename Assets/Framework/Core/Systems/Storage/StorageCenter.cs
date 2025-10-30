using Zenject;

namespace Framework.Core
{
    public class StorageCenter : IStorage
    {

      [Inject]  StorageOptions _options;

        readonly IStorage _storage;

        StorageCenter()
        {
            switch (_options.storageType)
            {
                default:
                    {
                        _storage = new PlayerPrefsStorage();
                        break;
                    }
            }
            ((BaseClass<StorageOptions>)_storage)?.Initialize(_options);
        }

        public void Save<T>(string key, T data) { _storage.Save(key, data); }
        public T Load<T>(string key, T defaultValue = default)
        {
            return _storage.Load(key, defaultValue);
        }
        public bool HasKey(string key) { return _storage.HasKey(key); }

    }
}