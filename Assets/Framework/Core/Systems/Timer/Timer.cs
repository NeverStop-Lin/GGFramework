using System;

using Zenject;

namespace Framework.Core
{
    public class Timer
    {

        [Inject]
        TimerCenter _center;

        readonly TimerOptions _options = new TimerOptions();

        public object Target
        {
            get { return _options.Target; }
        }
        public string Key
        {
            get { return _options.Key; }
        }


        float _delay;
        float _interval;
        int _execCount;
        float _duration;

        public TimerStatus Status = TimerStatus.None;

        public Timer(object target = null, string key = "default_timer")
        {
            _options.Target = target;
            _options.Key = key;
        }

        public Timer Delay(float value)
        {
            _options.Delay = value;
            return this;
        }
        public Timer Duration(float value)
        {
            _options.Duration = value;
            return this;
        }
        public Timer Interval(float value)
        {
            _options.Interval = value;
            return this;
        }
        public Timer Count(int value)
        {
            _options.Count = value;
            return this;
        }
        public Timer Action(Action<int> action)
        {
            _options.Action ??= new Actions<int>();
            _options.Action.Add(action);
            return this;
        }
        public Timer Action(Action action)
        {
            _options.Action ??= new Actions<int>();
            _options.Action.Add(action);
            return this;
        }

        public Timer Reset()
        {
            _delay = 0;
            _interval = 0;
            _execCount = 0;
            _duration = 0;
            Status = TimerStatus.None;
            return this;
        }

        public void Play()
        {
            Status = TimerStatus.Playing;
            _center.TryAdd(this);
        }
        public void Pause() { Status = TimerStatus.Paused; }
        public void Resume() { Status = TimerStatus.Playing; }
        public void Stop() { Status = TimerStatus.Stopped; }


        public void OnUpdate(float value)
        {
            if (Status != TimerStatus.Playing) return;

            if (_delay < _options.Delay)
            {
                _delay += value;
                return;
            }

            _duration += value;

            if (_delay >= _options.Delay && _execCount == 0)
            {
                _options.Action?.Invoke(_execCount);
                _execCount++;
            }
            else if ((_execCount == 0 && _options.Delay == 0) || (_interval += value) > _options.Interval)
            {
                _interval = 0;
                _options.Action?.Invoke(_execCount);
                _execCount++;
            }

            if ((_options.Count >= 0 && _execCount >= _options.Count) ||
                (_options.Duration > 0 && _duration >= _options.Duration))
            {
                Stop();
            }
        }
    }


}