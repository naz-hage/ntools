using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;
using Sdo.Services;

namespace SdoTests.Services
{
    public class GitHubClientWorkflowTests
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
        public async Task GetWorkflowAsync_ReturnsWorkflow()
        {
            var owner = "owner";
            var repo = "repo";
            var workflowId = 12345L;

            var workflowJson = $"{{ \"id\": {workflowId}, \"node_id\": \"node123\", \"name\": \"CI\", \"path\": \".github/workflows/ci.yml\", \"state\": \"active\" }}";

            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                if (req.Method == HttpMethod.Get && req.RequestUri!.AbsoluteUri.Contains($"/actions/workflows/{workflowId}"))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(workflowJson, Encoding.UTF8, "application/json")
                    };
                    return Task.FromResult(resp);
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

            using var httpClient = new HttpClient(handler);
            using var client = new GitHubClient(httpClient);

            var wf = await client.GetWorkflowAsync(owner, repo, workflowId);

            Assert.NotNull(wf);
            Assert.Equal(workflowId, wf!.Id);
            Assert.Equal("CI", wf.Name);
            Assert.Equal("active", wf.State);
        }

        [Fact]
        public async Task TriggerWorkflowAsync_ReturnsTrue_OnNoContent()
        {
            var owner = "owner";
            var repo = "repo";
            var workflowId = 555L;

            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                if (req.Method == HttpMethod.Post && req.RequestUri!.AbsoluteUri.Contains($"/actions/workflows/{workflowId}/dispatches"))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

            using var httpClient = new HttpClient(handler);
            using var client = new GitHubClient(httpClient);

            var success = await client.TriggerWorkflowAsync(owner, repo, workflowId, "main");
            Assert.True(success);
        }

        [Fact]
        public async Task GetWorkflowLogsAsync_ReturnsExtractedText()
        {
            var owner = "owner";
            var repo = "repo";
            var runId = 9999L;

            // Create an in-memory zip stream with a single entry
            var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry("logs/job1.txt");
                using var es = entry.Open();
                using var sw = new StreamWriter(es, Encoding.UTF8, 1024, leaveOpen: true);
                sw.Write("hello logs");
                sw.Flush();
            }
            ms.Seek(0, SeekOrigin.Begin);

            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                if (req.Method == HttpMethod.Get && req.RequestUri!.AbsoluteUri.Contains($"/actions/runs/{runId}/logs"))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StreamContent(ms)
                    };
                    resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
                    return Task.FromResult(resp);
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

            using var httpClient = new HttpClient(handler);
            using var client = new GitHubClient(httpClient);

            var text = await client.GetWorkflowLogsAsync(owner, repo, runId);

            Assert.NotNull(text);
            Assert.Contains("===== logs/job1.txt =====", text);
            Assert.Contains("hello logs", text);
        }
    }
}
