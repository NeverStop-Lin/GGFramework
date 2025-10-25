using Defective.JSON;
using UnityEngine;

namespace Framework.Modules.Analytics
{
    /// <summary>
    /// 数据上报器基�?
    /// </summary>
    public abstract class BaseReporter
    {
        
        public void Initialize()
        {
            Debug.Log($"Reporter 初始�?{GetType().Name}");
            OnInitialize();
        }
        protected abstract void OnInitialize();

        public void Report(string eventName, JSONObject json = null)
        {
            OnReport(eventName, json);
        }

        protected abstract void OnReport(string eventName, JSONObject json);
    }
}