namespace EkofyApp.Domain.Utils;

public static class CamelotHelper
{
    // Mapping từ (Key, Mode) => CamelotCode
    private static readonly Dictionary<(string Key, string Mode), string> _keyToCamelot = new()
    {
        { ("C", "major"), "8B" }, { ("C#", "major"), "3B" }, { ("D", "major"), "10B" },
        { ("D#", "major"), "5B" }, { ("E", "major"), "12B" }, { ("F", "major"), "7B" },
        { ("F#", "major"), "2B" }, { ("G", "major"), "9B" }, { ("G#", "major"), "4B" },
        { ("A", "major"), "11B" }, { ("A#", "major"), "6B" }, { ("B", "major"), "1B" },

        { ("C", "minor"), "5A" }, { ("C#", "minor"), "12A" }, { ("D", "minor"), "7A" },
        { ("D#", "minor"), "2A" }, { ("E", "minor"), "9A" }, { ("F", "minor"), "4A" },
        { ("F#", "minor"), "11A" }, { ("G", "minor"), "6A" }, { ("G#", "minor"), "1A" },
        { ("A", "minor"), "8A" }, { ("A#", "minor"), "3A" }, { ("B", "minor"), "10A" },
    };

    // Mapping ngược từ CamelotCode => List<(Key, Mode)>
    private static readonly Dictionary<string, List<(string Key, string Mode)>> _camelotToKeys =
        _keyToCamelot
            .GroupBy(kv => kv.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(kv => (kv.Key.Key, kv.Key.Mode)).ToList(),
                StringComparer.OrdinalIgnoreCase
            );

    public static string? GetCamelotCode(string key, string mode)
    {
        if (_keyToCamelot.TryGetValue((key, mode), out var code))
            return code;

        return null;
    }

    public static List<(string Key, string Mode)> GetCompatibleKeys(string key, string mode)
    {
        string? camelot = GetCamelotCode(key, mode);
        if (camelot == null)
        {
            return [];
        }

        int num = int.Parse(camelot[..^1]);
        char letter = camelot[^1]; // 'A' hoặc 'B'

        List<string> adjacent =
        [
            camelot,
            $"{(num == 1 ? 12 : num - 1)}{letter}",
            $"{(num == 12 ? 1 : num + 1)}{letter}",
            $"{num}{(letter == 'A' ? 'B' : 'A')}"
        ];

        // Chuyển từ mã Camelot → list (key, mode)
        List<(string Key, string Mode)> result = [];
        foreach (string c in adjacent)
        {
            if (_camelotToKeys.TryGetValue(c, out List<(string Key, string Mode)>? pairs))
            {
                result.AddRange(pairs);
            }
        }

        return result.Distinct().ToList();
    }
}

