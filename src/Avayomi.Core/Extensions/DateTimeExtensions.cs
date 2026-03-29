namespace Avayomi.Core.Extensions;

public static class DateTimeExtensions
{
    private static readonly DateTime Jan1St1970 = DateTime.UnixEpoch;

    public static long CurrentTimeMillis()
    {
        return (long)(DateTime.UtcNow - Jan1St1970).TotalMilliseconds;
    }

    public static long CurrentTimeMillis(this DateTime date)
    {
        return (long)(date - Jan1St1970).TotalMilliseconds;
    }

    public static long ToUnixTimeMilliseconds(this DateTime dateTime)
    {
        return (long)(dateTime - Jan1St1970).TotalMilliseconds;
    }
}
