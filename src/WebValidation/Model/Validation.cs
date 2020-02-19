using System;
using System.Collections.Generic;

namespace WebValidation
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "can't be read-only")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1724:Naming conflict", Justification = "safe to ignore")]
    public class Validation
    {
        public int Code { get; set; } = 200;
        public string ContentType { get; set; } = "application/json";
        public int? Length { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public int? MaxMilliseconds { get; set; }
        public List<ValueCheck> Contains { get; set; } = new List<ValueCheck>();
        public ValueCheck ExactMatch { get; set; }
        public JsonArray JsonArray { get; set; }
        public List<JsonProperty> JsonObject { get; set; }
    }

    public class ValueCheck
    {
        public string Value { get; set; }
        public bool IsCaseSensitive { get; set; } = true;
    }

    public class JsonProperty
    {
        public string Field { get; set; }
        public object Value { get; set; }
    }

    public class JsonArray
    {
        public int Count { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
    }
}
