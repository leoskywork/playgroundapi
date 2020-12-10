using System;
using System.IO;
using System.Linq;

namespace LeoMaster6.Controllers
{
    public class MockController : BaseController
    {
        protected static string GetMockingDir(string subDir)
        {
            // string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dllDir = System.AppDomain.CurrentDomain.BaseDirectory;

            return string.IsNullOrWhiteSpace(subDir) ? Path.Combine(dllDir, "Mocking") : Path.Combine(dllDir, "Mocking", subDir);
        }

        protected static string GetMockingPath(string subFolder, int id)
        {
            string dir = GetMockingDir(subFolder);

            string path = Directory.GetFiles(dir).FirstOrDefault(f => f.Replace('/', '@').Replace('\\', '@').Contains("@YF@" + id + "-"));

            return path;
        }

        protected static string ReadMockingFile(string subFolder, int id)
        {
            string path = GetMockingPath(subFolder, id);

            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("no mocking file for " + id);

            return File.ReadAllText(path);
        }
    }

    public static class JsonExtension
    {
        //public static T As<T>(this object obj) where T : class
        //{
        //    return obj as T;
        //}

        public static Newtonsoft.Json.Linq.JObject AsJObject(this object obj)
        {
            return obj as Newtonsoft.Json.Linq.JObject;
        }

        //public static int ToInt(this object obj)
        //{
        //    return (int)obj;
        //}
    }
}