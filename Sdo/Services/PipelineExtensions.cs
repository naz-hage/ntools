// PipelineExtensions.cs
// Conversion helpers to map platform-specific DTOs to neutral pipeline models.

using System;

namespace Sdo.Services
{
    public static class PipelineExtensions
    {
        public static PipelineDefinition? ToPipelineDefinition(this GitHubWorkflow? wf)
        {
            if (wf == null) return null;
            return new PipelineDefinition
            {
                PlatformId = wf.Id.ToString(),
                Name = wf.Name,
                Path = wf.Path,
                Url = wf.HtmlUrl,
                State = wf.State,
                CreatedAt = wf.CreatedAt,
                UpdatedAt = wf.UpdatedAt
            };
        }

        public static PipelineDefinition? ToPipelineDefinition(this AzureDevOpsBuildDefinition? def)
        {
            if (def == null) return null;
            return new PipelineDefinition
            {
                PlatformId = def.Id.ToString(),
                Name = def.Name,
                Path = def.Path,
                Type = def.Type,
                Url = def.Url,
                State = def.QueueStatus,
                CreatedAt = def.CreatedDate,
                UpdatedAt = def.ModifiedDate
            };
        }

        public static PipelineRun? ToPipelineRun(this GitHubWorkflowRun? run)
        {
            if (run == null) return null;
            return new PipelineRun
            {
                PlatformId = run.Id.ToString(),
                Name = run.Name,
                Branch = run.HeadBranch,
                Status = run.Status,
                Result = run.Conclusion,
                StartedAt = run.RunStartedAt,
                FinishedAt = run.UpdatedAt,
                Url = run.HtmlUrl
            };
        }

        public static PipelineRun? ToPipelineRun(this AzureDevOpsBuild? build)
        {
            if (build == null) return null;
            return new PipelineRun
            {
                PlatformId = build.Id.ToString(),
                Name = build.BuildNumber,
                Branch = build.SourceBranch,
                Status = build.Status,
                Result = build.Result,
                StartedAt = build.StartTime,
                FinishedAt = build.FinishTime,
                Url = build.Url
            };
        }
    }
}
