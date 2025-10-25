using Zenject;

namespace Framework.Core
{

    public class CacheObserver<T> : ValueObserver<T>
    {

        [Inject]
        IStorage _storage;

        string _cacheKey;

        public T _defaultValue;

        public void Initialize(string cacheKey, T value) { OnInitialize(cacheKey, value); }

        void OnInitialize(string cacheKey, T value)
        {
            _defaultValue = DeepCopyByJson(value);
            _cacheKey = cacheKey;
            _value = _storage.Load(_cacheKey, value);
            base.OnInitialize(_value);
        }

        public void Reset()
        {
            _value = DeepCopyByJson(_defaultValue);
        }
        
        // ���� JSON �������
        private static T DeepCopyByJson(T obj)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        protected override void Notify(params object[] args)
        {
            base.Notify(args);
            _storage.Save(_cacheKey, Value);
        }
    }
}