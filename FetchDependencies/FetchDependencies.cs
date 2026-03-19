using System.IO.Compression;

namespace FetchDependencies;

public enum Region
{
    Global,
    Chinese,
    Korean
}

public class FetchDependencies
{
    private static readonly Dictionary<Region, string> VersionUrls = new()
    {
        [Region.Global] = "https://www.iinact.com/updater/version",
        [Region.Chinese] = "https://cninact.diemoe.net/CN解析/版本.txt",
        [Region.Korean] = "https://iinact.hareen.io/version",
    };

    private static readonly Dictionary<Region, string> PluginUrls = new()
    {
        [Region.Global] = "https://www.iinact.com/updater/download",
        [Region.Chinese] = "https://cninact.diemoe.net/CN解析/FFXIV_ACT_Plugin.dll",
        [Region.Korean] = "https://iinact.hareen.io/download",
    };

    private Version PluginVersion { get; }
    private string DependenciesDir { get; }
    private Region Region { get; }
    private HttpClient HttpClient { get; }

    public FetchDependencies(Version version, string assemblyDir, Region region, HttpClient httpClient)
    {
        PluginVersion = version;
        DependenciesDir = assemblyDir;
        Region = region;
        HttpClient = httpClient;
    }

    public void GetFfxivPlugin()
    {
        var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
        var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");
        var deucalionPath = Path.Combine(DependenciesDir, "deucalion-1.1.0.distrib.dll");

        if (!NeedsUpdate(pluginPath))
            return;

        if (Region == Region.Chinese)
            DownloadFile(PluginUrls[Region], pluginPath);
        else
        {
            if (!File.Exists(pluginZipPath))
                DownloadFile(PluginUrls[Region], pluginZipPath);
            try
            {
                ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
            }
            catch (InvalidDataException)
            {
                File.Delete(pluginZipPath);
                DownloadFile(PluginUrls[Region], pluginZipPath);
                ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
            }
            File.Delete(pluginZipPath);

            foreach (var deucalionDll in Directory.GetFiles(DependenciesDir, "deucalion*.dll"))
                File.Delete(deucalionDll);
        }

        var patcher = new Patcher(PluginVersion, DependenciesDir);
        patcher.MainPlugin();
        patcher.LogFilePlugin();
        patcher.MemoryPlugin();
    }

    private bool NeedsUpdate(string dllPath)
    {
        if (!File.Exists(dllPath)) return true;
        try
        {
            using var plugin = new TargetAssembly(dllPath);

            if (!plugin.ApiVersionMatches())
                return true;

            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var remoteVersionString = HttpClient
                                      .GetStringAsync(VersionUrls[Region],
                                                      cancelAfterDelay.Token).Result;
            var remoteVersion = new Version(remoteVersionString);
            return remoteVersion > plugin.Version;
        }
        catch
        {
            return false;
        }
    }

    private void DownloadFile(string url, string path)
    {
        using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var downloadStream = HttpClient
                                   .GetStreamAsync(url,
                                                   cancelAfterDelay.Token).Result;
        using var zipFileStream = new FileStream(path, FileMode.Create);
        downloadStream.CopyTo(zipFileStream);
        zipFileStream.Close();
    }
}
