using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BPSR_ZDPS.Database
{
    public class DictionaryObjectConverter : JsonConverter
    {
        public override bool CanConvert(System.Type objectType)
        {
            return objectType.Equals(typeof(Dictionary<string, object>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var result = new Dictionary<string, object>();

            foreach (var prop in jo.Properties())
            {
                if (prop.Value.Type == JTokenType.Integer)
                {
                    if ((long)prop.Value > Int32.MinValue && (long)prop.Value < Int32.MaxValue)
                    {
                        result[prop.Name] = (int)prop.Value;
                    }
                    else
                    {
                        result[prop.Name] = (long)prop.Value;
                    }
                }
                else
                {
                    var value = prop.Value.Type switch
                    {
                        JTokenType.Float => (double)prop.Value,
                        JTokenType.Boolean => (bool)prop.Value,
                        JTokenType.String => (string)prop.Value,
                        JTokenType.Null => null,
                        _ => prop.Value.ToObject<object>()
                    };

                    result[prop.Name] = value;
                }
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((int)value);
        }
    }
}
