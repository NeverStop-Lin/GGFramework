

namespace Framework.Core
{
    public static class ObjectArrayUtils
    {
        public static T CastTo<T>(this object[] args, int index, T defaultValue = default)
        {
            // ��������
            if (args == null)
            {
                var warning = "��������Ϊ null������Ĭ��ֵ";
                FrameworkLogger.Warn(warning);
                return defaultValue;
            }

            // ����Խ����
            if (index < 0 || index >= args.Length)
            {
                var warning = $"���� {index} �������鷶Χ [0, {args.Length - 1}]������Ĭ��ֵ {defaultValue}";
                FrameworkLogger.Warn(warning);
     
                return defaultValue;
            }

            // ����ת�����
            object value = args[index];
            if (value is T matchedValue)
            {
                return matchedValue;
            }

            // ���Ͳ�ƥ�侯��
            var typeWarning
                = $"���� {index} ���Ͳ�ƥ�䣬���� {typeof(T).Name}��ʵ�� {value?.GetType().Name ?? "null"}������Ĭ��ֵ {defaultValue}";
            FrameworkLogger.Warn(typeWarning);
            return defaultValue;
        }
    }
}