namespace barberShop
{
    public static class DBDataTimeHelper
    {
        public static DateTime ToUtcDate(DateTime d)
        {
            return DateTime.SpecifyKind(d.Date, DateTimeKind.Utc);
        }

        public static DateTime ToUtc(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc)
                return dt;

            if (dt.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime();

            return dt.ToUniversalTime();
        }

        public static DateTime ToLocal(DateTime utc)
        {
            if (utc.Kind == DateTimeKind.Local)
                return utc;

            if (utc.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

            return utc.ToLocalTime();
        }
    }
}
