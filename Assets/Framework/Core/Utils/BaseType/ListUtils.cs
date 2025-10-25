using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Core
{
    public static class ListUtils
    {
        /// <summary>
        /// ���������е�Ԫ�ز�ִ��ָ����ί�С�
        /// </summary>
        /// <typeparam name="T">����Ԫ�ص����͡�</typeparam>
        /// <param name="collection">Ҫ�����ļ��ϡ�</param>
        /// <param name="callFunc">Ҫִ�е�ί�С�</param>
        public static void Each<T>(IEnumerable<T> collection, Action<T> callFunc)
        {
            if (collection == null || callFunc == null) return;
            foreach (var item in collection)
            {
                callFunc(item);
            }
        }

        /// <summary>
        /// ���������е�Ԫ�ز�ִ��ָ���Ĵ�������ί�С�
        /// </summary>
        /// <typeparam name="T">����Ԫ�ص����͡�</typeparam>
        /// <param name="collection">Ҫ�����ļ��ϡ�</param>
        /// <param name="callFunc">Ҫִ�еĴ�������ί�С�</param>
        public static void Each<T>(IEnumerable<T> collection, Action<T, int> callFunc)
        {
            var index = 0;
            Each(collection, (item) =>
            {
                callFunc(item, index++);
            });
        }

        /// <summary>
        /// �첽�ص��������е�Ԫ�ز�ִ��ָ�����첽ί�С�
        /// </summary>
        /// <typeparam name="T">����Ԫ�ص����͡�</typeparam>
        /// <param name="collection">Ҫ�����ļ��ϡ�</param>
        /// <param name="asyncCallFunc">Ҫִ�е��첽ί�С�</param>
        public static async Task EachAsync<T>(
            IEnumerable<T> collection,
            Func<T, Task> asyncCallFunc
        )
        {
            if (collection == null || asyncCallFunc == null) return;
            foreach (var item in collection)
            {
                await asyncCallFunc(item);
            }
        }
    }

}