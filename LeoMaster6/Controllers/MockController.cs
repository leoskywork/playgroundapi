using System.IO;
using System.Reflection;

namespace LeoMaster6.Controllers
{
    public class MockController : BaseController
    {
        protected string GetMockingDir(string subDir)
        {
           // string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dllDir = System.AppDomain.CurrentDomain.BaseDirectory;

            return string.IsNullOrWhiteSpace(subDir) ? Path.Combine(dllDir, "Mocking") : Path.Combine(dllDir, "Mocking", subDir);
        }
    }
}