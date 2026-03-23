using Nbuild.Helpers;

namespace Sdo.Mapping
{
    public class ConsoleMappingPresenter : IMappingPresenter
    {
        public void Present(string mapping)
        {
            if (string.IsNullOrEmpty(mapping)) return;
            ConsoleHelper.WriteLine(mapping, System.ConsoleColor.Yellow);
        }
    }
}
