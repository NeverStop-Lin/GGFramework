using System.Threading.Tasks;

namespace Framework.Core
{
    public struct UiLifeCycle<T>
    {
        public Task ShowTask { get; set; }
        public Task<object> HideTask { get; set; }
        public T Target { get; set; }
    }
}