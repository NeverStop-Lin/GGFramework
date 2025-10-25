using System.Threading.Tasks;

namespace Framework.Core
{
    public interface IBaseUI
    {
        public void Initialize();
        public Task<object> DoCreate(params object[] args);
        public Task<object> DoShow(params object[] args);
        public Task<object> DoReady(params object[] args);
        public Task<object> DoHide(params object[] args);
        public Task<object> DoDestroy(params object[] args);
    }
}