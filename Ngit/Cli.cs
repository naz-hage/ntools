using CommandLine.Attributes;

namespace Ngit;

public class Cli
{
    [OptionalArgument("", "c", "git Command, value= [tag | settag| autotag| setautotag| deletetag | branch | clone]\n" +
                            "     tag\t -> Get the current tag\n" +
                            "     autotag\t -> Set next tag based on the build type: STAGE vs.PROD\n" +
                            "     pushtag\t -> push specified tag in -tag option to remote repo\n" +
                            "     settag\t -> Set specified tag in -tag option \n" +
                            "     deletetag\t -> Delete specified tag in -tag option\n" +
                            "     branch\t -> Get the current branch\n" +
                            "     clone \t -> Clone specified Git repo in the -url option")]
    public string? GitCommand { get; set; }

    [OptionalArgument("", "url", "Git repo path")]
    public string? Url { get; set; }

    //[OptionalArgument("", "branch", "Branch Name")]
    //public string? Branch { get; set; }

    [OptionalArgument("", "tag", "Tag used for -c settag and -c deletetag")]
    public string? Tag { get; set; }

    [OptionalArgument("", "buildtype", "Build type used for -c autotag and -c setautotag Values: STAGE | PROD")]
    public string? BuildType { get; internal set; }

    [OptionalArgument(false, "v", "verbose. value = [true | false]")]
    public bool Verbose { get; set; }
}