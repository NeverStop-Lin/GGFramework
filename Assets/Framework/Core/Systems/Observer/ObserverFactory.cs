using System;
using Zenject;

namespace Framework.Core
{
    public class ObserverFactory : IFactory<Type, Observer>
    {
        private readonly DiContainer _container;

        public ObserverFactory(DiContainer container) { _container = container; }

        public Observer Create(Type type) { return (Observer)_container.Instantiate(type); }
    }

}