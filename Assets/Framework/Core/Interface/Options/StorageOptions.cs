using System;

namespace Framework.Core
{
    [Serializable]
    public  struct StorageOptions
    {
        public string prefix;
        public StorageType storageType;
    }

}