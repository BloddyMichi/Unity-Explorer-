#if NET6_0_OR_GREATER
using System.IO.Compression;
#endif

using UnityExplorer.Runtime;

namespace UnityExplorer;

internal static class DataCenterDiagnostics
{
    public static string GameDirectory
    {
        get
        {
            string modsDirectory = ExplorerCore.Loader?.ExplorerFolderDestination;
            if (string.IsNullOrEmpty(modsDirectory))
                return "<unknown>";

            DirectoryInfo parent = Directory.GetParent(modsDirectory);
            return parent?.FullName ?? "<unknown>";
        }
    }

    public static string SupportPackageDirectory =>
        Path.Combine(ExplorerCore.ExplorerFolder, "SupportPackages");

    public static string GetStatusText()
    {
        string assemblyPath = typeof(ExplorerCore).Assembly.Location;
        string loader = ExplorerCore.Loader?.GetType().Name ?? "<unknown>";

        return
            "Data Center runtime status\n" +
            $"Unity: {Application.unityVersion}\n" +
            $"UnityExplorer: {ExplorerCore.VERSION}\n" +
            $"Loader: {loader}\n" +
            $"{DataCenterCompatibility.GetSummary()}\n" +
            $"GameDir: {GameDirectory}\n" +
            $"ExplorerFolder: {ExplorerCore.ExplorerFolder}\n" +
            $"Assembly: {assemblyPath}";
    }

    public static string CreateSupportPackage()
    {
#if NET6_0_OR_GREATER
        Directory.CreateDirectory(SupportPackageDirectory);

        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string stagingRoot = Path.Combine(SupportPackageDirectory, "DataCenter-UnityExplorer-Ingame-" + timestamp);
        string zipPath = stagingRoot + ".zip";

        if (Directory.Exists(stagingRoot))
            Directory.Delete(stagingRoot, true);

        Directory.CreateDirectory(stagingRoot);

        string gameDir = GameDirectory;
        CopyIfExists(Path.Combine(gameDir, "MelonLoader", "Latest.log"), Path.Combine(stagingRoot, "MelonLoader"));

        string logDir = Path.Combine(ExplorerCore.ExplorerFolder, "Logs");
        if (Directory.Exists(logDir))
        {
            string targetLogDir = Path.Combine(stagingRoot, "UnityExplorerLogs");
            Directory.CreateDirectory(targetLogDir);

            foreach (FileInfo log in new DirectoryInfo(logDir)
                .GetFiles("*.txt")
                .OrderByDescending(it => it.LastWriteTimeUtc)
                .Take(10))
            {
                File.Copy(log.FullName, Path.Combine(targetLogDir, log.Name), true);
            }
        }

        File.WriteAllText(Path.Combine(stagingRoot, "support-info.txt"), GetStatusText());

        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ZipFile.CreateFromDirectory(stagingRoot, zipPath);
        Directory.Delete(stagingRoot, true);

        ExplorerCore.Log("Created Data Center support package: " + zipPath);
        return zipPath;
#else
        string message = "Ingame support package creation requires the net6/CoreCLR build.";
        ExplorerCore.LogWarning(message);
        return message;
#endif
    }

    public static void OpenLogs()
    {
        OpenPath(Path.Combine(GameDirectory, "MelonLoader", "Latest.log"));
        OpenPath(Path.Combine(ExplorerCore.ExplorerFolder, "Logs"));
    }

    public static void OpenSupportPackages()
    {
        Directory.CreateDirectory(SupportPackageDirectory);
        OpenPath(SupportPackageDirectory);
    }

    private static void CopyIfExists(string source, string destinationDir)
    {
        if (!File.Exists(source))
            return;

        Directory.CreateDirectory(destinationDir);
        File.Copy(source, Path.Combine(destinationDir, Path.GetFileName(source)), true);
    }

    private static void OpenPath(string path)
    {
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                ExplorerCore.LogWarning("Path not found: " + path);
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ExplorerCore.LogWarning("Failed to open path '" + path + "': " + ex.Message);
        }
    }
}
