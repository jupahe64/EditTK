using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using static EditTK.ResourceManager;

namespace EditTK
{
    public static class ResourceManager
    {
        public delegate bool FileUpdateAction(byte[] bytes, out string? errorString);
        public delegate void ResourceReloadingErrorHandler(string resourcePath, string errorString);

        public class Token { }

        private static string s_exeFolder = AppContext.BaseDirectory;
        private static readonly Dictionary<Assembly, string> s_assemblyNameLookup = new();

        private static readonly Dictionary<Token, (string filePath, DateTime lastLoadTime, FileUpdateAction action)> s_trackedFiles = new();
        private static Timer? s_reloadTimer;
        private static ResourceReloadingErrorHandler? s_resourceReloadingErrorHandler;
        private static bool s_isLogErrorToConsole = true;

        private static void ReloadTimerCallback(object? state)
        {
            foreach (var (token, (path, lastLoadTime, callback)) in s_trackedFiles)
            {
                var lastWriteTime = File.GetLastWriteTime(path);
                
                if (lastWriteTime > lastLoadTime)
                {
                    //make sure we don't trigger a dictionary update while iterating
                    CollectionsMarshal.GetValueRefOrNullRef(s_trackedFiles, token).lastLoadTime = lastWriteTime;

                    if(!callback.Invoke(File.ReadAllBytes(path), out string? errorString))
                    {
                        if(s_isLogErrorToConsole)
                            Console.WriteLine($"Reloading {path} failed:\n{errorString}\n");

                        if(s_resourceReloadingErrorHandler == null)
                        {
                            Console.WriteLine("""
                                ┌─────────────────────────────────────────────────────┐
                                │To display these errors yourself call                │
                                │ResourceManager.RegisterResourceReloadingErrorHandler│
                                └─────────────────────────────────────────────────────┘
                                """);

                            continue;
                        }
                        
                        s_resourceReloadingErrorHandler(path, errorString ?? "");

                    }
                }
            }
        }

        public static void RegisterResourceReloadingErrorHandler(ResourceReloadingErrorHandler action, 
            bool disableConsoleLogging = true)
        {
            s_resourceReloadingErrorHandler = action;

            if(disableConsoleLogging)
                s_isLogErrorToConsole = false;
        }

        public static Stream? GetFileFromExeFolder(params string[] filePath)
        {
            return File.OpenRead(Path.Combine(s_exeFolder, Path.Combine(filePath)));
        }

        public static Stream? GetFileFromExeFolder(string[] filePath, FileUpdateAction fileUpdateAction, out Token trackedFileToken)
        {
            string absolutePath = Path.Combine(s_exeFolder, Path.Combine(filePath));

            s_reloadTimer ??= new Timer(ReloadTimerCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            trackedFileToken = new Token();

            var lastWriteTime = File.GetLastWriteTime(absolutePath);
            s_trackedFiles[trackedFileToken] = (absolutePath, lastWriteTime, fileUpdateAction);

            return File.OpenRead(absolutePath);
        }

        public static void UntrackFile(Token trackedFileToken)
        {
            s_trackedFiles.Remove(trackedFileToken);

            if(s_trackedFiles.Count == 0)
                s_reloadTimer?.Dispose();
        }

        public static Stream? GetFileFromEmbeddedResources(Assembly assembly, params string[] filePath) 
        {
            if (!s_assemblyNameLookup.TryGetValue(assembly, out var name))
                s_assemblyNameLookup[assembly] = name!;

            return assembly.GetManifestResourceStream($"{name}.{string.Join('.', filePath)}");
        }
    }
}
