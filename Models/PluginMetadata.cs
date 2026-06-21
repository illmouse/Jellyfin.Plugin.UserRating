using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.UserRatings.Models;

public class PluginMetadata
{
    [JsonPropertyName("currentVersion")]
    public string CurrentVersion { get; set; } = string.Empty;

    [JsonPropertyName("versionHistory")]
    public List<string> VersionHistory { get; set; } = new();

    [JsonPropertyName("migrations")]
    public List<MigrationRecord> Migrations { get; set; } = new();
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
