// AzureDevOpsReleaseListResponse.cs
// Azure DevOps classic release instance list response.


namespace Sdo.Services
{
    public class AzureDevOpsReleaseListResponse
    {
        public int Count { get; set; }
        public List<AzureDevOpsRelease>? Value { get; set; }
    }
}




