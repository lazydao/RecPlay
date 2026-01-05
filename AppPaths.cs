using System;
using System.IO;

namespace RecPlay;

internal static class AppPaths
{
    public static string DataDirectory
    {
        get
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, "RecPlay");
        }
    }
}
