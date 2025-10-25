
using Newtonsoft.Json;
using UnityEngine;

namespace Framework.Core
{
    public class PlayerPrefsStorage : BaseClass<StorageOptions>, IStorage
    {


        string Prefix
        {
            get { return Options.prefix; }
        }
        public void Save<T>(string key, T data)
        {
            var newKey = $"{Prefix}_{key}";
            var json = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString(newKey, json);
            PlayerPrefs.Save();
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            var newKey = $"{Prefix}_{key}";
            if (!PlayerPrefs.HasKey(newKey)) return defaultValue;
            var json = PlayerPrefs.GetString(newKey);
            if (json == "") return defaultValue;
            return JsonConvert.DeserializeObject<T>(json);
        }

        public bool HasKey(string key) => PlayerPrefs.HasKey($"{Prefix}_{key}");
    }
}