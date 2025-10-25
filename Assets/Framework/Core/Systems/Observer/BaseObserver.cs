using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Framework.Core
{

    public abstract class BaseObserver<T> : Observer
    {
        [Inject]
        DiContainer _container;

        public Actions<T, T> OnChange { get; } = new Actions<T, T>();



        public BaseObserver() { }


        protected virtual void Notify(params object[] args) { OnChange.Invoke(args); }

    }
}