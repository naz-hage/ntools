using System.Net.Http;
using System.Threading.Tasks;

namespace Nbuild
{
    public interface IReleaseService
    {
        Task<HttpResponseMessage> DownloadAssetByName(string tagName, string assetName, string downloadPath);
    }
}
