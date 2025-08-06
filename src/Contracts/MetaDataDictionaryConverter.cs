using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts
{
    public class MetaDataDictionaryConverter : System.Text.Json.Serialization.JsonConverter<List<Dictionary<string, object>>>
    {
        public override List<Dictionary<string, object>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dictionaries = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(ref reader, options);
            foreach (var dictionary in dictionaries)
            {
                foreach (string key in dictionary.Keys.ToList())
                {
                    if (dictionary[key] is JsonElement je)
                    {
                        dictionary[key] = Unwrap(je);
                    }
                }
            }

            return dictionaries;
        }

        public override void Write(Utf8JsonWriter writer, List<Dictionary<string, object>> value, JsonSerializerOptions options)
            => System.Text.Json.JsonSerializer.Serialize(writer, value, options);

        private static object Unwrap(JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => je.ToString(),
                JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => je.EnumerateArray().Select(Unwrap).ToList(),
                JsonValueKind.Object => je.EnumerateObject().ToDictionary(x => x.Name, x => Unwrap(x.Value)),
                _ => null
            };
        }
    }
}
