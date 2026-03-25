namespace Sdo.Mapping
{
    public interface IMappingGenerator
    {
        string PrCreateGitHub(string owner, string repo, string title, string file, string head, string @base = "main", bool draft = false);
        string PrListGitHub(string owner, string repo, string state, int top);
        string PrListAzure(string organization, string project, string repo, string state, int top);
        string PrCreateAzure(string organization, string project, string repo, string title, string headRef, string baseRef, bool draft = false, string? description = null);
        string PrShowAzure(string organization, string project, string repo, int id);
        string PrUpdateAzure(string organization, string project, string repo, int id, string? title, string? state, string? description);
        string IssueCreateGitHub(string owner, string repo, string title, string bodyOrFile, bool isBodyFile);
        string IssueListGitHub(string owner, string repo, string state, int top);
        string IssueShowGitHub(string owner, string repo, int id);
        string RepoListGitHub(string owner, int top);
        string RepoListAzure(string project, string organization, int top);
        string RepoCreateGitHub(string name, bool isPrivate, string description);
        string RepoCreateAzure(string name, string project, string organization);
        string BoardsQueryAzure(string organization, string project, string wiql, int top);
        string WorkItemShowAzure(string organization, string project, int id);
        string IssueUpdateGitHub(string owner, string repo, int id, string? title, string? state, string? body, string? assignee);
        string WorkItemUpdateAzure(string organization, string project, int id, string? title, string? state, string? assignee, string? description);
    }
}
