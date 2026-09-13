using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ZeroProbe.UI.Core;
using ZeroProbe.UI.Modules.ApiClient.Models;
using ZeroProbe.UI.Modules.ApiClient.Services;
using ZeroProbe.UI.ViewModels;

namespace ZeroProbe.UI.Modules.ApiClient
{
    public class ApiClientViewModel : ToolViewModelBase
    {
        private readonly HttpApiClientService _apiService = new();
        private readonly EnvironmentManager _envManager = new();
        private CancellationTokenSource? _activeCts;

        // Environment & Variables
        public EnvironmentManager EnvironmentMgr => _envManager;
        public ObservableCollection<ApiEnvironment> Environments { get; } = new();
        private ApiEnvironment? _selectedEnvironment;

        // Request Configuration
        private string _method = "GET";
        private string _url = "https://jsonplaceholder.typicode.com/posts/1";
        private BodyMode _bodyMode = BodyMode.None;
        private string _bodyText = "";
        private AuthMode _authMode = AuthMode.None;
        private string _authToken = "";
        private string _authUser = "";
        private string _authPass = "";
        private string _apiKeyName = "X-Api-Key";
        private string _apiKeyValue = "";
        private int _timeoutSeconds = 30;

        // Response State
        private bool _hasResponse;
        private int _statusCode;
        private string _statusText = "";
        private string _statusColor = "#64748B"; // TextMuted
        private string _elapsedText = "";
        private string _sizeText = "";
        private string _responseDisplayBody = "";
        private string _rawResponseBody = "";
        private string _formattedResponseBody = "";
        private bool _isPrettyView = true;

        public ApiClientViewModel()
        {
            AvailableMethods = new List<string> { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
            AvailableBodyModes = Enum.GetValues<BodyMode>().ToList();
            AvailableAuthModes = Enum.GetValues<AuthMode>().ToList();

            // Default Environments
            var noEnv = new ApiEnvironment("No Environment");
            var devEnv = new ApiEnvironment("Development (Localhost)", new[]
            {
                new EnvironmentVariableItem("baseUrl", "http://localhost:5000"),
                new EnvironmentVariableItem("token", "dev_secret_token_12345")
            });
            var jsonPlaceholderEnv = new ApiEnvironment("JSONPlaceholder Cloud", new[]
            {
                new EnvironmentVariableItem("baseUrl", "https://jsonplaceholder.typicode.com"),
                new EnvironmentVariableItem("postId", "1")
            });
            var httpBinEnv = new ApiEnvironment("HttpBin Inspection", new[]
            {
                new EnvironmentVariableItem("baseUrl", "https://httpbin.org")
            });

            Environments.Add(noEnv);
            Environments.Add(devEnv);
            Environments.Add(jsonPlaceholderEnv);
            Environments.Add(httpBinEnv);
            SelectedEnvironment = jsonPlaceholderEnv;

            // Default Headers
            Headers.Add(new HttpHeaderItem("Accept", "application/json", true));
            Headers.Add(new HttpHeaderItem("User-Agent", "ZeroProbe/2.0", true));

            // Default Query Param example
            QueryParams.Add(new HttpQueryParamItem("format", "json", false));

            // Commands
            AddParamCommand = new RelayCommand(_ => QueryParams.Add(new HttpQueryParamItem("", "", true)));
            RemoveParamCommand = new RelayCommand(p => { if (p is HttpQueryParamItem item) QueryParams.Remove(item); });
            AddHeaderCommand = new RelayCommand(_ => Headers.Add(new HttpHeaderItem("", "", true)));
            RemoveHeaderCommand = new RelayCommand(h => { if (h is HttpHeaderItem item) Headers.Remove(item); });
            AddVariableCommand = new RelayCommand(_ => SelectedEnvironment?.Variables.Add(new EnvironmentVariableItem("", "", true)));
            RemoveVariableCommand = new RelayCommand(v => { if (v is EnvironmentVariableItem item) SelectedEnvironment?.Variables.Remove(item); });
            ImportCurlCommand = new RelayCommand(DoImportCurl);
            FormatJsonBodyCommand = new RelayCommand(DoFormatJsonBody);
            CopyResponseCommand = new RelayCommand(DoCopyResponse);
            ClearHistoryCommand = new RelayCommand(_ => History.Clear());
            LoadHistoryItemCommand = new RelayCommand(h => { if (h is ApiHistoryItem item) LoadHistory(item); });
        }

        #region Request Properties

        public List<string> AvailableMethods { get; }
        public List<BodyMode> AvailableBodyModes { get; }
        public List<AuthMode> AvailableAuthModes { get; }

        public string Method
        {
            get => _method;
            set
            {
                if (SetProperty(ref _method, value))
                {
                    // Auto switch body mode to JSON on POST/PUT/PATCH if None
                    if ((value == "POST" || value == "PUT" || value == "PATCH") && BodyMode == BodyMode.None)
                    {
                        BodyMode = BodyMode.Json;
                    }
                }
            }
        }

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        public ObservableCollection<HttpQueryParamItem> QueryParams { get; } = new();
        public ObservableCollection<HttpHeaderItem> Headers { get; } = new();

        public BodyMode BodyMode
        {
            get => _bodyMode;
            set => SetProperty(ref _bodyMode, value);
        }

        public string BodyText
        {
            get => _bodyText;
            set => SetProperty(ref _bodyText, value);
        }

        public AuthMode AuthMode
        {
            get => _authMode;
            set
            {
                if (SetProperty(ref _authMode, value))
                {
                    OnPropertyChanged(nameof(IsBearerAuth));
                    OnPropertyChanged(nameof(IsBasicAuth));
                    OnPropertyChanged(nameof(IsApiKeyAuth));
                }
            }
        }

        public bool IsBearerAuth => AuthMode == AuthMode.Bearer;
        public bool IsBasicAuth => AuthMode == AuthMode.Basic;
        public bool IsApiKeyAuth => AuthMode == AuthMode.ApiKey;

        public string AuthToken
        {
            get => _authToken;
            set => SetProperty(ref _authToken, value);
        }

        public string AuthUser
        {
            get => _authUser;
            set => SetProperty(ref _authUser, value);
        }

        public string AuthPass
        {
            get => _authPass;
            set => SetProperty(ref _authPass, value);
        }

        public string ApiKeyName
        {
            get => _apiKeyName;
            set => SetProperty(ref _apiKeyName, value);
        }

        public string ApiKeyValue
        {
            get => _apiKeyValue;
            set => SetProperty(ref _apiKeyValue, value);
        }

        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set => SetProperty(ref _timeoutSeconds, value);
        }

        #endregion

        #region Response Properties

        public bool HasResponse
        {
            get => _hasResponse;
            set => SetProperty(ref _hasResponse, value);
        }

        public int StatusCode
        {
            get => _statusCode;
            set => SetProperty(ref _statusCode, value);
        }

        public string ResponseStatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public string ElapsedText
        {
            get => _elapsedText;
            set => SetProperty(ref _elapsedText, value);
        }

        public string SizeText
        {
            get => _sizeText;
            set => SetProperty(ref _sizeText, value);
        }

        public string ResponseDisplayBody
        {
            get => _responseDisplayBody;
            set => SetProperty(ref _responseDisplayBody, value);
        }

        public bool IsPrettyView
        {
            get => _isPrettyView;
            set
            {
                if (SetProperty(ref _isPrettyView, value))
                {
                    ResponseDisplayBody = value ? _formattedResponseBody : _rawResponseBody;
                }
            }
        }

        public ObservableCollection<KeyValuePair<string, string>> ResponseHeaders { get; } = new();
        public ObservableCollection<ApiHistoryItem> History { get; } = new();

        #endregion

        public ApiEnvironment? SelectedEnvironment
        {
            get => _selectedEnvironment;
            set
            {
                if (SetProperty(ref _selectedEnvironment, value))
                {
                    _envManager.LoadEnvironment(value);
                    if (value != null)
                    {
                        StatusText = $"Môi trường hoạt động: {value.Name}";
                    }
                }
            }
        }

        public RelayCommand AddParamCommand { get; }
        public RelayCommand RemoveParamCommand { get; }
        public RelayCommand AddHeaderCommand { get; }
        public RelayCommand RemoveHeaderCommand { get; }
        public RelayCommand AddVariableCommand { get; }
        public RelayCommand RemoveVariableCommand { get; }
        public RelayCommand ImportCurlCommand { get; }
        public RelayCommand FormatJsonBodyCommand { get; }
        public RelayCommand CopyResponseCommand { get; }
        public RelayCommand ClearHistoryCommand { get; }
        public RelayCommand LoadHistoryItemCommand { get; }

        #region Execution

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                StatusText = "Error: URL cannot be empty";
                return;
            }

            IsRunning = true;
            StatusText = $"Sending {Method} request to {Url}...";
            AddLog($"[API] {Method} {Url}");

            _activeCts?.Dispose();
            _activeCts = new CancellationTokenSource();

            _ = ExecuteRequestAsync(_activeCts.Token);
        }

        protected override void OnStop()
        {
            _activeCts?.Cancel();
            AddLog("[API] Request cancelled by user.");
            StatusText = "Cancelled";
            IsRunning = false;
        }

        protected override void OnEngineOutput(string line)
        {
            // Not used for native in-process HttpClient
        }

        private async Task ExecuteRequestAsync(CancellationToken ct)
        {
            try
            {
                _envManager.LoadEnvironment(SelectedEnvironment);

                var finalUrl = _envManager.Interpolate(Url);
                var finalParams = QueryParams.Select(p => new HttpQueryParamItem(
                    _envManager.Interpolate(p.Key),
                    _envManager.Interpolate(p.Value),
                    p.IsEnabled)).ToList();
                var finalHeaders = Headers.Select(h => new HttpHeaderItem(
                    _envManager.Interpolate(h.Key),
                    _envManager.Interpolate(h.Value),
                    h.IsEnabled)).ToList();
                var finalBody = _envManager.Interpolate(BodyText);
                var finalAuthToken = _envManager.Interpolate(AuthToken);
                var finalApiKeyValue = _envManager.Interpolate(ApiKeyValue);

                var response = await _apiService.SendAsync(
                    Method,
                    finalUrl,
                    finalParams,
                    finalHeaders,
                    BodyMode,
                    finalBody,
                    AuthMode,
                    finalAuthToken,
                    AuthUser,
                    AuthPass,
                    ApiKeyName,
                    finalApiKeyValue,
                    TimeoutSeconds,
                    ct);

                await Dispatcher.InvokeAsync(() =>
                {
                    HasResponse = true;
                    StatusCode = response.StatusCode;
                    ResponseStatusText = response.StatusDescription;
                    ElapsedText = $"{response.ElapsedMs} ms";
                    SizeText = FormatBytes(response.SizeBytes);

                    // Color code badge based on status
                    StatusColor = response.StatusCode switch
                    {
                        >= 200 and < 300 => "#A6E3A1", // Success Green
                        >= 300 and < 400 => "#38BDF8", // Redirect Cyan
                        >= 400 and < 500 => "#F9E2AF", // Client Warning Amber
                        >= 500 => "#F38BA8",           // Server Error Red
                        _ => "#F38BA8"
                    };

                    _rawResponseBody = response.RawBody;
                    _formattedResponseBody = response.FormattedBody;
                    ResponseDisplayBody = IsPrettyView ? _formattedResponseBody : _rawResponseBody;

                    ResponseHeaders.Clear();
                    foreach (var h in response.Headers)
                    {
                        ResponseHeaders.Add(h);
                    }

                    // Add to Session History
                    History.Insert(0, new ApiHistoryItem
                    {
                        Timestamp = DateTime.Now,
                        Method = Method,
                        Url = finalUrl,
                        StatusCode = response.StatusCode,
                        StatusDescription = response.StatusDescription,
                        ElapsedMs = response.ElapsedMs,
                        SizeBytes = response.SizeBytes,
                        RequestBody = finalBody,
                        BodyMode = BodyMode,
                        AuthMode = AuthMode
                    });

                    // Keep history to last 30 items
                    while (History.Count > 30)
                    {
                        History.RemoveAt(History.Count - 1);
                    }

                    StatusText = $"{response.StatusDescription} ({response.ElapsedMs} ms)";
                    AddLog($"[API] Result: {response.StatusDescription} in {response.ElapsedMs} ms ({SizeText})");
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    HasResponse = true;
                    StatusCode = 0;
                    ResponseStatusText = "Error";
                    StatusColor = "#F38BA8";
                    ElapsedText = "0 ms";
                    SizeText = "0 B";
                    _rawResponseBody = ex.ToString();
                    _formattedResponseBody = ex.ToString();
                    ResponseDisplayBody = ex.ToString();
                    StatusText = $"Error: {ex.Message}";
                    AddLog($"[API] Error: {ex.Message}");
                });
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    IsRunning = false;
                    RaiseCommandStates();
                });
            }
        }

        private void DoImportCurl(object? _)
        {
            var owner = Application.Current?.MainWindow;
            var dialog = new CurlImportDialog();
            if (owner != null) dialog.Owner = owner;
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.CurlCommand))
            {
                ApplyCurl(dialog.CurlCommand);
            }
        }

        public void ApplyCurl(string curlCommand)
        {
            try
            {
                var parsed = CurlParserService.Parse(curlCommand);
                if (!string.IsNullOrEmpty(parsed.Method))
                    Method = parsed.Method;
                if (!string.IsNullOrEmpty(parsed.Url))
                    Url = parsed.Url;
                if (!string.IsNullOrEmpty(parsed.Body))
                {
                    BodyText = parsed.Body;
                    BodyMode = parsed.BodyMode;
                }
                if (parsed.AuthMode != AuthMode.None)
                {
                    AuthMode = parsed.AuthMode;
                    if (parsed.AuthMode == AuthMode.Bearer)
                        AuthToken = parsed.AuthToken;
                }
                if (parsed.Headers.Count > 0)
                {
                    foreach (var h in parsed.Headers)
                    {
                        var existing = Headers.FirstOrDefault(x => x.Key.Equals(h.Key, StringComparison.OrdinalIgnoreCase));
                        if (existing != null)
                        {
                            existing.Value = h.Value;
                            existing.IsEnabled = true;
                        }
                        else
                        {
                            Headers.Add(h);
                        }
                    }
                }
                StatusText = $"Đã nhập cURL thành công: {Method} {Url}";
                AddLog($"[cURL] Imported: {Method} {Url} ({parsed.Headers.Count} headers, body: {parsed.BodyMode})");
            }
            catch (Exception ex)
            {
                StatusText = $"Lỗi phân tích cURL: {ex.Message}";
                AddLog($"[cURL] Parse Error: {ex.Message}");
            }
        }

        private void DoFormatJsonBody(object? _)
        {
            if (string.IsNullOrWhiteSpace(BodyText)) return;
            BodyText = HttpApiClientService.TryFormatJson(BodyText);
        }

        private void DoCopyResponse(object? _)
        {
            if (string.IsNullOrEmpty(ResponseDisplayBody)) return;
            try
            {
                Clipboard.SetText(ResponseDisplayBody);
                StatusText = "Copied response body to clipboard!";
            }
            catch { }
        }

        private void LoadHistory(ApiHistoryItem item)
        {
            Method = item.Method;
            Url = item.Url;
            BodyMode = item.BodyMode;
            BodyText = item.RequestBody;
            AuthMode = item.AuthMode;
            StatusText = $"Loaded request: {item.DisplayText}";
        }

        public void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case "JSONPlaceholder GET":
                    Method = "GET";
                    Url = "https://jsonplaceholder.typicode.com/posts/1";
                    BodyMode = BodyMode.None;
                    BodyText = "";
                    break;
                case "JSONPlaceholder POST":
                    Method = "POST";
                    Url = "https://jsonplaceholder.typicode.com/posts";
                    BodyMode = BodyMode.Json;
                    BodyText = "{\n  \"title\": \"ZeroProbe Postman Test\",\n  \"body\": \"Testing REST API Client in ZeroProbe\",\n  \"userId\": 1\n}";
                    break;
                case "HttpBin Headers":
                    Method = "GET";
                    Url = "https://httpbin.org/headers";
                    BodyMode = BodyMode.None;
                    BodyText = "";
                    break;
                case "HttpBin POST Form":
                    Method = "POST";
                    Url = "https://httpbin.org/post";
                    BodyMode = BodyMode.FormUrlEncoded;
                    BodyText = "username=zero\nrole=admin\nplatform=zero_universe";
                    break;
            }
            StatusText = $"Loaded preset: {presetName}";
        }

        #endregion
    }
}
