using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Framework.Core
{
    /// <summary>
    /// 事件订阅系统，支持添加、移除、自动调用等功能
    /// </summary>
    public class Actions
    {
        /// <summary>
        /// 订阅项，包含回调函数、目标对象和一次性标记
        /// </summary>
        public struct ActionItem
        {
            /// <summary>订阅的回调函数（委托）</summary>
            public object Action;
            
            /// <summary>订阅者对象，用于批量移除</summary>
            public object Target;
            
            /// <summary>是否为一次性订阅（执行后自动移除）</summary>
            public bool Once;
        }

        /// <summary>
        /// 所有订阅项列表
        /// </summary>
        readonly List<ActionItem> _actions = new List<ActionItem>();

        /// <summary>
        /// 自动调用函数，首次订阅时（first=true）会调用此函数获取参数并立即执行
        /// 用于同步初始状态，例如新订阅者立即获取当前值
        /// </summary>
        Func<object[]> _autoInvokeFunc;

        /// <summary>
        /// 设置自动调用函数，用于首次订阅时立即执行
        /// </summary>
        /// <param name="func">返回调用参数的函数</param>
        public void AutoInvoke(Func<object[]> func) { _autoInvokeFunc = func; }

        /// <summary>
        /// 添加订阅（基础方法）
        /// </summary>
        /// <param name="action">回调函数</param>
        /// <param name="target">订阅者对象，用于后续批量移除（传 this）</param>
        /// <param name="first">首次订阅时是否立即执行（配合 AutoInvoke）</param>
        /// <param name="once">是否为一次性订阅，执行后自动移除</param>
        protected void Add(
            object action,
            [CanBeNull] object target,
            bool first = true,
            bool once = false
        )
        {
            // 添加到订阅列表
            _actions.Add(new ActionItem()
            {
                Action = action,
                Target = target,
                Once = once
            });
            
            // 如果设置了首次立即执行，且配置了 AutoInvoke
            if (first && _autoInvokeFunc != null)
            {
                // 立即调用一次，用于同步初始状态
                OnInvokeItem(action, _autoInvokeFunc());
            }
        }

        /// <summary>
        /// 添加一次性订阅（无参版本）
        /// </summary>
        /// <param name="action">回调函数</param>
        /// <param name="target">订阅者对象</param>
        public void AddOnce(object action, [CanBeNull] object target)
        {
            Add((object)action, target, false, true);
        }

        /// <summary>
        /// 添加订阅（无参 Action）
        /// </summary>
        /// <param name="action">回调函数</param>
        /// <param name="target">订阅者对象，用于后续批量移除（传 this）</param>
        /// <param name="first">首次订阅时是否立即执行</param>
        public void Add(Action action, [CanBeNull] object target = null, bool first = true)
        {
            Add((object)action, target, first);
        }

        /// <summary>
        /// 添加一次性订阅（无参 Action）
        /// </summary>
        /// <param name="action">回调函数</param>
        /// <param name="target">订阅者对象</param>
        public void AddOnce(Action action, [CanBeNull] object target)
        {
            AddOnce((object)action, target);
        }



        /// <summary>
        /// 移除指定的订阅
        /// </summary>
        /// <param name="action">要移除的回调函数</param>
        /// <param name="target">目标对象，null 表示只匹配 action，非 null 则同时匹配 action 和 target</param>
        public void Remove(object action, [CanBeNull] object target)
        {
            _actions.RemoveAll(x => x.Action == action && (target == null || x.Target == target));
        }
        /// <summary>
        /// 移除指定对象的所有订阅
        /// </summary>
        /// <param name="target">目标对象（通常是订阅时传入的 this）</param>
        public void RemoveTarget(object target) 
        { 
            _actions.RemoveAll(x => x.Target == target); 
        }

        /// <summary>
        /// 清空所有订阅
        /// </summary>
        public void Clear() 
        { 
            _actions.Clear(); 
        }

        /// <summary>
        /// 触发事件，调用所有订阅的回调函数
        /// </summary>
        /// <param name="args">传递给回调函数的参数</param>
        /// <returns>所有回调函数的返回值数组</returns>
        /// <exception cref="AggregateException">当有回调函数抛出异常时，收集所有异常并抛出聚合异常</exception>
        public object[] Invoke(params object[] args)
        {
            var removeList = new List<ActionItem>();
            var results = new List<object>();
            var exceptions = new List<Exception>();
            
            // 遍历所有订阅项
            foreach (var actionItem in _actions)
            {
                if (actionItem.Action != null)
                {
                    try
                    {
                        // 调用回调函数
                        results.Add(OnInvokeItem(actionItem.Action, args));
                        
                        // 如果是一次性订阅，加入移除列表
                        if (actionItem.Once)
                        {
                            removeList.Add(actionItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 收集异常，继续执行其他回调（不中断）
                        exceptions.Add(ex);
                    }
                }
            }
            
            // 移除一次性订阅
            removeList.ForEach(item =>
            {
                Remove(item.Action, item.Target);
            });

            // 如果有异常，抛出聚合异常
            if (exceptions.Count > 0)
            {
                throw new AggregateException("事件回调中发生异常", exceptions);
            }

            return results.ToArray();
        }

        /// <summary>
        /// 调用单个回调函数
        /// </summary>
        /// <param name="action">回调函数（委托）</param>
        /// <param name="args">传递的参数数组</param>
        /// <returns>回调函数的返回值</returns>
        protected object OnInvokeItem(object action, params object[] args)
        {
            var callFunc = (Delegate)action;
            
            // 根据回调函数的参数数量，截取对应数量的参数
            var subParams = args[0..callFunc.Method.GetParameters().Length];
            
            // 动态调用委托
            var result = callFunc.DynamicInvoke(subParams);
            return result;
        }

    }

    /// <summary>
    /// 单参数事件订阅系统
    /// </summary>
    /// <typeparam name="T1">回调函数的第一个参数类型</typeparam>
    public class Actions<T1> : Actions
    {
        /// <summary>
        /// 添加订阅（单参数 Action）
        /// </summary>
        /// <param name="action">回调函数</param>
        /// <param name="target">订阅者对象，用于后续批量移除（传 this）</param>
        /// <param name="first">首次订阅时是否立即执行</param>
        public void Add(Action<T1> action, [CanBeNull] object target = null, bool first = true)
        {
            Add((object)action, target, first);
        }
        
        /// <summary>
        /// 添加一次性订阅（单参数 Action）
        /// </summary>
        /// <param name="action">回调函数</param>
        /// <param name="target">订阅者对象</param>
        public void AddOnce(Action<T1> action, [CanBeNull] object target)
        {
            AddOnce((object)action, target);
        }
    }

    /// <summary>
    /// 双参数事件订阅系统
    /// </summary>
    /// <typeparam name="T1">回调函数的第一个参数类型</typeparam>
    /// <typeparam name="T2">回调函数的第二个参数类型</typeparam>
    public class Actions<T1, T2> : Actions<T1>
    {
        /// <summary>
        /// 添加订阅（双参数 Action）
        /// </summary>
        /// <param name="action">回调函数</param>
        /// <param name="target">订阅者对象，用于后续批量移除（传 this）</param>
        /// <param name="first">首次订阅时是否立即执行</param>
        public void Add(Action<T1, T2> action, [CanBeNull] object target, bool first = true)
        {
            Add((object)action, target, first);
        }

        /// <summary>
        /// 添加一次性订阅（双参数 Action）
        /// </summary>
        /// <param name="action">回调函数</param>
        /// <param name="target">订阅者对象</param>
        public void AddOnce(Action<T1, T2> action, [CanBeNull] object target)
        {
            AddOnce((object)action, target);
        }
    }
    
    /// <summary>
    /// 三参数事件订阅系统
    /// </summary>
    /// <typeparam name="T1">回调函数的第一个参数类型</typeparam>
    /// <typeparam name="T2">回调函数的第二个参数类型</typeparam>
    /// <typeparam name="T3">回调函数的第三个参数类型</typeparam>
    public class Actions<T1, T2, T3> : Actions<T1, T2>
    {
        /// <summary>
        /// 添加订阅（三参数 Action）
        /// </summary>
        /// <param name="action">回调函数</param>
        /// <param name="target">订阅者对象，用于后续批量移除（传 this）</param>
        /// <param name="first">首次订阅时是否立即执行</param>
        public void Add(Action<T1, T2, T3> action, [CanBeNull] object target, bool first = true)
        {
            Add((object)action, target, first);
        }

        /// <summary>
        /// 添加一次性订阅（三参数 Action）
        /// </summary>
        /// <param name="action">回调函数</param>
        /// <param name="target">订阅者对象</param>
        public void AddOnce(Action<T1, T2, T3> action, [CanBeNull] object target)
        {
            AddOnce((object)action, target);
        }
    }

}