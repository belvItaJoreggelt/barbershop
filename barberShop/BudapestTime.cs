namespace barberShop
{
    public static class BudapestTime
    {
        public static TimeZoneInfo Tz { get; } =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest");

        public static DateTime BudapestLocalToUtc(DateTime unspecifiedBp)
        {
            if (unspecifiedBp.Kind != DateTimeKind.Unspecified)
                throw new ArgumentException("Használj DateTimeKind.Unspecified budapesti helyi időhöz");
            /*
            if (Tz.IsInvalidTime(unspecifiedBp))
                throw new ArgumentException("A megadott időpont nem létezik az óraátállítás miatt!");

            if (Tz.IsAmbiguousTime(unspecifiedBp))
                throw new ArgumentException("A megadott érték nem egyértelmű az óraátllítás miatt!");
            */
            return TimeZoneInfo.ConvertTimeToUtc(unspecifiedBp, Tz);
        }

        public static DateTime UtcToBudapest(DateTime utc)
        {
            if (utc.Kind == DateTimeKind.Unspecified)
                utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            else if (utc.Kind == DateTimeKind.Local)
                utc = utc.ToUniversalTime();

            return TimeZoneInfo.ConvertTimeFromUtc(utc, Tz);
        }

        public static DateTime TodayBudapestDate
        {
            get
            {
                var nowBudapest = UtcToBudapest(DateTime.UtcNow);
                return nowBudapest.Date;
            }
        }
    }
}
