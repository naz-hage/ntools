using System;

namespace Sdo.Mapping
{
    public class MappingGenerator : IMappingGenerator
    {
        public string PrCreateGitHub(string owner, string repo, string title, string file, string head, string @base = "main", bool draft = false)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                return string.Empty;

            var mapping = $"gh pr create -R {owner}/{repo} --title \"{title}\" --body-file \"{file}\" --base {@base} --head {head}";
            if (draft) mapping += " --draft";
            return mapping;
        }

        public string PrListGitHub(string owner, string repo, string state, int top)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                return string.Empty;
            return $"gh pr list -R {owner}/{repo} --state {state} --limit {top}";
        }

        public string IssueCreateGitHub(string owner, string repo, string title, string bodyOrFile, bool isBodyFile)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)) return string.Empty;
            if (isBodyFile)
            {
                return $"gh issue create -R {owner}/{repo} --title \"{title}\" --body-file \"{bodyOrFile}\"";
            }
            else
            {
                var bodyEscaped = (bodyOrFile ?? string.Empty).Replace("\"", "\\\"");
                return $"gh issue create -R {owner}/{repo} --title \"{title}\" --body \"{bodyEscaped}\"";
            }
        }

        public string IssueListGitHub(string owner, string repo, string state, int top)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)) return string.Empty;
            var topPart = top > 0 ? $"--limit {top}" : string.Empty;
            return $"gh issue list -R {owner}/{repo} --state {state} {topPart}".Replace("  ", " ").Trim();
        }

        public string IssueShowGitHub(string owner, string repo, int id)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)) return string.Empty;
            return $"gh issue view -R {owner}/{repo} {id}";
        }

        public string IssueUpdateGitHub(string owner, string repo, int id, string? title, string? state, string? body, string? assignee)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)) return string.Empty;
            var parts = new System.Collections.Generic.List<string>();
            parts.Add($"gh issue edit -R {owner}/{repo} {id}");
            if (!string.IsNullOrEmpty(title)) parts.Add($"--title \"{title}\"");
            if (!string.IsNullOrEmpty(state)) parts.Add($"--state {state}");
            if (!string.IsNullOrEmpty(body)) parts.Add($"--body \"{body.Replace("\"", "\\\"")}\"");
            if (!string.IsNullOrEmpty(assignee)) parts.Add($"--assignee {assignee}");
            return string.Join(" ", parts);
        }

        public string RepoList(string owner, int top)
        {
            if (string.IsNullOrEmpty(owner)) return string.Empty;
            return $"gh repo list {owner} --visibility all --limit {top}";
        }

        public string RepoListAzure(string project, string organization, int top)
        {
            return $"az repos list --project \"{project}\" --organization \"{organization}\" --top {top}";
        }

        public string RepoCreate(string name, bool isPrivate, string description)
        {
            var vis = isPrivate ? "--private" : "--public";
            var desc = (description ?? string.Empty).Replace("\"", "\\\"");
            return $"gh repo create {name} {vis} --description \"{desc}\"";
        }

        public string RepoCreateAzure(string name, string project, string organization)
        {
            return $"az repos create --name \"{name}\" --project \"{project}\" --organization \"{organization}\"";
        }

        public string PrListAzure(string organization, string project, string repo, string state, int top)
        {
            if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project) || string.IsNullOrEmpty(repo)) return string.Empty;
            return $"az repos pr list --org \"{organization}\" --project \"{project}\" --repository \"{repo}\" --status {state} --top {top}";
        }

        public string PrCreateAzure(string organization, string project, string repo, string title, string headRef, string baseRef, bool draft = false, string? description = null)
        {
            if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project) || string.IsNullOrEmpty(repo)) return string.Empty;
            var desc = (description ?? string.Empty).Replace("\"", "\\\"");
            var mapping = $"az repos pr create --org \"{organization}\" --project \"{project}\" --repository \"{repo}\" --title \"{title}\" --source-branch {headRef} --target-branch {baseRef}";
            if (!string.IsNullOrEmpty(desc)) mapping += $" --description \"{desc}\"";
            if (draft) mapping += " --draft";
            return mapping;
        }

        public string PrShowAzure(string organization, string project, string repo, int id)
        {
            if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project) || string.IsNullOrEmpty(repo)) return string.Empty;
            return $"az repos pr show --org \"{organization}\" --project \"{project}\" --repository \"{repo}\" --id {id}";
        }

        public string PrUpdateAzure(string organization, string project, string repo, int id, string? title, string? state, string? description)
        {
            if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project) || string.IsNullOrEmpty(repo)) return string.Empty;
            var parts = new System.Collections.Generic.List<string>();
            parts.Add($"az repos pr update --org \"{organization}\" --project \"{project}\" --repository \"{repo}\" --id {id}");
            if (!string.IsNullOrEmpty(title)) parts.Add($"--title \"{title}\"");
            if (!string.IsNullOrEmpty(state)) parts.Add($"--status {state}");
            if (!string.IsNullOrEmpty(description)) parts.Add($"--description \"{description.Replace("\"", "\\\"")}\"");
            return string.Join(" ", parts);
        }

        public string BoardsQueryAzure(string organization, string project, string wiql, int top)
        {
            var topPart = top > 0 ? $" --top {top}" : string.Empty;
            // Ensure WIQL is quoted
            return $"az boards query --org {organization} --project \"{project}\" --wiql \"{wiql}\"{topPart}";
        }

        public string WorkItemShowAzure(string organization, string project, int id)
        {
            if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project)) return string.Empty;
            return $"az boards work-item show --id {id} --org {organization} --project \"{project}\"";
        }

        public string WorkItemUpdateAzure(string organization, string project, int id, string? title, string? state, string? assignee, string? description)
        {
            if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project)) return string.Empty;
            var fields = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(state)) fields.Add($"System.State={state}");
            if (!string.IsNullOrEmpty(title)) fields.Add($"System.Title={title.Replace("\"", "\\\"")}");
            if (!string.IsNullOrEmpty(assignee)) fields.Add($"System.AssignedTo={assignee}");
            if (!string.IsNullOrEmpty(description)) fields.Add($"System.Description={description.Replace("\"", "\\\"")}");
            var fieldsArg = fields.Count > 0 ? $" --fields \"{string.Join("\" \"", fields)}\"" : string.Empty;
            return $"az boards work-item update --id {id} --org {organization} --project \"{project}\"{fieldsArg}";
        }

        public string RepoListGitHub(string owner, int top)
        {
            if (string.IsNullOrEmpty(owner)) return string.Empty;
            return $"gh repo list {owner} --visibility all --limit {top}";
        }

        public string RepoCreateGitHub(string name, bool isPrivate, string description)
        {
            var vis = isPrivate ? "--private" : "--public";
            var desc = (description ?? string.Empty).Replace("\"", "\\\"");
            return $"gh repo create {name} {vis} --description \"{desc}\"";
        }
    }
}
