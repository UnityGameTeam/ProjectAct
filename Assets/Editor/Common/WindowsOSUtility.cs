using UnityEngine;
using System.Diagnostics;
using System.IO;

public static class WindowsOSUtility
{
    public static void ExploreFile(string path)
    {
        if (Directory.Exists(path))
        {
            System.Diagnostics.Process.Start(Path.GetFullPath(path));
        }
        else if (File.Exists(path))
        {
            Process open = new Process();
            open.StartInfo.FileName = "explorer";
            open.StartInfo.Arguments = @"/select," + path;
            open.Start();
        }
    }
}
