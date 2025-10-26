using System;
using Zenject;

namespace Framework.Core
{
    /// <summary>
    /// 已废弃：请使用 UIFactoryBehaviour 代替
    /// 此类保留用于兼容性，将在未来版本中移除
    /// </summary>
    [Obsolete("请使用 UIFactoryBehaviour 代替。UIFactory用于普通类UI，UIFactoryBehaviour用于MonoBehaviour UI。", false)]
    public class UIFactory : IFactory<Type, IBaseUI>
    {
        private readonly DiContainer _container;

        public UIFactory(DiContainer container)
        {
            _container = container;
        }

        public IBaseUI Create(Type uiType)
        {
            return (IBaseUI)_container.Instantiate(uiType);
        }
    }
}