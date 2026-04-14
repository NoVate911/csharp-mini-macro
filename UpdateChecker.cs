using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniMacro
{
    internal sealed class UpdateResult
    {
        public bool   IsUpdateAvailable { get; }
        public string LatestVersion     { get; }
        public string ReleaseUrl        { get; }
        public bool   CheckFailed       { get; }

        public UpdateResult(bool isUpdateAvailable, string latestVersion,
                            string releaseUrl, bool checkFailed)
        {
            IsUpdateAvailable = isUpdateAvailable;
            LatestVersion     = latestVersion;
            ReleaseUrl        = releaseUrl;
            CheckFailed       = checkFailed;
        }
    }

    internal static class UpdateChecker
    {
        private const string ApiUrl      = "https://api.github.com/repos/NoVate911/csharp-mini-macro/releases/latest";
        private const string ReleasesUrl = "https://github.com/NoVate911/csharp-mini-macro/releases/latest";

        public static async Task<UpdateResult> CheckAsync()
        {
            try
            {
#if LEGACY
                // net48 по умолчанию может использовать TLS 1.0 — явно указываем 1.2
                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.SecurityProtocolType.Tls12;
#endif
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("MiniMacro/updater");

                var json = await client.GetStringAsync(ApiUrl);

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("tag_name", out var tagEl))
                    return new UpdateResult(false, "", ReleasesUrl, checkFailed: false);

                var tag = tagEl.GetString() ?? "";
                var latestStr = tag.TrimStart('v');

                if (!Version.TryParse(latestStr, out var latestVersion))
                    return new UpdateResult(false, "", ReleasesUrl, checkFailed: false);

                var current = Assembly.GetExecutingAssembly().GetName().Version
                              ?? new Version(0, 0, 0);

                var currentShort = new Version(current.Major, current.Minor, current.Build);
                bool isOlder = currentShort < latestVersion;

                return new UpdateResult(isOlder, tag, ReleasesUrl, checkFailed: false);
            }
            catch
            {
                return new UpdateResult(false, "", ReleasesUrl, checkFailed: true);
            }
        }
    }
}
