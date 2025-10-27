using Newtonsoft.Json;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// 配置基类的非泛型部分
    /// 用于存储所有泛型配置共享的静态资源加载器
    /// </summary>
    public abstract class BaseConfigCore
    {
        // 资源加载器（所有BaseConfig共享）
        protected static IResource _resourceLoader;
        
        public static void SetResourceLoader(IResource resource)
        {
            _resourceLoader = resource;
        }
    }
    
    /// <summary>
    /// 配置基类（泛型）
    /// 提供配置数据的自动加载功能
    /// </summary>
    public abstract class BaseConfig<T> : BaseConfigCore, IConfig
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
    }
}