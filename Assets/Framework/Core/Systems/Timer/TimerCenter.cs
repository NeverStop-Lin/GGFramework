using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ModestTree;
using Unity.VisualScripting;
using Zenject;

namespace Framework.Core
{
    public class TimerCenter
    {

        [Inject]
        DiContainer _container; // ע��Zenject����

        readonly List<Timer> _timerList = new List<Timer>();



        public float RunningTime { get; set; }
        public float TotalRunningTime { get; set; }


        public Timer TryAdd(Timer timer)
        {
            if (_timerList.FindIndex(t => t == timer) < 0)
            {
                _timerList.Add(timer);
            }
            return timer;
        }

        public Timer Interval(Action<int> action, float interval, int count = -1, float delay = 0, float duration = -1)
        {
            var timer = GetOrCreate().Action(action).Delay(delay).Interval(interval).Count(count).Duration(duration);
            timer.Play();
            return timer;
        }
        public Timer Interval(Action action, float interval, int count = -1, float delay = 0, float duration = -1)
        {
            return Interval((i) => { action(); }, interval, count, delay, duration);
        }

        public Timer Timeout(Action action, float value)
        {
            var timer = GetOrCreate().Action(action).Delay(value);
            timer.Play();
            return timer;
        }


        public Timer GetOrCreate(object target = null, string key = "default_timer")
        {
            if (target == null) { return _container.Instantiate<Timer>(); }
            var timer = _timerList.FirstOrDefault(timer => timer.Target == target && timer.Key == key) ?? TryAdd(
                _container.Instantiate<Timer>(new[]
                {
                    target, key
                }));


            return timer;
        }

        public void FixedInterval() { throw new System.NotImplementedException(); }

        public void OnUpdate(float value)
        {
            RunningTime += value;

            var removeList = new List<Timer>();
            var array = new Timer[_timerList.Count];
            _timerList.CopyTo(array);
            array.ToList().ForEach(timer =>
            {
                timer.OnUpdate(value);
                if (timer.Status == TimerStatus.Stopped)
                {
                    removeList.Add(timer);
                }
            });
            removeList.ForEach(timer => _timerList.Remove(timer));
        }
        public void OnFixedUpdate() { }
    }
}