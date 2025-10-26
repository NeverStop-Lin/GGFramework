using System.Threading.Tasks;

namespace Framework.Core
{
    public struct UiLifeCycle<T>
    {
        public Task ShowTask { get; set; }
        public Task HideTask { get; set; }
        public ActionUiAttachment Action { get; set; }
        public T Target { get; set; }
    }
}