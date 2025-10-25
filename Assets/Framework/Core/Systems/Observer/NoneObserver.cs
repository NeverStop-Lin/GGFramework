using System;
using System.Collections.Generic;
using Framework.Core;

namespace Framework.Core
{

    public class NoneObserver<T> : BaseObserver<T>
    {

        public void Notify(T value)
        {
            base.Notify(value);
        }
    }
}