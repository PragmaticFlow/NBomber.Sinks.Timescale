using System.Text.Json.Serialization;

namespace NBomber.Sinks.Timescale.Contracts;

internal class SessionTagsDbRecord
{
    [JsonPropertyName("global")] public IReadOnlyDictionary<string, string> Global { get; set; } = new Dictionary<string, string>();
    [JsonPropertyName("scenarios")] public ScenarioTagsDbRecord[] Scenarios { get; set; } = [];
}

internal class ScenarioTagsDbRecord
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("tags")] public Dictionary<string, string> Tags { get; set; } = [];
}
