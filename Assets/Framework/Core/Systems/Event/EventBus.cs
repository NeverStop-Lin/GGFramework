using System;
using System.Collections.Generic;


namespace Framework.Core
{
    public static class EventBus
    {
        static readonly Dictionary<string, Actions<object, object, object>> _events
            = new Dictionary<string, Actions<object, object, object>>();


        public static Actions<object, object, object> On(string eventName)
        {
            if (_events.TryGetValue(eventName, out var value)) return value;

            value = new Actions<object, object, object>();
            _events[eventName] = value;
            return value;
        }

        public static Actions<object, object, object> On(string eventType, Enum eventName)
        {
            return On(ToEventString(eventType, eventName));
        }


        public static object[] Emit(string eventName, params object[] args)
        {
            return _events.TryGetValue(eventName, out var value)
                ? value.Invoke(args)
                : Array.Empty<object>();
        }
        public static object[] Emit(string eventType, Enum eventName, params object[] args)
        {
            return Emit(ToEventString(eventType, eventName), args);
        }

        static string ToEventString(string eventType, Enum eventName)
        {
            return $"{eventType}:{eventName.ToString()}";
        }

    }
}