## Release {{buildDetails.buildNumber}}

### What's Changed
{{#forEach commits}}
{{#if (gt (length this.changes) 0)}}
<br>**{{moment this.author.date format="dd-MMM-yy"}}**
{{/if}}
* {{#if this.pullRequestId}}[{{truncate this.commitId 7}}]({{this.remoteUrl}}) - {{this.comment}} by @{{this.author.displayName}} in https://dev.azure.com/{{buildDetails.project.name}}/_git/{{buildDetails.repository.name}}/pullrequest/{{this.pullRequestId}}{{else}}[{{truncate this.commitId 7}}]({{this.remoteUrl}}) - {{this.comment}} by @{{this.author.displayName}}{{/if}}
{{/forEach}}

### New Contributors
{{#forEach workItems}}
{{#if this.isNewContributor}}
* @{{lookup this.fields 'System.AssignedTo' 'displayName'}} made their first contribution {{#if this.pullRequestId}}in https://dev.azure.com/{{buildDetails.project.name}}/_git/{{buildDetails.repository.name}}/pullrequest/{{this.pullRequestId}}{{/if}}
{{/if}}
{{/forEach}}

### Full Changelog
**Full Changelog**: https://dev.azure.com/{{buildDetails.project.name}}/_git/{{buildDetails.repository.name}}/branchCompare?baseVersion=GT{{compareBuildDetails.sourceVersion}}&targetVersion=GT{{buildDetails.sourceVersion}}