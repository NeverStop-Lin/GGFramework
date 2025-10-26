using System;

namespace Framework.Core
{
    /// <summary>
    /// UI实例键
    /// 用于唯一标识一个UI实例（类型 + 实例ID）
    /// </summary>
    public struct UIInstanceKey : IEquatable<UIInstanceKey>
    {
        /// <summary>
        /// UI类型
        /// </summary>
        public Type UIType { get; }
        
        /// <summary>
        /// 实例ID（null表示默认实例）
        /// </summary>
        public string InstanceId { get; }
        
        public UIInstanceKey(Type uiType, string instanceId = null)
        {
            UIType = uiType;
            InstanceId = instanceId;
        }
        
        public bool Equals(UIInstanceKey other)
        {
            return UIType == other.UIType && InstanceId == other.InstanceId;
        }
        
        public override bool Equals(object obj)
        {
            return obj is UIInstanceKey other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                return ((UIType?.GetHashCode() ?? 0) * 397) ^ (InstanceId?.GetHashCode() ?? 0);
            }
        }
        
        public static bool operator ==(UIInstanceKey left, UIInstanceKey right)
        {
            return left.Equals(right);
        }
        
        public static bool operator !=(UIInstanceKey left, UIInstanceKey right)
        {
            return !left.Equals(right);
        }
        
        public override string ToString()
        {
            return string.IsNullOrEmpty(InstanceId) 
                ? $"{UIType?.Name}" 
                : $"{UIType?.Name}#{InstanceId}";
        }
    }
}

