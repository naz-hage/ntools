using Xunit;
using Sdo.Mapping;

namespace SdoTests
{
    public class MappingGeneratorTests
    {
        private readonly MappingGenerator _gen = new MappingGenerator();

        [Fact]
        public void PrCreate_GeneratesExpectedGitHubCommand()
        {
            var cmd = _gen.PrCreateGitHub("owner","repo","My Title","file.md","feature","main", false);
            Assert.Equal("gh pr create -R owner/repo --title \"My Title\" --body-file \"file.md\" --base main --head feature", cmd);
        }

        [Fact]
        public void PrList_GeneratesExpected()
        {
            var cmd = _gen.PrListGitHub("owner","repo","open",5);
            Assert.Equal("gh pr list -R owner/repo --state open --limit 5", cmd);
        }

        [Fact]
        public void RepoList_GeneratesExpected()
        {
            var cmd = _gen.RepoList("owner", 10);
            Assert.Equal("gh repo list owner --visibility all --limit 10", cmd);
        }

        [Fact]
        public void RepoListAzure_GeneratesExpected()
        {
            var cmd = _gen.RepoListAzure("project","org",3);
            Assert.Equal("az repos list --project \"project\" --organization \"org\" --top 3", cmd);
        }

        [Fact]
        public void RepoCreate_GeneratesExpected()
        {
            var cmd = _gen.RepoCreate("name", true, "desc");
            Assert.Equal("gh repo create name --private --description \"desc\"", cmd);
        }
    }
}
