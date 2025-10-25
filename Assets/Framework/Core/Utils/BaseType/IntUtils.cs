using System;
using System.Collections.Generic;

namespace Framework.Core
{
    public static class IntUtils
    {
        /// <summary>
        /// ���� Times ����������һ�� �µ� List ������ T Ϊ callFunc �ķ������͡�
        /// </summary>
        /// <typeparam name="T">callFunc �ķ������͡�</typeparam>
        /// <param name="value">Ҫִ�� callFunc �Ĵ�����</param>
        /// <param name="callFunc">Ҫִ�еĺ�����</param>
        /// <returns>����һ�� �µ� List��</returns>
        public static List<T> Times<T>(this int value, Func<int, T> callFunc)
        {
            var result = new List<T>();
            for (var i = 0; i < value; i++)
            {
                result.Add(callFunc(i));
            }
            return result;
        }

        /// <summary>
        /// ���� Times ����������һ�� �µ� List������Ԫ��Ϊ�� 0 �� value-1 ��ֵ��
        /// </summary>
        /// <param name="value">Ҫִ�� callFunc �Ĵ�����</param>
        /// <returns>����һ�� �µ� List��</returns>
        public static List<int> Times(this int value) { return Times(value, i => i); }

        /// <summary>
        /// ���� Times ������ִ�� callFunc ������� 0 �� value-1 ��ֵ��
        /// </summary>
        /// <param name="value">Ҫִ�� callFunc �Ĵ�����</param>
        /// <param name="callFunc">Ҫִ�еĺ�����</param>
        public static void Times(this int value, Action<int> callFunc)
        {
            for (var i = 0; i < value; i++)
            {
                callFunc(i);
            }
        }

        public static int Limit(this int value, int max, int min = 0)
        {
            return value < min
                ? min
                : value > max
                    ? max
                    : value;
        }


        /// <summary>
        /// ����0��value-1�����������Ƿ���������������JS�����every������
        /// </summary>
        /// <param name="value">Ҫ���Ĵ���</param>
        /// <param name="predicate">�����жϺ���</param>
        /// <returns>��������������������ʱ����true</returns>
        public static bool Every<T>(this List<T> value, Func<T, bool> predicate)
        {
            for (var i = 0; i < value.Count; i++)
            {
                if (!predicate(value[i])) return false;
            }
            return true;
        }

    }
}