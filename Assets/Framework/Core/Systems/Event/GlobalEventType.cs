namespace Framework.Core
{
    public static partial class GlobalEventType
    {
        public static readonly string UI = "UI";
        public enum UIEvent
        {
            Create,
            Show,
            Ready,
            Hide,
            Destroy,

        }

    }
}