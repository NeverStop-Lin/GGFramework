using Newtonsoft.Json;
using UnityEngine;

namespace Framework.Core
{
    public abstract class BaseConfig<T> : IConfig
    {
        object _value = null;
        public T Value
        {
            get
            {
                if (_value == null)
                    Load();
                return (T)_value;
            }
            private set { _value = value; }
        }

        public virtual string Url { get; set; }
        
        // 资源加载器（注入）
        private static IResource _resourceLoader;
        
        public static void SetResourceLoader(IResource resource)
        {
            _resourceLoader = resource;
        }

        void Load()
        {
            if (_resourceLoader == null)
            {
                Debug.LogError($"[BaseConfig] 资源加载器未初始化，请先调用 BaseConfig<T>.SetResourceLoader()");
                return;
            }
            
            var textAsset = _resourceLoader.Load<TextAsset>(Url);
            
            if (textAsset != null)
            {
                Value = JsonConvert.DeserializeObject<T>(textAsset.text);
            }
            else
            {
                Debug.LogError($"[BaseConfig] 配置文件加载失败: {Url}");
            }
        }

        //TODO @stone �ֵ�֧�֣�
    }
}