namespace Sdo.Services
{
    /// <summary>
    /// Represents a build definition reference in a build response.
    /// </summary>
    public class AzureDevOpsBuildDefinitionReference
    {
        /// <summary>
        /// Gets or sets the definition ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the definition name.
        /// </summary>
        public string? Name { get; set; }
    }
}