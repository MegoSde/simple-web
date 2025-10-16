using System.Text.Json.Nodes;

namespace cms.Common;

static class JsonHelpers
{
    public static JsonObject DeepClone(JsonObject obj) =>
        JsonNode.Parse(obj.ToJsonString())!.AsObject();

    public static JsonObject EnsureObject(JsonObject? maybe) =>
        maybe is JsonObject o ? o : new JsonObject();

    public static void MergeMissing(JsonObject target, JsonObject defaults)
    {
        foreach (var kv in defaults)
        {
            if (!target.ContainsKey(kv.Key) || target[kv.Key] is null)
            {
                target[kv.Key] = kv.Value?.DeepClone();
            }
            else if (target[kv.Key] is JsonObject tObj && kv.Value is JsonObject dObj)
            {
                // rekursiv merge for nested objects
                MergeMissing(tObj, dObj);
            }
        }
    }
}
