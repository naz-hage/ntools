namespace NbuildTasks
{
    public class Enums
    {
        public enum BuildType
        {
            STAGING,
            PRODUCTION
        }

        public enum RetCode : int
        {
            Success = 0,
            InvalidParameter = -1,
            FileNotFound = -3,
            CloneProjectFailed = -4,
            SetStagingTagFailed = -5,
            SetProductionTagFailed = -6,
            DeleteTagFailed = -7,
            SetBranchFailed = -9,
            AutoTagFailed = -10,
            SetAutoTagFailed = -11,
            GitWrapperFailed = -12,
            GetTagFailed = -13,
            GetBranchFailed = -14,
            SetTagFailed = -15,

        }
    }
}
