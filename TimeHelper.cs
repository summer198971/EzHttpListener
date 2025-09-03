using System;

namespace EzHttpListener
{
    /// <summary>
    /// 全局时间帮助类，统一管理UTC+8时区时间
    /// </summary>
    public static class TimeHelper
    {
        /// <summary>
        /// 获取UTC+8时区的当前时间
        /// </summary>
        /// <returns>UTC+8时区的DateTime</returns>
        public static DateTime GetUtc8Time()
        {
            return DateTime.UtcNow.AddHours(8);
        }

        /// <summary>
        /// 获取UTC时区的当前时间
        /// </summary>
        /// <returns>UTC时区的DateTime</returns>
        public static DateTime GetUtcTime()
        {
            return DateTime.UtcNow;
        }

        /// <summary>
        /// 获取UTC+8时区的Unix时间戳（秒）
        /// </summary>
        /// <returns>Unix时间戳（秒）</returns>
        public static long GetUtc8UnixTimeSeconds()
        {
            var utc8Time = GetUtc8Time();
            return new DateTimeOffset(utc8Time).ToUnixTimeSeconds();
        }

        /// <summary>
        /// 获取UTC+8时区的Unix时间戳（毫秒）
        /// </summary>
        /// <returns>Unix时间戳（毫秒）</returns>
        public static long GetUtc8UnixTimeMilliseconds()
        {
            var utc8Time = GetUtc8Time();
            return new DateTimeOffset(utc8Time).ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 将Unix时间戳转换为UTC+8时区的DateTime
        /// </summary>
        /// <param name="unixTimeStamp">Unix时间戳（秒）</param>
        /// <returns>UTC+8时区的DateTime</returns>
        public static DateTime FromUnixTimeStampToUtc8(long unixTimeStamp)
        {
            var utcTime = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).DateTime;
            return utcTime.AddHours(8);
        }

        /// <summary>
        /// 将Unix时间戳转换为UTC时区的DateTime
        /// </summary>
        /// <param name="unixTimeStamp">Unix时间戳（秒）</param>
        /// <returns>UTC时区的DateTime</returns>
        public static DateTime FromUnixTimeStampToUtc(long unixTimeStamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).DateTime;
        }
    }
} 