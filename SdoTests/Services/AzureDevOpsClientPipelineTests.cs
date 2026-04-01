using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using Xunit;
using Sdo.Services;

namespace SdoTests.Services
{
    public class AzureDevOpsClientPipelineTests
    {
        private class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

            public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
            {
                _responder = responder ?? throw new ArgumentNullException(nameof(responder));
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _responder(request, cancellationToken);
            }
        }

        [Fact]
        public async Task ListPipelineDefinitionsAsync_ReturnsPipelineDefinitions()
        {
            var organization = "org";
            var project = "proj";

                        var definitionsJson = @"{
    ""value"": [
        {
            ""id"": 123,
            ""name"": ""CI"",
            ""path"": "".github/workflows/ci.yml"",
            ""type"": ""build"",
            ""url"": ""https://dev.azure.com/org/proj/_apis/build/definitions/123"",
            ""queueStatus"": ""enabled"",
            ""createdDate"": ""2026-01-01T00:00:00Z"",
            ""modifiedDate"": ""2026-01-02T00:00:00Z""
        }
    ]
}";

            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                if (req.Method == HttpMethod.Get && req.RequestUri!.AbsoluteUri.Contains("/_apis/build/definitions"))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(definitionsJson, Encoding.UTF8, "application/json")
                    };
                    return Task.FromResult(resp);
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

            using var httpClient = new HttpClient(handler);
            using var client = new AzureDevOpsClient(httpClient, organization, project);

            var defs = await client.ListPipelineDefinitionsAsync(project);

            Assert.NotNull(defs);
            Assert.Single(defs!);
            var first = defs![0];
            Assert.Equal("123", first.PlatformId);
            Assert.Equal("CI", first.Name);
            Assert.Equal(".github/workflows/ci.yml", first.Path);
            Assert.Equal("build", first.Type);
        }

        [Fact]
        public async Task ListPipelineRunsAsync_ReturnsPipelineRuns()
        {
            var organization = "org";
            var project = "proj";

                        var buildsJson = @"{
    ""value"": [
        {
            ""id"": 456,
            ""buildNumber"": ""2026.1"",
            ""status"": ""completed"",
            ""result"": ""succeeded"",
            ""sourceBranch"": ""refs/heads/main"",
            ""queueTime"": ""2026-03-01T00:00:00Z"",
            ""startTime"": ""2026-03-01T00:01:00Z"",
            ""finishTime"": ""2026-03-01T00:05:00Z"",
            ""definition"": { ""id"": 123 },
            ""logs"": { ""url"": ""https://dev.azure.com/org/proj/_apis/build/builds/456/logs"" },
            ""url"": ""https://dev.azure.com/org/proj/_build/results?buildId=456""
        }
    ]
}";

            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                if (req.Method == HttpMethod.Get && req.RequestUri!.AbsoluteUri.Contains("/_apis/build/builds"))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(buildsJson, Encoding.UTF8, "application/json")
                    };
                    return Task.FromResult(resp);
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

            using var httpClient = new HttpClient(handler);
            using var client = new AzureDevOpsClient(httpClient, organization, project);

            var runs = await client.ListPipelineRunsAsync(project, top: 1);

            Assert.NotNull(runs);
            Assert.Single(runs!);
            var run = runs![0];
            Assert.Equal("456", run.PlatformId);
            Assert.Equal("2026.1", run.Name);
            Assert.Contains("main", run.Branch ?? string.Empty);
            Assert.Equal("completed", run.Status);
            Assert.Equal("succeeded", run.Result);
            Assert.Equal("https://dev.azure.com/org/proj/_build/results?buildId=456", run.Url);
        }

        [Fact]
        public async Task GetPipelineRunLogsAsync_ReturnsConcatenatedLogs()
        {
            var organization = "org";
            var project = "proj";
            var buildId = 456;

            var logsListJson = @"{ ""value"": [ { ""id"": 1 }, { ""id"": 2 } ] }";

            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                var uri = req.RequestUri!.AbsoluteUri;
                if (req.Method == HttpMethod.Get && uri.Contains($"/builds/{buildId}/logs?"))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(logsListJson, Encoding.UTF8, "application/json")
                    });
                }

                if (req.Method == HttpMethod.Get && uri.Contains($"/builds/{buildId}/logs/1"))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("log entry one", Encoding.UTF8, "text/plain")
                    });
                }

                if (req.Method == HttpMethod.Get && uri.Contains($"/builds/{buildId}/logs/2"))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("log entry two", Encoding.UTF8, "text/plain")
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

            using var httpClient = new HttpClient(handler);
            using var client = new AzureDevOpsClient(httpClient, organization, project);

            var text = await client.GetPipelineRunLogsAsync(project, buildId);

            Assert.NotNull(text);
            Assert.Contains("===== Log 1 =====", text);
            Assert.Contains("log entry one", text);
            Assert.Contains("===== Log 2 =====", text);
            Assert.Contains("log entry two", text);
        }
    }
}
