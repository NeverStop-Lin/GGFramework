
namespace Framework.Core
{
    public class TimerOptions
    {
        public int Count;
        public float Duration;

        public float Delay;
        public float Interval;

        public object Target = null;
        public string Key = "default_timer";

        public Actions<int> Action;

    }

}