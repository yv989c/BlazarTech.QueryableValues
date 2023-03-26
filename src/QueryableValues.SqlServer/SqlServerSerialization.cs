#if EFCORE
namespace BlazarTech.QueryableValues
{
    /// <summary>
    /// Specifies the serialization format to be used when sending data to SQL Server.
    /// </summary>
    public enum SqlServerSerialization
    {
        /// <summary>
        /// Automatically chooses between JSON and XML serialization based on server and database compatibility.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This option may cause an additional round-trip to the database to check for JSON compatibility,
        /// but only once per unique connection string for the life of the process. If JSON serialization is not supported, XML is used instead.
        /// </para>
        /// <para>
        /// Caveat: If the very first query sent to the server is a QueryableValues enabled one, it will use XML and then switch to JSON (if supported) afterward.
        /// </para>
        /// </remarks>
        Auto = 0,

        /// <summary>
        /// Uses the JSON serializer for better performance.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Using JSON is faster than XML, but requires SQL Server 2016 or newer and a database compatibility level of 130 or higher.<br/>
        /// More info: <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/openjson-transact-sql"/>.
        /// </para>
        /// <para>
        /// <b>WARNING:</b> If JSON serialization is not supported, an error will occur at runtime.
        /// </para>
        /// </remarks>
        UseJson = 1,

        /// <summary>
        /// Uses the XML serializer, which is compatible with all supported versions of SQL Server to date.
        /// </summary>
        UseXml = 2
    }
}
#endif