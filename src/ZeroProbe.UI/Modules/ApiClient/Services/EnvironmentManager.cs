using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ZeroProbe.UI.Modules.ApiClient.Services
{
    public class EnvironmentVariableItem
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }

    public class ApiEnvironment
    {
        public string Name { get; set; } = "Default";
        public List<EnvironmentVariableItem> Variables { get; set; } = new();
    }

    public class EnvironmentManager
    {
        private readonly Dictionary<string, string> _activeVariables = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, string> ActiveVariables => _activeVariables;

        public void SetVariable(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            _activeVariables[key.Trim()] = value ?? string.Empty;
        }

        public string? GetVariable(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            return _activeVariables.TryGetValue(key.Trim(), out var val) ? val : null;
        }

        public void LoadEnvironment(ApiEnvironment environment)
        {
            _activeVariables.Clear();
            if (environment == null) return;

            foreach (var item in environment.Variables)
            {
                if (item.IsEnabled && !string.IsNullOrWhiteSpace(item.Key))
                {
                    _activeVariables[item.Key.Trim()] = item.Value ?? string.Empty;
                }
            }
        }

        /// <summary>
        /// Replaces all occurrences of {{variable_name}} with the corresponding environment variable.
        /// </summary>
        public string Interpolate(string? input)
        {
            if (string.IsNullOrEmpty(input)) return input ?? string.Empty;

            return Regex.Replace(input, @"\{\{([a-zA-Z0-9_\-\.]+)\}\}", match =>
            {
                var varName = match.Groups[1].Value.Trim();
                if (_activeVariables.TryGetValue(varName, out var replacement))
                {
                    return replacement;
                }
                return match.Value; // Keep unchanged if variable not defined
            });
        }
    }
}
