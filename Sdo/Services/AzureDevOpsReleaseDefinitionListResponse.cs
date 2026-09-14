// AzureDevOpsReleaseDefinitionListResponse.cs
// Azure DevOps classic release pipeline definition list response.


namespace Sdo.Services
{
    public class AzureDevOpsReleaseDefinitionListResponse
    {
        public int Count { get; set; }
        public List<AzureDevOpsReleaseDefinition>? Value { get; set; }
    }
}
