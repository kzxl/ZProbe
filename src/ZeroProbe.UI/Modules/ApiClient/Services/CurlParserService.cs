using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ZeroProbe.UI.Modules.ApiClient.Models;

namespace ZeroProbe.UI.Modules.ApiClient.Services
{
    public class ParsedCurlRequest
    {
        public string Method { get; set; } = "GET";
        public string Url { get; set; } = string.Empty;
        public List<HttpHeaderItem> Headers { get; set; } = new();
        public string Body { get; set; } = string.Empty;
        public BodyMode BodyMode { get; set; } = BodyMode.None;
        public AuthMode AuthMode { get; set; } = AuthMode.None;
        public string AuthToken { get; set; } = string.Empty;
    }

    public static class CurlParserService
    {
        public static ParsedCurlRequest Parse(string curlCommand)
        {
            var result = new ParsedCurlRequest();
            if (string.IsNullOrWhiteSpace(curlCommand)) return result;

            var tokens = Tokenize(curlCommand.Trim());
            if (tokens.Count == 0) return result;

            // Remove leading "curl" if present
            int startIdx = 0;
            if (tokens[0].Equals("curl", StringComparison.OrdinalIgnoreCase))
            {
                startIdx = 1;
            }

            for (int i = startIdx; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if ((token == "-X" || token == "--request") && i + 1 < tokens.Count)
                {
                    result.Method = tokens[++i].ToUpperInvariant();
                }
                else if ((token == "-H" || token == "--header") && i + 1 < tokens.Count)
                {
                    var headerLine = tokens[++i];
                    var colonIdx = headerLine.IndexOf(':');
                    if (colonIdx > 0)
                    {
                        var key = headerLine.Substring(0, colonIdx).Trim();
                        var val = headerLine.Substring(colonIdx + 1).Trim();

                        if (key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) &&
                            val.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            result.AuthMode = AuthMode.Bearer;
                            result.AuthToken = val.Substring(7).Trim();
                        }
                        else
                        {
                            result.Headers.Add(new HttpHeaderItem(key, val));
                        }
                    }
                }
                else if ((token == "-d" || token == "--data" || token == "--data-raw" || token == "--data-binary") && i + 1 < tokens.Count)
                {
                    result.Body = tokens[++i];
                    if (result.Method == "GET")
                    {
                        result.Method = "POST";
                    }

                    var trimmedBody = result.Body.Trim();
                    if ((trimmedBody.StartsWith("{") && trimmedBody.EndsWith("}")) ||
                        (trimmedBody.StartsWith("[") && trimmedBody.EndsWith("]")))
                    {
                        result.BodyMode = BodyMode.Json;
                    }
                    else if (trimmedBody.Contains("=") || trimmedBody.Contains("&"))
                    {
                        result.BodyMode = BodyMode.FormUrlEncoded;
                    }
                    else
                    {
                        result.BodyMode = BodyMode.Raw;
                    }
                }
                else if (token.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         token.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                         token.StartsWith("{{", StringComparison.OrdinalIgnoreCase))
                {
                    result.Url = token;
                }
                else if (!token.StartsWith("-") && string.IsNullOrEmpty(result.Url))
                {
                    result.Url = token;
                }
            }

            return result;
        }

        private static List<string> Tokenize(string commandLine)
        {
            var tokens = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool isEscaped = false;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];

                if (isEscaped)
                {
                    current.Append(c);
                    isEscaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    isEscaped = true;
                    continue;
                }

                if (c == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    continue;
                }

                if (c == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inSingleQuote && !inDoubleQuote)
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
            }

            return tokens;
        }
    }
}
