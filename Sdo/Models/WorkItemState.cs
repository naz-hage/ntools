// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// WorkItemState.cs
//
// Enum for standardized work item states across platforms.

namespace Sdo.Models
{
    /// <summary>
    /// Canonical work item states from Python SDO.
    /// These represent the actual states used in Azure DevOps and GitHub.
    /// </summary>
    public enum WorkItemState
    {
        /// <summary>New state (maps to GitHub 'open')</summary>
        New,

        /// <summary>Approved state (maps to GitHub 'open')</summary>
        Approved,

        /// <summary>Committed state (maps to GitHub 'open')</summary>
        Committed,

        /// <summary>Done state (maps to GitHub 'closed')</summary>
        Done,

        /// <summary>To Do state (maps to GitHub 'open')</summary>
        ToDo,

        /// <summary>In Progress state (maps to GitHub 'open')</summary>
        InProgress
    }

    /// <summary>
    /// Helper class for translating work item states between platforms.
    /// Based on Python SDO state definitions and mappings.
    /// </summary>
    public static class WorkItemStateTranslator
    {
        /// <summary>
        /// Converts a WorkItemState to GitHub API state value.
        /// </summary>
        /// <param name="state">The work item state.</param>
        /// <returns>GitHub state string ("open" or "closed").</returns>
        public static string ToGitHubState(WorkItemState state)
        {
            return state switch
            {
                WorkItemState.New => "open",
                WorkItemState.Approved => "open",
                WorkItemState.Committed => "open",
                WorkItemState.ToDo => "open",
                WorkItemState.InProgress => "open",
                WorkItemState.Done => "closed",
                _ => "open"
            };
        }

        /// <summary>
        /// Converts a WorkItemState to Azure DevOps state value.
        /// Returns the exact state name as used in Azure DevOps.
        /// </summary>
        /// <param name="state">The work item state.</param>
        /// <returns>Azure DevOps state string (exact name with proper casing).</returns>
        public static string ToAzureDevOpsState(WorkItemState state)
        {
            return state switch
            {
                WorkItemState.New => "New",
                WorkItemState.Approved => "Approved",
                WorkItemState.Committed => "Committed",
                WorkItemState.Done => "Done",
                WorkItemState.ToDo => "To Do",
                WorkItemState.InProgress => "In Progress",
                _ => "New"
            };
        }

        /// <summary>
        /// Parses a string state value to a WorkItemState enum.
        /// Accepts user input (case-insensitive) and maps to canonical states.
        /// </summary>
        /// <param name="stateString">The state string to parse (case-insensitive).</param>
        /// <returns>The corresponding WorkItemState, or null if not recognized.</returns>
        public static WorkItemState? ParseState(string? stateString)
        {
            if (string.IsNullOrWhiteSpace(stateString))
                return null;

            var normalized = stateString.Trim().ToLower();

            // Accept any variation of the canonical states
            return normalized switch
            {
                // New state
                "new" => WorkItemState.New,
                
                // Approved state
                "approved" => WorkItemState.Approved,
                
                // Committed state
                "committed" => WorkItemState.Committed,
                
                // Done states (accept multiple variations)
                "done" => WorkItemState.Done,
                "closed" => WorkItemState.Done,
                
                // To Do states
                "to do" => WorkItemState.ToDo,
                "todo" => WorkItemState.ToDo,
                
                // In Progress states
                "in progress" => WorkItemState.InProgress,
                "inprogress" => WorkItemState.InProgress,
                
                _ => null
            };
        }

        /// <summary>
        /// Gets valid state values as a comma-separated string for help text.
        /// </summary>
        /// <returns>Comma-separated list of valid state names from Python SDO.</returns>
        public static string GetValidStatesForHelp()
        {
            return "New, Approved, Committed, Done, To Do, In Progress";
        }
    }
}
