using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ZProbe.UI.Modules.ApiClient.Models;

namespace ZProbe.UI.Modules.ApiClient.Services
{
    public class HttpApiClientService
    {
        private static readonly HttpClient SharedClient;

        static HttpApiClientService()
        {
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = true,
                ConnectTimeout = TimeSpan.FromSeconds(15),
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };

            SharedClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            SharedClient.DefaultRequestHeaders.UserAgent.ParseAdd("ZProbe-API-Client/2.0 (ZeroUniverse; Windows)");
        }

        public async Task<ApiResponseModel> SendAsync(
            string method,
            string rawUrl,
            IEnumerable<HttpQueryParamItem> queryParams,
            IEnumerable<HttpHeaderItem> headers,
            BodyMode bodyMode,
            string bodyText,
            AuthMode authMode,
            string authToken,
            string authUser,
            string authPass,
            string apiKeyName,
            string apiKeyValue,
            int timeoutSeconds,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var responseModel = new ApiResponseModel();

            try
            {
                // 1. Build URI with Query Parameters
                var uriBuilder = new UriBuilder(NormalizeUrl(rawUrl));
                var activeParams = queryParams.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Key)).ToList();
                if (activeParams.Count > 0)
                {
                    var existingQuery = uriBuilder.Query.TrimStart('?');
                    var queryList = new List<string>();
                    if (!string.IsNullOrEmpty(existingQuery))
                    {
                        queryList.Add(existingQuery);
                    }
                    foreach (var param in activeParams)
                    {
                        var k = Uri.EscapeDataString(param.Key);
                        var v = Uri.EscapeDataString(param.Value ?? "");
                        queryList.Add($"{k}={v}");
                    }
                    uriBuilder.Query = string.Join("&", queryList);
                }

                using var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), uriBuilder.Uri);

                // 2. Authentication
                switch (authMode)
                {
                    case AuthMode.Bearer:
                        if (!string.IsNullOrWhiteSpace(authToken))
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Trim());
                        }
                        break;
                    case AuthMode.Basic:
                        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{authUser}:{authPass}"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                        break;
                    case AuthMode.ApiKey:
                        if (!string.IsNullOrWhiteSpace(apiKeyName))
                        {
                            request.Headers.TryAddWithoutValidation(apiKeyName.Trim(), apiKeyValue ?? "");
                        }
                        break;
                }

                // 3. Request Body
                HttpContent? content = null;
                if (method != "GET" && method != "HEAD")
                {
                    switch (bodyMode)
                    {
                        case BodyMode.Json:
                            content = new StringContent(bodyText ?? "", Encoding.UTF8, "application/json");
                            break;
                        case BodyMode.FormUrlEncoded:
                            var formPairs = ParseFormPairs(bodyText);
                            content = new FormUrlEncodedContent(formPairs);
                            break;
                        case BodyMode.Raw:
                            content = new StringContent(bodyText ?? "", Encoding.UTF8, "text/plain");
                            break;
                    }
                }
                request.Content = content;

                // 4. Custom Headers
                foreach (var h in headers.Where(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.Key)))
                {
                    var key = h.Key.Trim();
                    var val = h.Value ?? "";

                    if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        if (request.Content != null)
                        {
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(val);
                        }
                    }
                    else
                    {
                        request.Headers.TryAddWithoutValidation(key, val);
                    }
                }

                // 5. Send with Cancellation Token and Timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(3, timeoutSeconds)));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                var response = await SharedClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, linkedCts.Token);
                sw.Stop();

                responseModel.StatusCode = (int)response.StatusCode;
                responseModel.StatusDescription = $"{response.StatusCode} ({(int)response.StatusCode})";
                responseModel.ElapsedMs = sw.ElapsedMilliseconds;

                // 6. Response Headers
                var headerList = new List<KeyValuePair<string, string>>();
                foreach (var (k, v) in response.Headers)
                {
                    headerList.Add(new KeyValuePair<string, string>(k, string.Join(", ", v)));
                }
                if (response.Content != null)
                {
                    foreach (var (k, v) in response.Content.Headers)
                    {
                        headerList.Add(new KeyValuePair<string, string>(k, string.Join(", ", v)));
                    }
                }
                responseModel.Headers = headerList;

                // 7. Response Body & Sizing
                var rawBody = response.Content != null ? await response.Content.ReadAsStringAsync(linkedCts.Token) : "";
                responseModel.RawBody = rawBody;
                responseModel.SizeBytes = Encoding.UTF8.GetByteCount(rawBody);

                // Format JSON if applicable
                responseModel.FormattedBody = TryFormatJson(rawBody);

                return responseModel;
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                responseModel.StatusCode = 0;
                responseModel.StatusDescription = ct.IsCancellationRequested ? "Cancelled by User" : "Request Timed Out";
                responseModel.ElapsedMs = sw.ElapsedMilliseconds;
                responseModel.ErrorMessage = "The request was cancelled or timed out before completion.";
                return responseModel;
            }
            catch (Exception ex)
            {
                sw.Stop();
                responseModel.StatusCode = 0;
                responseModel.StatusDescription = "Network Error";
                responseModel.ElapsedMs = sw.ElapsedMilliseconds;
                responseModel.ErrorMessage = ex.Message;
                responseModel.RawBody = ex.ToString();
                responseModel.FormattedBody = ex.ToString();
                return responseModel;
            }
        }

        public static string TryFormatJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var trimmed = input.Trim();
            if (!((trimmed.StartsWith("{") && trimmed.EndsWith("}")) || (trimmed.StartsWith("[") && trimmed.EndsWith("]"))))
            {
                return input;
            }

            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                return input;
            }
        }

        private static string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "http://localhost";
            var trimmed = url.Trim();
            if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return "https://" + trimmed;
            }
            return trimmed;
        }

        private static IEnumerable<KeyValuePair<string, string>> ParseFormPairs(string text)
        {
            var list = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrWhiteSpace(text)) return list;

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var idx = line.IndexOf('=');
                if (idx > 0)
                {
                    var k = line.Substring(0, idx).Trim();
                    var v = line.Substring(idx + 1).Trim();
                    list.Add(new KeyValuePair<string, string>(k, v));
                }
                else
                {
                    var colIdx = line.IndexOf(':');
                    if (colIdx > 0)
                    {
                        var k = line.Substring(0, colIdx).Trim();
                        var v = line.Substring(colIdx + 1).Trim();
                        list.Add(new KeyValuePair<string, string>(k, v));
                    }
                }
            }
            return list;
        }
    }
}
