using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Sdo.Services
{
    /// <summary>
    /// Represents the API response for a work item.
    /// </summary>
    public class WorkItemResponse
    {
        /// <summary>
        /// Gets or sets the work item ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work item URL (top-level property from API response).
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the work item fields.
        /// </summary>
        public Dictionary<string, object?>? Fields { get; set; }

        /// <summary>
        /// Converts to AzureDevOpsWorkItem.
        /// </summary>
        public AzureDevOpsWorkItem ToWorkItem()
        {
            var item = new AzureDevOpsWorkItem
            {
                Id = Id,
                // Prefer top-level Url property, fall back to Fields if not available
                Url = !string.IsNullOrEmpty(Url) ? Url :
                    (Fields?.ContainsKey("System.Url") == true ? Fields["System.Url"]?.ToString() : null),
            };

            if (Fields != null)
            {
                item.Title = Fields.ContainsKey("System.Title") ? Fields["System.Title"]?.ToString() ?? "Unknown" : "Unknown";
                item.Type = Fields.ContainsKey("System.WorkItemType") ? Fields["System.WorkItemType"]?.ToString() ?? "Unknown" : "Unknown";
                item.State = Fields.ContainsKey("System.State") ? Fields["System.State"]?.ToString() ?? "New" : "New";
                item.Description = Fields.ContainsKey("System.Description") ? Fields["System.Description"]?.ToString() : null;
                
                if (Fields.ContainsKey("System.CreatedDate") && DateTime.TryParse(Fields["System.CreatedDate"]?.ToString(), out var createdDate))
                {
                    item.CreatedDate = createdDate;
                }

                if (Fields.ContainsKey("System.ChangedDate") && DateTime.TryParse(Fields["System.ChangedDate"]?.ToString(), out var changedDate))
                {
                    item.ChangedDate = changedDate;
                }

                // Extract sprint/iteration path - get the last component after backslash
                if (Fields.ContainsKey("System.IterationPath"))
                {
                    var iterationPath = Fields["System.IterationPath"]?.ToString();
                    if (!string.IsNullOrEmpty(iterationPath) && iterationPath.Contains("\\"))
                    {
                        item.Sprint = iterationPath.Split("\\").LastOrDefault();
                    }
                    else if (!string.IsNullOrEmpty(iterationPath))
                    {
                        item.Sprint = iterationPath;
                    }
                }

                // Extract area path - get the last component after backslash
                if (Fields.ContainsKey("System.AreaPath"))
                {
                    var areaPath = Fields["System.AreaPath"]?.ToString();
                    if (!string.IsNullOrEmpty(areaPath) && areaPath.Contains("\\"))
                    {
                        item.Area = areaPath.Split("\\").LastOrDefault();
                    }
                    else if (!string.IsNullOrEmpty(areaPath))
                    {
                        item.Area = areaPath;
                    }
                }

                // Extract assigned to user name
                if (Fields.ContainsKey("System.AssignedTo"))
                {
                    var assignedToField = Fields["System.AssignedTo"];
                    if (assignedToField != null)
                    {
                        // Try to handle as JsonElement with displayName property
                        if (assignedToField is JsonElement jElement)
                        {
                            if (jElement.TryGetProperty("displayName", out var displayName))
                            {
                                item.AssignedTo = displayName.GetString();
                            }
                            else if (jElement.ValueKind == JsonValueKind.String)
                            {
                                item.AssignedTo = jElement.GetString();
                            }
                        }
                        else
                        {
                            // Fallback: just convert to string
                            item.AssignedTo = assignedToField.ToString();
                        }
                    }
                }

                // Comments count is typically in comments section, defaulting to 0
                item.CommentCount = 0;

                // Extract acceptance criteria
                if (Fields.ContainsKey("Microsoft.VSTS.Common.AcceptanceCriteria"))
                {
                    var acceptanceCriteria = Fields["Microsoft.VSTS.Common.AcceptanceCriteria"]?.ToString();
                    if (!string.IsNullOrEmpty(acceptanceCriteria))
                    {
                        item.AcceptanceCriteria = acceptanceCriteria;
                    }
                }
            }

            return item;
        }
    }
}