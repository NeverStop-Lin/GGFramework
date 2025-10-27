using System;

namespace Framework.Core
{
    /// <summary>
    /// object 数组工具类
    /// 提供类型安全的数组元素访问
    /// </summary>
    public static class ObjectArrayUtils
    {
        /// <summary>
        /// 从 object 数组中获取指定索引的元素并转换为目标类型
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="args">object 数组</param>
        /// <param name="index">索引</param>
        /// <param name="defaultValue">默认值（仅用于可选参数场景）</param>
        /// <returns>转换后的值</returns>
        /// <exception cref="ArgumentNullException">当数组为 null 时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当索引越界时抛出</exception>
        /// <exception cref="InvalidCastException">当类型不匹配时抛出</exception>
        public static T CastTo<T>(this object[] args, int index, T defaultValue = default)
        {
            // 参数检查
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args), "参数数组为 null");
            }

            // 索引越界检查
            if (index < 0 || index >= args.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), 
                    $"索引 {index} 超出数组范围 [0, {args.Length - 1}]");
            }

            // 类型转换检查
            object value = args[index];
            if (value is T matchedValue)
            {
                return matchedValue;
            }

            // 类型不匹配时抛出异常
            throw new InvalidCastException(
                $"索引 {index} 的类型不匹配，期望 {typeof(T).Name}，实际 {value?.GetType().Name ?? "null"}");
        }
    }
}