using System;
using Zenject;

namespace Framework.Core
{
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