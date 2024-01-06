namespace Ngit;

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
        GetTagFailed = -2,
        GetBranchFailed = -3,
        SetTagFailed = -4,
        SetStagingTagFailed = -5,
        SetProductionTagFailed = -6,
        DeleteTagFailed = -7,
        CloneProjectFailed = -8,
        SetBranchFailed = -9,
        AutoTagFailed = -10,
        SetAutoTagFailed = -11,
        GitWrapperFailed = -12
    }
}
