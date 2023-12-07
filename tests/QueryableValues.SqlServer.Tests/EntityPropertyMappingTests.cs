using System;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests
{
    public class EntityPropertyMappingTests
    {
        class DummyClass
        {
            public int MyProperty { get; set; }
        }

        [Fact]
        public void GetMappings_Should_Throw()
        {
            Assert.Throws<InvalidOperationException>(() => EntityPropertyMapping.GetMappings(typeof(DummyClass), typeof(DummyClass)));
        }

        [Fact]
        public void GetMappings_Should_NotThrow()
        {
            EntityPropertyMapping.GetMappings(typeof(DummyClass), typeof(SimpleQueryableValuesEntity<int>));
            EntityPropertyMapping.GetMappings(typeof(DummyClass), typeof(ComplexQueryableValuesEntity));
        }
    }
}
