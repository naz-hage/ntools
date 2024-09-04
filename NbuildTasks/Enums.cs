namespace NbuildTasks
{
    public class Enums
    {
        public enum BuildType
        {
            STAGE,
            PROD
        }

        public enum RetCode : int
        {
            Success = 0,
            InvalidParameter = -1,
            FileNotFound = -3,
            CloneProjectFailed = -4,
            SetStagingTagFailed = -5,
            SetProdTagFailed = -6,
            DeleteTagFailed = -7,
            SetBranchFailed = -9,
            AutoTagFailed = -10,
            SetAutoTagFailed = -11,
            GitWrapperFailed = -12,
            GetTagFailed = -13,
            GetBranchFailed = -14,
            SetTagFailed = -15,
            NotAGitRepository = -16,
            GitNotConfigured = -17,
        }
    }
}
