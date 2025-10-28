using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Framework.Core
{
    public class Actions
    {
        public struct ActionItem
        {
            public object Action;
            public object Target;
            public bool Once;
        }

        readonly List<ActionItem> _actions = new List<ActionItem>();

        Func<object[]> _autoInvokeFunc;

        public void AutoInvoke(Func<object[]> func) { _autoInvokeFunc = func; }

        protected void Add(
            object action,
            [CanBeNull] object target,
            bool first = true,
            bool once = false
        )
        {

            _actions.Add(new ActionItem()
            {
                Action = action,
                Target = target,
                Once = once
            });
            if (first && _autoInvokeFunc != null)
            {
                OnInvokeItem(action, _autoInvokeFunc());
            }
        }

        public void AddOnce(object action, [CanBeNull] object target)
        {
            Add((object)action, target, false, true);
        }


        public void Add(Action action, [CanBeNull] object target = null, bool first = true)
        {
            Add((object)action, target, first);
        }

        public void AddOnce(Action action, [CanBeNull] object target)
        {
            AddOnce((object)action, target);
        }



        public void Remove(object action, [CanBeNull] object target)
        {
            _actions.RemoveAll(x => x.Action == action && (target != null && x.Target == target));
        }
        public void RemoveTarget(object target) { _actions.RemoveAll(x => x.Target == target); }

        public void Clear() { _actions.Clear(); }

        public object[] Invoke(params object[] args)
        {

            var removeList = new List<ActionItem>();
            var results = new List<object>();
            var exceptions = new List<Exception>();
            
            foreach (var actionItem in _actions)
            {
                if (actionItem.Action != null)
                {
                    try
                    {
                        results.Add(OnInvokeItem(actionItem.Action, args));
                        if (actionItem.Once)
                        {
                            removeList.Add(actionItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 收集异常，继续执行其他回调
                        exceptions.Add(ex);
                    }
                }
            }
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



        protected object OnInvokeItem(object action, params object[] args)
        {
            var callFunc = (Delegate)action;
            var subParams = args[0..callFunc.Method.GetParameters().Length];
            var result = callFunc.DynamicInvoke(subParams);
            return result;
        }

    }

    public class Actions<T1> : Actions
    {

        public void Add(Action<T1> action, [CanBeNull] object target = null, bool first = true)
        {
            Add((object)action, target, first);
        }
        public void AddOnce(Action<T1> action, [CanBeNull] object target)
        {
            AddOnce((object)action, target);
        }

    }

    public class Actions<T1, T2> : Actions<T1>
    {

        public void Add(Action<T1, T2> action, [CanBeNull] object target, bool first = true)
        {
            Add((object)action, target, first);
        }

        public void AddOnce(Action<T1, T2> action, [CanBeNull] object target)
        {
            AddOnce((object)action, target);
        }
    }
    public class Actions<T1, T2, T3> : Actions<T1, T2>
    {

        public void Add(Action<T1, T2, T3> action, [CanBeNull] object target, bool first = true)
        {
            Add((object)action, target, first);
        }

        public void AddOnce(Action<T1, T2, T3> action, [CanBeNull] object target)
        {
            AddOnce((object)action, target);
        }

    }

}