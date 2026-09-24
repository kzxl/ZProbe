using System;
using System.Text.Json;

namespace ZProbe.UI.Modules.ApiClient.Services
{
    public class ChainingRuleItem
    {
        public string TargetVariableName { get; set; } = string.Empty;
        public string JsonPath { get; set; } = string.Empty; // e.g. "$.token" or "data.user.id"
        public bool IsEnabled { get; set; } = true;
    }

    public static class ResponseExtractorService
    {
        /// <summary>
        /// Extracts a value from a JSON string using a dotted path (e.g. "token", "data.id", "$.user.name").
        /// </summary>
        public static string? ExtractValue(string jsonContent, string path)
        {
            if (string.IsNullOrWhiteSpace(jsonContent) || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            // Strip leading "$." or "."
            var cleanPath = path.Trim();
            if (cleanPath.StartsWith("$.")) cleanPath = cleanPath.Substring(2);
            else if (cleanPath.StartsWith("$")) cleanPath = cleanPath.Substring(1);
            cleanPath = cleanPath.TrimStart('.');

            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                var current = doc.RootElement;

                var segments = cleanPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var seg in segments)
                {
                    // Check for array index notation: e.g. "items[0]"
                    int bracketIdx = seg.IndexOf('[');
                    if (bracketIdx > 0 && seg.EndsWith("]"))
                    {
                        var propName = seg.Substring(0, bracketIdx);
                        var indexStr = seg.Substring(bracketIdx + 1, seg.Length - bracketIdx - 2);

                        if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(propName, out current))
                        {
                            return null;
                        }

                        if (int.TryParse(indexStr, out int arrIdx) && current.ValueKind == JsonValueKind.Array)
                        {
                            int count = 0;
                            bool found = false;
                            foreach (var elem in current.EnumerateArray())
                            {
                                if (count == arrIdx)
                                {
                                    current = elem;
                                    found = true;
                                    break;
                                }
                                count++;
                            }
                            if (!found) return null;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(seg, out current))
                        {
                            return null;
                        }
                    }
                }

                return current.ValueKind switch
                {
                    JsonValueKind.String => current.GetString(),
                    JsonValueKind.Number => current.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    _ => current.GetRawText()
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Processes multiple chaining rules on a response body and updates the EnvironmentManager.
        /// </summary>
        public static int ProcessChainingRules(string jsonContent, System.Collections.Generic.IEnumerable<ChainingRuleItem> rules, EnvironmentManager envManager)
        {
            if (string.IsNullOrWhiteSpace(jsonContent) || rules == null || envManager == null)
            {
                return 0;
            }

            int extractedCount = 0;
            foreach (var rule in rules)
            {
                if (!rule.IsEnabled || string.IsNullOrWhiteSpace(rule.TargetVariableName) || string.IsNullOrWhiteSpace(rule.JsonPath))
                {
                    continue;
                }

                var extracted = ExtractValue(jsonContent, rule.JsonPath);
                if (extracted != null)
                {
                    envManager.SetVariable(rule.TargetVariableName, extracted);
                    extractedCount++;
                }
            }

            return extractedCount;
        }
    }
}
