using System.Collections.Generic;
using WebValidation.Model;
using WebValidation.Parameters;
using Xunit;

namespace UnitTests
{
    public class TestArrayValidator
    {
        [Fact]
        public void JsonArrayTest()
        {
            ValidationResult res;
            JsonArray a;

            // validate empty array
            a = new JsonArray();
            res = Validator.Validate(a);
            Assert.False(res.Failed);

            // validate bad count
            a = new JsonArray
            {
                Count = -1
            };
            res = Validator.Validate(a);
            Assert.True(res.Failed);

            // validate bad count
            a = new JsonArray
            {
                Count = 1,
                MinCount = 1
            };
            res = Validator.Validate(a);
            Assert.True(res.Failed);

            // validate bad count
            a = new JsonArray
            {
                MaxCount = 1,
                MinCount = 1
            };
            res = Validator.Validate(a);
            Assert.True(res.Failed);
        }

        [Fact]
        public void ByIndexTest()
        {
            List<JsonPropertyByIndex> list = new List<JsonPropertyByIndex>();
            JsonPropertyByIndex f;

            // empty list is valid
            Assert.True(Validator.Validate(list).Validated);

            // validate index < 0 fails
            f = new JsonPropertyByIndex
            {
                Index = -1,
                Value = null,
                Validation = null
            };
            list.Add(f);
            Assert.True(Validator.Validate(list).Failed);
        }
    }
}
