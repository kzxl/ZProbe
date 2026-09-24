using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using ZProbe.UI.ViewModels;

namespace ZProbe.UI.Modules.ApiClient.Services
{
    public class EnvironmentVariableItem : ViewModelBase
    {
        private string _key = string.Empty;
        private string _value = string.Empty;
        private bool _isEnabled = true;

        public string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public EnvironmentVariableItem() { }

        public EnvironmentVariableItem(string key, string value, bool isEnabled = true)
        {
            _key = key;
            _value = value;
            _isEnabled = isEnabled;
        }
    }

    public class ApiEnvironment : ViewModelBase
    {
        private string _name = "Default";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ObservableCollection<EnvironmentVariableItem> Variables { get; } = new();

        public ApiEnvironment() { }

        public ApiEnvironment(string name, IEnumerable<EnvironmentVariableItem>? variables = null)
        {
            _name = name;
            if (variables != null)
            {
                foreach (var v in variables)
                {
                    Variables.Add(v);
                }
            }
        }
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

        public void LoadEnvironment(ApiEnvironment? environment)
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

        public void SyncFromEnvironment(ApiEnvironment? environment)
        {
            LoadEnvironment(environment);
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
