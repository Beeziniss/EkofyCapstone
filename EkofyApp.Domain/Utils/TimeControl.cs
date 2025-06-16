namespace EkofyApp.Domain.Utils
{
    public class TimeControl
    {
        public static DateTime GetUtcPlus7Time()
        {
            #region Chỉ chạy được trên local nếu publish thì sẽ lỗi
            // Get the current UTC time
            //DateTime utcNow = DateTime.UtcNow;

            //// Define the UTC+7 time zone
            //TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            //// Convert the UTC time to UTC+7
            //DateTime utcPlus7Now = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
            #endregion

            // Get the current UTC time and add a 7-hour offset
            DateTime utcPlus7Now = DateTime.UtcNow.AddHours(7);

            return utcPlus7Now;
        }
    }
}
