using System.Net.Http;
using System.Threading.Tasks;

namespace Nbuild
{
    public delegate IReleaseService ReleaseServiceFactory(string repo);

    public class ReleaseServiceAdapter : IReleaseService
    {
        private readonly GitHubRelease.ReleaseService _inner;

        public ReleaseServiceAdapter(string repo)
        {
            _inner = new GitHubRelease.ReleaseService(repo);
        }

        public Task<HttpResponseMessage> DownloadAssetByName(string tagName, string assetName, string downloadPath)
        {
            return _inner.DownloadAssetByName(tagName, assetName, downloadPath);
        }
    }
}
