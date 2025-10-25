using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Framework.Core
{
    public static class ListExtensions
    {
        /// <summary>
        /// ���������е�Ԫ�ز�ִ��ָ����ί�С�
        /// </summary>
        /// <typeparam name="T">����Ԫ�ص����͡�</typeparam>
        /// <param name="collection">Ҫ�����ļ��ϡ�</param>
        /// <param name="callFunc">Ҫִ�е�ί�С�</param>
        public static void Each<T>(this IEnumerable<T> collection, Action<T> callFunc)
        {
            ListUtils.Each(collection, callFunc);
        }

        /// <summary>
        /// ���������е�Ԫ�ز�ִ��ָ���Ĵ�������ί�С�
        /// </summary>
        /// <typeparam name="T">����Ԫ�ص����͡�</typeparam>
        /// <param name="collection">Ҫ�����ļ��ϡ�</param>
        /// <param name="callFunc">Ҫִ�еĴ�������ί�С�</param>
        public static void Each<T>(this IEnumerable<T> collection, Action<T, int> callFunc)
        {
            ListUtils.Each(collection, callFunc);
        }

        /// <summary>
        /// �첽�ص��������е�Ԫ�ز�ִ��ָ�����첽ί�С�
        /// </summary>
        /// <typeparam name="T">����Ԫ�ص����͡�</typeparam>
        /// <param name="collection">Ҫ�����ļ��ϡ�</param>
        /// <param name="asyncCallFunc">Ҫִ�е��첽ί�С�</param>
        public static async Task EachAsync<T>(this IEnumerable<T> collection, Func<T, Task> asyncCallFunc)
        {
            await ListUtils.EachAsync(collection, asyncCallFunc);
        }

        public static List<T> Clone<T>(this List<T> list)
        {
            var array = new T[list.Count];
            list.CopyTo(array);
            return array.ToList();
        }

        public static T Limit<T>(this List<T> list, int index) { return list[index.Limit(list.Count - 1, 0)]; }
    }
}