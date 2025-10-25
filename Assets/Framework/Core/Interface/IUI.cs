



using System;
using System.Threading.Tasks;

namespace Framework.Core
{
    public interface IUI
    {
        public UiLifeCycle<T> Show<T>(params object[] args) where T : IBaseUI, new();
        public Task<object> Hide<T>(params object[] args);
        public Task<object> Hide<T>(T target, params object[] args) where T : Type;

    }
}