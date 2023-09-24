using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EditTK
{
    public static class ResourceManager
    {
        private static string s_exeFolder = AppContext.BaseDirectory;
        private static Dictionary<Assembly, string> s_assemblyNameLookup = new();

        public static Stream? GetFileFromExeFolder(params string[] filePath)
        {
            return File.OpenRead(Path.Combine(s_exeFolder, Path.Combine(filePath)));
        }

        public static Stream? GetFileFromEmbeddedResources(Assembly assembly, params string[] filePath) 
        {
            if (!s_assemblyNameLookup.TryGetValue(assembly, out var name))
                s_assemblyNameLookup[assembly] = name!;

            return assembly.GetManifestResourceStream($"{name}.{string.Join('.', filePath)}");
        }
    }
}
