namespace Framework.Core
{
    public static class MathUtils
    {
        public static float Random(float max, float min = 0f)
        {
            return UnityEngine.Random.Range(min, max);
        }
        public static int Random(int max, int min = 0)
        {
            return UnityEngine.Random.Range(min, max);
        }
    }
}