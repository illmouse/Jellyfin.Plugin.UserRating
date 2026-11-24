using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Jellyfin.Plugin.UserRatings.Services;

/// <summary>
/// Resolves localized strings from the embedded Strings/{culture}.json files.
/// Lookup order: exact culture, base language, English. Missing keys always fall back to English.
/// File names may use either '-' or '_' as the region separator (pt-BR.json / pt_BR.json).
/// </summary>
public static class Localizer
{
    private const string ResourcePrefix = "Jellyfin.Plugin.UserRatings.Strings.";
    private const string DefaultCulture = "en";

    private static readonly Assembly _assembly = typeof(Localizer).Assembly;
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the complete dictionary for <paramref name="lang"/> (English overlaid with the best match)
    /// and the culture that was actually used. Returns null only if the English resource is missing.
    /// </summary>
    public static (Dictionary<string, string> Strings, string Culture)? Resolve(string? lang)
    {
        var english = Load(DefaultCulture);
        if (english == null)
        {
            return null;
        }

        foreach (var candidate in Candidates(lang))
        {
            if (string.Equals(candidate, DefaultCulture, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var dict = Load(candidate);
            if (dict == null)
            {
                continue;
            }

            var merged = new Dictionary<string, string>(english);
            foreach (var kv in dict)
            {
                // An empty value means "not translated yet" (Weblate exports these) - keep English.
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    merged[kv.Key] = kv.Value;
                }
            }

            return (merged, candidate);
        }

        return (english, DefaultCulture);
    }

    /// <summary>Looks up a single string for the current request/thread UI culture.</summary>
    public static string Get(string key)
    {
        var resolved = Resolve(CultureInfo.CurrentUICulture.Name);
        return resolved != null && resolved.Value.Strings.TryGetValue(key, out var value) ? value : key;
    }

    /// <summary>"pt_BR"/"pt-br" -> ["pt-BR", "pt"]; invalid or empty input -> [].</summary>
    internal static IEnumerable<string> Candidates(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang) || lang.Length > 16)
        {
            yield break;
        }

        var parts = lang.Replace('_', '-').Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !IsAlpha(parts[0]))
        {
            yield break;
        }

        var baseLang = parts[0].ToLowerInvariant();
        if (parts.Length > 1 && IsAlpha(parts[1]))
        {
            yield return baseLang + "-" + parts[1].ToUpperInvariant();
        }

        yield return baseLang;
    }

    private static bool IsAlpha(string s)
    {
        foreach (var c in s)
        {
            if (!char.IsAsciiLetter(c))
            {
                return false;
            }
        }

        return s.Length > 0;
    }

    private static Dictionary<string, string>? Load(string culture)
    {
        // Only successful loads are cached so arbitrary ?lang= values can't grow the cache.
        if (_cache.TryGetValue(culture, out var cached))
        {
            return cached;
        }

        var loaded = LoadUncached(culture);
        if (loaded != null)
        {
            _cache[culture] = loaded;
        }

        return loaded;
    }

    private static Dictionary<string, string>? LoadUncached(string culture)
    {
        // Match against real resource names so user input never builds a path.
        foreach (var name in _assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                || !name.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            var code = name.Substring(ResourcePrefix.Length, name.Length - ResourcePrefix.Length - 5).Replace('_', '-');
            if (!string.Equals(code, culture, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var stream = _assembly.GetManifestResourceStream(name);
            return stream == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
        }

        return null;
    }
}
