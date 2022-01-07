using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Util
{
    public static class SystemUtils
    {
        /// <summary>
        /// Combines an array of strings into a path relative to the application directory
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>The Combined absolute path</returns>
        public static string RelativeFilePath(params string[] paths)
        {
            string[] _paths = new string[paths.Length + 1];

            Array.Copy(paths, 0, _paths, 1, paths.Length);

            _paths[0] = AppContext.BaseDirectory;

            return Path.Combine(_paths);
        }
    }
}
