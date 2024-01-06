using CommandLine.Attributes;

namespace Ngit;

public class Cli
{
    [OptionalArgument("", "git", "git Command, value= [gettag | settag| autotag| autoversion| deletetag | getbranch | setbranch| createbranch]\n" +
                            " \t gettag\t\t -> Get tag of a branch for a given project\n" +
                            " \t settag\t\t -> Set specied tag of a branch for a given project\n" +
                            " \t autotag\t\t -> Set next tag based of branch and project on STAGING vs.PRODUCTION build (commit to remote repo)\n" +
                            " \t autoversion\t -> Equivalent to `autotag` cmd (Does not commit to remote repo)\n" +
                            " \t deletetag\t -> Delete specified tag of a branch for a given Project\n" +
                            " \t getbranch\t -> Get the current branch for a given project\n" +
                            " \t setbranch\t -> Set/checkout specified branch for a given project\n" +
                            " \t createbranch\t -> Create specified branch for a given project\n" +
                            " \t clone \t\t -> Clone a Project")]
    public string? GitCommand { get; set; }

    [OptionalArgument("", "org", "Organization Name")]
    public string? Organization { get; set; }

    [OptionalArgument("", "url", "GitHub Url Name")]
    public string? Url { get; set; }

    [OptionalArgument("", "branch", "Branch Name")]
    public string? Branch { get; set; }

    [OptionalArgument("", "tag", "Tag Name")]
    public string? Tag { get; set; }

    [OptionalArgument("", "buildtype", "Values: STAGING | PRODUCTION")]
    public string? BuildType { get; internal set; }
    [OptionalArgument(false, "v", "verbose. value = [true | false]")]
    public bool Verbose { get; set; }
}