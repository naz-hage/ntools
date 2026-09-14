// AzureDevOpsRelease.cs
// Azure DevOps classic release instance model.


namespace Sdo.Services
{
    /// <summary>
    /// Represents a release created from a classic release pipeline definition.
    /// </summary>
    public class AzureDevOpsRelease
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Url { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}





