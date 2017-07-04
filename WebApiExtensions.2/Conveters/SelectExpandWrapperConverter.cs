using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web.OData.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WebApiExtensions.Conveters
{
    public class SelectExpandWrapperConverter : JsonConverter
    {
        static readonly ConcurrentDictionary<Type, Dictionary<string, JsonProperty>> cache = new ConcurrentDictionary<Type, Dictionary<string, JsonProperty>>();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var wrapper = (ISelectExpandWrapper)value;
            var dict = wrapper.ToDictionary();
            var type = value.GetType().GetGenericArguments()[0];
            var props = cache.GetOrAdd(type, t =>
            {
                var contract = (JsonObjectContract)serializer.ContractResolver.ResolveContract(t);
                return contract.Properties.ToDictionary(prop => prop.UnderlyingName, StringComparer.OrdinalIgnoreCase);
            });
            

            writer.WriteStartObject();
            foreach (var kvp in dict)
            {
                if (kvp.Value == null && serializer.NullValueHandling == NullValueHandling.Ignore)
                    continue;
                if (!props.TryGetValue(kvp.Key, out var prop))
                {
                    writer.WritePropertyName(kvp.Key);
                    serializer.Serialize(writer, kvp.Value);
                }
                else
                {
                    writer.WritePropertyName(prop.PropertyName);
                    if (prop.MemberConverter == null)
                        serializer.Serialize(writer, kvp.Value);
                    else
                        prop.MemberConverter.WriteJson(writer, kvp.Value, serializer);
                }
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ISelectExpandWrapper).IsAssignableFrom(objectType);
        }
    }

}
