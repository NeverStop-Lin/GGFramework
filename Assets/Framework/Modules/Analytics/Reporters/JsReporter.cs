using System.Runtime.InteropServices;
using Defective.JSON;
using UnityEngine;

namespace Framework.Modules.Analytics
{
    /// <summary>
    /// JavaScript 上报�?
    /// 用于 WebGL 平台的数据上�?
    /// </summary>
    public class JsReporter : BaseReporter
        
    {
       
        [DllImport("__Internal")]
        static extern void JsReport(string eventID, string evenData);

        protected override void OnInitialize()
        {

        }
        protected override void OnReport(string eventName, JSONObject json)
        {
            var eventData = json?.ToString() ?? "";
            Debug.Log("JsReport: " + eventName + " / " + eventData);
            JsReport(eventName, eventData);
        }
    }
}