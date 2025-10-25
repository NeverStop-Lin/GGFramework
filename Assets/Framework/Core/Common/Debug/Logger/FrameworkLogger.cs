namespace Framework.Core
{
    using UnityEngine;
    using System.Diagnostics;

    public static class FrameworkLogger
    {


        [DebuggerHidden]
        public static void Info(object message) => UnityEngine.Debug.Log(message);


        // ���Ƶ�ʵ��LogWarning��LogError����
        public static void Warn(object message) => UnityEngine.Debug.LogWarning(message);

        public static void Error(object message) => UnityEngine.Debug.LogError(message);

    } 
}