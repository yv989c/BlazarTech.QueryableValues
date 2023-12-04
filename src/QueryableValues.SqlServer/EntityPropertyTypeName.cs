namespace BlazarTech.QueryableValues
{
    internal enum EntityPropertyTypeName
    {
        Unknown,
        Boolean,
        Byte,
        Int16,
        Int32,
        Int64,
        Decimal,
        Single,
        Double,
        DateTime,
        DateTimeOffset,
        Guid,
        Char,
        String,
#if EFCORE8
        DateOnly,
        TimeOnly
#endif
    }
}