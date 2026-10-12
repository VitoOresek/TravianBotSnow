using Microsoft.Win32;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

#nullable disable

namespace MainCore.Services
{
    [RegisterSingleton<IChromeDriverInstaller, ChromeDriverInstaller>]
    public sealed partial class ChromeDriverInstaller(ILogger logger) : IChromeDriverInstaller
    {
        private readonly ILogger _logger = logger.ForContext<ChromeDriverInstaller>();
        private readonly HttpClient httpClient = new();

        private const string apiEndpoint = "https://googlechromelabs.github.io/chrome-for-testing/known-good-versions-with-downloads.json";

        /// <summary>Environment variable that overrides Chrome auto detection.</summary>
        public const string ChromePathVariable = "TBS_CHROME_PATH";

        public string ChromePath { get; private set; }
        public Version ChromeVersion { get; private set; }
        public string DriverPath => GetDriverPath();

        public async Task Install()
        {
            ChromePath = FindChrome();
            ChromeVersion = await GetChromeVersion(ChromePath);
            _logger.Information("Found Chrome {Version} at {Path}", ChromeVersion, ChromePath);

            if (ChromeVersion.Major <= 114)
            {
                throw new Exception($"Your chrome version is {ChromeVersion}. Please update your chrome first");
            }

            var chromeDriverVersion = await GetCurrentDriverVersion();
            if (chromeDriverVersion.Major != ChromeVersion.Major)
            {
                _logger.Information("Chromedriver {DriverVersion} does not match Chrome {ChromeVersion}, downloading", chromeDriverVersion, ChromeVersion);
                await Download(ChromeVersion);
            }

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(DriverPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            // always verify, an unpatched driver leaks the cdc_ marker to every page
            if (PatchDriver(DriverPath))
            {
                _logger.Information("Patched chromedriver to remove the cdc_ automation marker");
            }
        }

        private async Task Download(Version chromeVersion)
        {
            var asset = await httpClient.GetFromJsonAsync<CftAsset>(apiEndpoint);
            if (asset is null)
            {
                throw new Exception("Failed to get asset from chrome API");
            }

            var candidates = asset.Versions
                .Where(x => x.Version.Major == chromeVersion.Major)
                .Where(x => x.Downloads.ChromeDriver is not null)
                .OrderByDescending(x => x.Version)
                .ToList();

            // prefer the exact build, otherwise the newest driver of the same major version
            var version = candidates.Find(x => x.Version == chromeVersion) ?? candidates.FirstOrDefault();
            if (version is null)
            {
                throw new Exception($"ChromeDriver version not found for Chrome version {chromeVersion}");
            }

            var platform = GetPlatformName();
            var link = version.Downloads.ChromeDriver.Find(x => x.Platform == platform);
            if (link is null)
            {
                throw new Exception($"ChromeDriver version not found for Chrome version {chromeVersion} [no {platform} download link]");
            }

            var driverZipResponse = await httpClient.GetAsync(link.Url);
            if (!driverZipResponse.IsSuccessStatusCode)
            {
                throw new Exception($"ChromeDriver download request failed with status code: {driverZipResponse.StatusCode}, reason phrase: {driverZipResponse.ReasonPhrase}");
            }

            var driverName = GetDriverName();
            var targetPath = GetDriverPath();
            var tempPath = $"{targetPath}.download";

            using (var zipFileStream = await driverZipResponse.Content.ReadAsStreamAsync())
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read))
            {
                // exact match: the linux archive also holds LICENSE.chromedriver and THIRD_PARTY_NOTICES.chromedriver
                var entry = zipArchive.Entries.First(x => x.Name == driverName);
                using var chromeDriverStream = entry.Open();
                using var chromeDriverWriter = new FileStream(tempPath, FileMode.Create);
                await chromeDriverStream.CopyToAsync(chromeDriverWriter);
            }

            File.Move(tempPath, targetPath, true);
            _logger.Information("Downloaded chromedriver {Version} ({Platform})", version.Version, platform);
        }

        [GeneratedRegex(@"\{window\.cdc.*?;\}")]
        private static partial Regex CdcRegex();

        /// <summary>
        /// Replaces the injected <c>{window.cdc_...;}</c> block with a same-length dummy, the technique used by undetected-chromedriver.
        /// Works on PE and ELF binaries alike because the byte length never changes.
        /// </summary>
        /// <returns>true if the binary was modified</returns>
        public static bool PatchDriver(string driverPath)
        {
            var latin1 = Encoding.Latin1;
            var content = latin1.GetString(File.ReadAllBytes(driverPath));

            if (!CdcRegex().IsMatch(content)) return false;

            var patched = CdcRegex().Replace(content, match => "{tbs}".PadRight(match.Length, ' '));
            File.WriteAllBytes(driverPath, latin1.GetBytes(patched));
            return true;
        }

        public static bool IsDriverPatched(string driverPath)
        {
            var content = Encoding.Latin1.GetString(File.ReadAllBytes(driverPath));
            return !CdcRegex().IsMatch(content);
        }

        [GeneratedRegex(@"(\d+)\.(\d+)\.(\d+)\.(\d+)")]
        private static partial Regex VersionRegex();

        public static Version ParseVersion(string output)
        {
            var match = VersionRegex().Match(output ?? "");
            if (!match.Success) throw new FormatException($"Cannot find a version number in '{output?.Trim()}'");
            return new Version(match.Value);
        }

        private async Task<Version> GetCurrentDriverVersion()
        {
            var targetPath = GetDriverPath();
            if (!File.Exists(targetPath)) return new Version(0, 0, 0, 0);

            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(targetPath, File.GetUnixFileMode(targetPath) | UnixFileMode.UserExecute);
                }
                // expected output is something like "ChromeDriver 88.0.4324.96 (68dba2d8a0b149a1d3afac56fa74648032bcf46b-refs/branch-heads/4324@{#1784})"
                var output = await RunAndRead(targetPath, "--version");
                return ParseVersion(output);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Existing chromedriver is not usable, it will be replaced");
                return new Version(0, 0, 0, 0);
            }
        }

        private static async Task<string> RunAndRead(string fileName, string argument)
        {
            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = fileName,
                    ArgumentList = { argument },
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            );
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }

        private static string GetPlatformName()
        {
            if (OperatingSystem.IsWindows()) return "win64";
            if (OperatingSystem.IsLinux())
            {
                return RuntimeInformation.OSArchitecture switch
                {
                    Architecture.X64 => "linux64",
                    Architecture.Arm64 => "linux-arm64",
                    _ => throw new PlatformNotSupportedException($"Google does not publish chromedriver for linux {RuntimeInformation.OSArchitecture}."),
                };
            }
            if (OperatingSystem.IsMacOS()) return RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "mac-arm64" : "mac-x64";
            throw new PlatformNotSupportedException("Your operating system is not supported.");
        }

        private static string GetDriverName()
        {
            return OperatingSystem.IsWindows() ? "chromedriver.exe" : "chromedriver";
        }

        private static string GetDriverPath()
        {
            var driverName = GetDriverName();
            string targetPath = Path.GetDirectoryName(AppContext.BaseDirectory);
            targetPath = Path.Combine(targetPath, driverName);
            return targetPath;
        }

        private static readonly string[] LinuxChromeCommands =
        [
            "google-chrome-stable",
            "google-chrome",
        ];

        private static readonly string[] LinuxChromePaths =
        [
            "/opt/google/chrome/chrome",
            "/usr/bin/google-chrome-stable",
            "/usr/bin/google-chrome",
        ];

        public static string FindChrome()
        {
            var custom = Environment.GetEnvironmentVariable(ChromePathVariable);
            if (!string.IsNullOrWhiteSpace(custom))
            {
                if (!File.Exists(custom)) throw new FileNotFoundException($"{ChromePathVariable} points to a missing file", custom);
                return custom;
            }

            if (OperatingSystem.IsWindows())
            {
                var chromePath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\chrome.exe", null, null) as string;
                if (chromePath is null || !File.Exists(chromePath))
                {
                    throw new Exception("Google Chrome not found in registry");
                }
                return chromePath;
            }

            if (OperatingSystem.IsLinux())
            {
                foreach (var command in LinuxChromeCommands)
                {
                    var path = FindInPath(command);
                    if (path is not null) return path;
                }

                foreach (var path in LinuxChromePaths)
                {
                    if (File.Exists(path)) return path;
                }

                throw new Exception($"Google Chrome not found. Install it with scripts/install-deps.sh or set {ChromePathVariable}");
            }

            if (OperatingSystem.IsMacOS())
            {
                const string macPath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
                if (File.Exists(macPath)) return macPath;
                throw new Exception($"Google Chrome not found. Set {ChromePathVariable}");
            }

            throw new PlatformNotSupportedException("Your operating system is not supported.");
        }

        private static string FindInPath(string command)
        {
            var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = Path.Combine(directory, command);
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        private static async Task<Version> GetChromeVersion(string chromePath)
        {
            if (OperatingSystem.IsWindows())
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(chromePath);
                return new Version(fileVersionInfo.FileVersion);
            }

            try
            {
                // "Google Chrome 142.0.7444.59 " on linux and macOS
                var output = await RunAndRead(chromePath, "--version");
                return ParseVersion(output);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred trying to execute '{chromePath} --version'", ex);
            }
        }
    }

    public class CftAsset
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("versions")]
        public List<CftVersion> Versions { get; set; }
    }

    public class CftVersion
    {
        [JsonPropertyName("version")]
        public Version Version { get; set; }

        [JsonPropertyName("revision")]
        public int Revision { get; set; }

        [JsonPropertyName("downloads")]
        public CftDownloads Downloads { get; set; }
    }

    public class CftDownloads
    {
        [JsonPropertyName("chrome")]
        public List<CftLink> Chrome { get; set; }

        [JsonPropertyName("chromedriver")]
        public List<CftLink> ChromeDriver { get; set; }
    }

    public class CftLink
    {
        [JsonPropertyName("platform")]
        public string Platform { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
