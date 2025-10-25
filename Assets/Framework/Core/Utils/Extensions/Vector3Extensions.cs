using UnityEngine;

namespace Framework.Core
{
    public static class Vector3Extensions
    {
        public static Vector3 WithZ(this Vector3 original, float z)
        {
            return new Vector3(original.x, original.y, z);
        }
    }
}