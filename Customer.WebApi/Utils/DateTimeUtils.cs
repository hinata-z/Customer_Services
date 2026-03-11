namespace Customer.WebApi.Utils
{
    public class DateTimeUtils
    {

        public static long GetUnixSecondsTime()
        {

            DateTimeOffset dateTimeOffset = new DateTimeOffset(DateTime.Now);
            return dateTimeOffset.ToUnixTimeSeconds();
        }

        public static long ToUnixTimeMilliseconds(DateTime time)
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(time);
            return dateTimeOffset.ToUnixTimeMilliseconds();
        }

        public static long GetUnixTime(long second)
        {

            DateTimeOffset dateTimeOffset = new DateTimeOffset(DateTime.Now).AddSeconds(second);
            return dateTimeOffset.ToUnixTimeSeconds();
        }

        public static DateTime GetDateTimeByUninx(long unixTime)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime.ToLocalTime();
        }
        public static string GenerateTimeStamp()
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }


    }
}
