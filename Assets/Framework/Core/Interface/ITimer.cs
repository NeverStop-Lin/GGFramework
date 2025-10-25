
namespace Framework.Core
{
    public interface ITimer
    {

        float RunningTime { get; set; }
        float TotalRunningTime { get; set; }

        void Interval(TimerOptions options);
        void FixedInterval();

        void OnUpdate(float value);
        void OnFixedUpdate();
    }
}