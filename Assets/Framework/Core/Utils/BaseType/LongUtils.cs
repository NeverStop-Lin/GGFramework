using System;

namespace Framework.Core
{
    public static class LongUtils
    {
        /// <summary>
        /// �ж�ʱ����Ƿ�Ϊ���죨���ڱ���ʱ�䣩
        /// </summary>
        /// <param name="timestamp">���뼶ʱ���</param>
        public static bool IsToday(this long timestamp)
        {
            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToLocalTime();
            return dateTime.Date == DateTime.Today;
        }
        
    }
    
}