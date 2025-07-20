namespace Ntools
{
    public static class CurrentProcess
    {
        public static bool IsElevated()
        {
            // Always return false for test environment
            return false;
        }
    }
}
