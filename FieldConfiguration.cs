using System.Collections.Generic;
using Newtonsoft.Json;

namespace NameBuilderConfigurator
{
    /// <summary>
    /// Field configuration matching the NameBuilder schema
    /// </summary>
    public class FieldConfiguration
    {
        [JsonProperty("field")]
        public string Field { get; set; }
        
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
        
        [JsonProperty("format", NullValueHandling = NullValueHandling.Ignore)]
        public string Format { get; set; }
        
        [JsonProperty("maxLength", NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxLength { get; set; }
        
        [JsonProperty("truncationIndicator", NullValueHandling = NullValueHandling.Ignore)]
        public string TruncationIndicator { get; set; }
        
        [JsonProperty("default", NullValueHandling = NullValueHandling.Ignore)]
        public string Default { get; set; }
        
        [JsonProperty("alternateField", NullValueHandling = NullValueHandling.Ignore)]
        public FieldConfiguration AlternateField { get; set; }
        
        [JsonProperty("prefix", NullValueHandling = NullValueHandling.Ignore)]
        public string Prefix { get; set; }
        
        [JsonProperty("suffix", NullValueHandling = NullValueHandling.Ignore)]
        public string Suffix { get; set; }
        
        [JsonProperty("includeIf", NullValueHandling = NullValueHandling.Ignore)]
        public FieldCondition IncludeIf { get; set; }
        
        [JsonProperty("timezoneOffsetHours", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimezoneOffsetHours { get; set; }
    }
    
    /// <summary>
    /// Conditional field inclusion configuration
    /// </summary>
    public class FieldCondition
    {
        [JsonProperty("field", NullValueHandling = NullValueHandling.Ignore)]
        public string Field { get; set; }
        
        [JsonProperty("operator", NullValueHandling = NullValueHandling.Ignore)]
        public string Operator { get; set; }
        
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }
        
        [JsonProperty("anyOf", NullValueHandling = NullValueHandling.Ignore)]
        public List<FieldCondition> AnyOf { get; set; }
        
        [JsonProperty("allOf", NullValueHandling = NullValueHandling.Ignore)]
        public List<FieldCondition> AllOf { get; set; }
    }
    
    /// <summary>
    /// Root configuration object
    /// </summary>
    public class PluginConfiguration
    {
        [JsonProperty("entity", NullValueHandling = NullValueHandling.Ignore, Order = 1)]
        public string Entity { get; set; }
        
        [JsonProperty("targetField", NullValueHandling = NullValueHandling.Ignore, Order = 2)]
        public string TargetField { get; set; } = "name";
        
        [JsonProperty("fields", Order = 3)]
        public List<FieldConfiguration> Fields { get; set; } = new List<FieldConfiguration>();
        
        [JsonProperty("maxLength", NullValueHandling = NullValueHandling.Ignore, Order = 4)]
        public int? MaxLength { get; set; }
        
        [JsonProperty("enableTracing", NullValueHandling = NullValueHandling.Ignore, Order = 5)]
        public bool? EnableTracing { get; set; }
    }
}
