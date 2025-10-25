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

        void Load()
        {
            var jsonStr = Resources.Load<TextAsset>(Url).text;
            Value = JsonConvert.DeserializeObject<T>(jsonStr);
        }

        //TODO @stone �ֵ�֧�֣�
    }
}