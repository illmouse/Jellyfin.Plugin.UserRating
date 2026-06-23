using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.UserRatings.Models;

public class PluginMetadata
{
    [JsonPropertyName("currentVersion")]
    public string CurrentVersion { get; set; } = string.Empty;

    [JsonPropertyName("currentVersionInstalledAt")]
    public DateTime? CurrentVersionInstalledAt { get; set; }

    [JsonPropertyName("versionHistory")]
    [JsonConverter(typeof(VersionHistoryConverter))]
    public List<VersionEntry> VersionHistory { get; set; } = new();

    [JsonPropertyName("migrations")]
    public List<MigrationRecord> Migrations { get; set; } = new();
}

public class VersionEntry
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("installedAt")]
    public DateTime? InstalledAt { get; set; }
}

/// <summary>
/// Reads versionHistory entries as either strings (legacy format) or objects (new format).
/// Legacy: ["1.12.0.1", "1.12.0.2"] → entries with null InstalledAt.
/// New:    [{"version":"1.12.0.1","installedAt":"2026-06-20T..."}]
/// Always writes in the new object format.
/// </summary>
public class VersionHistoryConverter : JsonConverter<List<VersionEntry>>
{
    public override List<VersionEntry>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return new();

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return new();
        }

        var list = new List<VersionEntry>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                list.Add(new VersionEntry { Version = reader.GetString() ?? string.Empty, InstalledAt = null });
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var entry = new VersionEntry();
                if (doc.RootElement.TryGetProperty("version", out var vProp) && vProp.ValueKind == JsonValueKind.String)
                    entry.Version = vProp.GetString() ?? string.Empty;
                if (doc.RootElement.TryGetProperty("installedAt", out var dProp) && dProp.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(dProp.GetString(), out var dt))
                        entry.InstalledAt = dt;
                }
                list.Add(entry);
            }
            else
            {
                reader.Skip();
            }
        }
        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<VersionEntry> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var entry in value)
        {
            writer.WriteStartObject();
            writer.WriteString("version", entry.Version);
            if (entry.InstalledAt.HasValue)
                writer.WriteString("installedAt", entry.InstalledAt.Value);
            else
                writer.WriteNull("installedAt");
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}

public class MigrationRecord
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("pluginVersion")]
    public string PluginVersion { get; set; } = string.Empty;

    [JsonPropertyName("resultMigrated")]
    public int ResultMigrated { get; set; }

    [JsonPropertyName("resultSkipped")]
    public int ResultSkipped { get; set; }
}