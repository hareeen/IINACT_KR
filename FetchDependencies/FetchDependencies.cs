using System.IO.Compression;
using System.Text.Json.Nodes;

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
        
        if (!NeedsUpdate(pluginPath))
            return;
        
        if (!File.Exists(pluginZipPath))
        {
            DownloadPlugin(pluginZipPath);
        }

        try
        {
            ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
        }
        catch (InvalidDataException)
        {
            File.Delete(pluginZipPath);
            DownloadPlugin(pluginZipPath);
            ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
        }
        File.Delete(pluginZipPath);

        foreach (var deucalionDll in Directory.GetFiles(DependenciesDir, "deucalion*.dll"))
            File.Delete(deucalionDll);

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

    private void DownloadPlugin(string pluginZipPath)
    {
        try
        {
            DownloadFile(PluginUrls[Region], pluginZipPath);
        }
        catch
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/ravahn/FFXIV_ACT_Plugin/releases/latest");
            request.Headers.UserAgent.ParseAdd("IINACT/1.0");
            using var response = HttpClient.Send(request);
            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStream();
            var json = JsonNode.Parse(stream);
            var downloadUrl = json?["assets"]?[0]?["browser_download_url"]?.ToString();

            if (string.IsNullOrEmpty(downloadUrl))
                throw new Exception("Could not find fallback download URL from GitHub API.");

            DownloadFile(downloadUrl, pluginZipPath);
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
