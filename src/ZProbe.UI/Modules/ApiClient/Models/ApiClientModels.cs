using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZProbe.UI.Modules.ApiClient.Models
{
    public enum AuthMode
    {
        None,
        Bearer,
        Basic,
        ApiKey
    }

    public enum BodyMode
    {
        None,
        Json,
        FormUrlEncoded,
        Raw
    }

    public class HttpHeaderItem : INotifyPropertyChanged
    {
        private string _key = "";
        private string _value = "";
        private bool _isEnabled = true;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public HttpHeaderItem() { }

        public HttpHeaderItem(string key, string value, bool isEnabled = true)
        {
            _key = key;
            _value = value;
            _isEnabled = isEnabled;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public class HttpQueryParamItem : INotifyPropertyChanged
    {
        private string _key = "";
        private string _value = "";
        private bool _isEnabled = true;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public HttpQueryParamItem() { }

        public HttpQueryParamItem(string key, string value, bool isEnabled = true)
        {
            _key = key;
            _value = value;
            _isEnabled = isEnabled;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public class ApiHistoryItem
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Method { get; set; } = "GET";
        public string Url { get; set; } = "";
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; } = "";
        public long ElapsedMs { get; set; }
        public long SizeBytes { get; set; }
        public string RequestBody { get; set; } = "";
        public BodyMode BodyMode { get; set; } = BodyMode.None;
        public AuthMode AuthMode { get; set; } = AuthMode.None;
        public string DisplayText => $"[{Method}] {Url} — {StatusCode} ({ElapsedMs}ms)";
    }

    public class ApiResponseModel
    {
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; } = "";
        public long ElapsedMs { get; set; }
        public long SizeBytes { get; set; }
        public string RawBody { get; set; } = "";
        public string FormattedBody { get; set; } = "";
        public List<KeyValuePair<string, string>> Headers { get; set; } = new();
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
        public string? ErrorMessage { get; set; }
    }
}
