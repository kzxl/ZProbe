using System.Collections.Generic;
using Xunit;
using ZeroProbe.UI.Modules.ApiClient.Models;
using ZeroProbe.UI.Modules.ApiClient.Services;

namespace ZeroProbe.Tests
{
    public class ApiClientUpgradeTests
    {
        [Fact]
        public void EnvironmentManager_Interpolate_ReplacesVariablesCorrectly()
        {
            var env = new EnvironmentManager();
            env.SetVariable("baseUrl", "https://api.zerouniverse.io");
            env.SetVariable("userId", "10492");

            var template = "{{baseUrl}}/v1/users/{{userId}}/profile";
            var interpolated = env.Interpolate(template);

            Assert.Equal("https://api.zerouniverse.io/v1/users/10492/profile", interpolated);
        }

        [Fact]
        public void CurlParser_ParsesPostRequestWithHeadersAndBody()
        {
            var curl = "curl -X POST \"https://api.example.com/auth/login\" -H \"Content-Type: application/json\" -H \"Authorization: Bearer secret_token_xyz\" -d '{\"username\":\"admin\",\"password\":\"pass123\"}'";

            var req = CurlParserService.Parse(curl);

            Assert.Equal("POST", req.Method);
            Assert.Equal("https://api.example.com/auth/login", req.Url);
            Assert.Equal(AuthMode.Bearer, req.AuthMode);
            Assert.Equal("secret_token_xyz", req.AuthToken);
            Assert.Equal(BodyMode.Json, req.BodyMode);
            Assert.Contains("username", req.Body);
            Assert.Single(req.Headers); // Content-Type (Authorization was parsed into AuthMode)
            Assert.Equal("Content-Type", req.Headers[0].Key);
            Assert.Equal("application/json", req.Headers[0].Value);
        }

        [Fact]
        public void ResponseExtractor_ExtractsJsonPathCorrectly()
        {
            var json = """
            {
                "status": "success",
                "data": {
                    "token": "jwt.abc.123",
                    "user": {
                        "id": 42,
                        "name": "Alex"
                    }
                }
            }
            """;

            var token = ResponseExtractorService.ExtractValue(json, "$.data.token");
            var userId = ResponseExtractorService.ExtractValue(json, "data.user.id");
            var status = ResponseExtractorService.ExtractValue(json, "status");

            Assert.Equal("jwt.abc.123", token);
            Assert.Equal("42", userId);
            Assert.Equal("success", status);
        }

        [Fact]
        public void ResponseExtractor_HandlesArrayIndices()
        {
            var json = """
            {
                "items": [
                    { "id": 101, "title": "First" },
                    { "id": 102, "title": "Second" }
                ]
            }
            """;

            var secondId = ResponseExtractorService.ExtractValue(json, "items[1].id");
            var firstTitle = ResponseExtractorService.ExtractValue(json, "$.items[0].title");

            Assert.Equal("102", secondId);
            Assert.Equal("First", firstTitle);
        }

        [Fact]
        public void ResponseExtractor_ProcessChainingRules_UpdatesEnvironment()
        {
            var json = """{ "auth": { "jwt": "CHAINED_SECRET_999" } }""";
            var env = new EnvironmentManager();

            var rules = new List<ChainingRuleItem>
            {
                new() { TargetVariableName = "accessToken", JsonPath = "$.auth.jwt", IsEnabled = true }
            };

            int extracted = ResponseExtractorService.ProcessChainingRules(json, rules, env);

            Assert.Equal(1, extracted);
            Assert.Equal("CHAINED_SECRET_999", env.GetVariable("accessToken"));
        }
    }
}
