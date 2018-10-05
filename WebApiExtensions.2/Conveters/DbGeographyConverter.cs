using System;
using System.Data.Entity.Spatial;
using Newtonsoft.Json;

namespace WebApiExtensions.Conveters
{
    public class DbGeographyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DbGeography);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            try
            {
                if (reader.TokenType == JsonToken.String)
                {
                    return DbGeography.FromText(reader.Value.ToString());
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    decimal? latitude = null, longitude = null;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.EndObject)
                        {
                            break;
                        }
                        if (reader.TokenType != JsonToken.PropertyName)
                        {
                            continue;
                        }
                        switch (reader.Value.ToString())
                        {
                            case "latitude":
                                latitude = reader.ReadAsDecimal();
                                break;
                            case "longitude":
                                longitude = reader.ReadAsDecimal();
                                break;
                        }
                    }
                    return DbGeography.FromText($"POINT({longitude ?? 0} {latitude ?? 0})");

                }
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Error converting value {reader.Value} to type '{objectType}'.", ex);
            }
            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing geography.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var geography = (DbGeography)value;
            writer.WriteStartObject();
            writer.WritePropertyName("latitude");
            writer.WriteValue(geography.Latitude);
            writer.WritePropertyName("longitude");
            writer.WriteValue(geography.Longitude);
            writer.WriteEndObject();
        }
    }
}
