// AzureDevOpsReleaseDefinition.cs
// Azure DevOps classic release pipeline definition model.


namespace Sdo.Services
{
    /// <summary>
    /// Represents an Azure DevOps classic release pipeline definition.
    /// </summary>
    public class AzureDevOpsReleaseDefinition
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsDeleted { get; set; }
        public string? Path { get; set; }
        public string? Url { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public List<AzureDevOpsReleaseArtifact>? Artifacts { get; set; }


        public string DefinitionState => IsDeleted ? "deleted" : "defined";
    }


    public class AzureDevOpsReleaseArtifact
    {
        public string? Type { get; set; }
        public string? Alias { get; set; }
        public string? SourceId { get; set; }
        public Dictionary<string, AzureDevOpsReleaseReference>? DefinitionReference { get; set; }
    }


    public class AzureDevOpsReleaseReference
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}




