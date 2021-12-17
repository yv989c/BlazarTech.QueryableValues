namespace BlazarTech.QueryableValues
{
#nullable disable
    internal class QueryableValuesEntity<T>
    {
        public T V { get; set; }
    }

    internal class QueryableValuesEntity
    {
        public int? Int1 { get; set; }
        public int? Int2 { get; set; }
        public int? Int3 { get; set; }
        public int? Int4 { get; set; }
        public int? Int5 { get; set; }
        public int? Int6 { get; set; }
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public int? OtherId { get; set; }
        public int AnotherId { get; set; }
    }
#nullable restore
}