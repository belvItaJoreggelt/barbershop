namespace barberShop
{
    public static class DBDataTimeHelper
    {
        public static DateTime ToUtcDate(DateTime d)
            => DateTime.SpecifyKind(d.Date,DateTimeKind.Utc);

        public static DateTime ToUtc(DateTime local)
            => DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
    }
}
