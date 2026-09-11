using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;

namespace NbuildTests
{
    [TestClass]
    public class NbuildAssemblyTests
    {
        [TestMethod]
        public void NbuildAssembly_DoesNotExposeExecutableEntryPoint()
        {
            Assert.IsNull(typeof(Command).Assembly.EntryPoint);
        }
    }
}