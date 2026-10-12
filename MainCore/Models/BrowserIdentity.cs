namespace MainCore.Models
{
    public enum IdentityPlatform
    {
        Windows,
        MacOS,
        Linux,
    }

    /// <summary>
    /// A desktop Chrome identity whose user agent string, navigator.platform and client hints
    /// (navigator.userAgentData / Sec-CH-UA headers) all agree with each other and with the real Chrome engine version.
    /// </summary>
    public sealed class BrowserIdentity
    {
        public IdentityPlatform Platform { get; }
        public Version ChromeVersion { get; }
        public string UserAgent { get; }

        private BrowserIdentity(IdentityPlatform platform, Version chromeVersion)
        {
            Platform = platform;
            ChromeVersion = chromeVersion;
            UserAgent = BuildUserAgent(platform, chromeVersion.Major);
        }

        /// <summary>
        /// Builds the identity from the user agent stored on an access.
        /// Only the operating system family is kept from it: mobile, tablet or unknown agents become Windows,
        /// the Chrome version is always the installed one, because a stale version is visible to any feature test.
        /// </summary>
        public static BrowserIdentity Create(string? storedUserAgent, Version installedChromeVersion)
        {
            return new BrowserIdentity(DetectPlatform(storedUserAgent), installedChromeVersion);
        }

        public static IdentityPlatform HostPlatform =>
            OperatingSystem.IsWindows() ? IdentityPlatform.Windows
            : OperatingSystem.IsMacOS() ? IdentityPlatform.MacOS
            : IdentityPlatform.Linux;

        /// <summary>
        /// When the identity is the real host OS, Chrome already reports consistent values and nothing needs overriding.
        /// </summary>
        public bool RequiresOverride => Platform != HostPlatform;

        public static IdentityPlatform DetectPlatform(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return IdentityPlatform.Windows;
            if (userAgent.Contains("Android") || userAgent.Contains("iPhone") || userAgent.Contains("iPad") || userAgent.Contains("Mobile"))
                return IdentityPlatform.Windows;
            if (userAgent.Contains("Windows")) return IdentityPlatform.Windows;
            if (userAgent.Contains("Macintosh")) return IdentityPlatform.MacOS;
            if (userAgent.Contains("X11") || userAgent.Contains("Linux") || userAgent.Contains("CrOS")) return IdentityPlatform.Linux;
            return IdentityPlatform.Windows;
        }

        private static string BuildUserAgent(IdentityPlatform platform, int major)
        {
            var os = platform switch
            {
                IdentityPlatform.Windows => "Windows NT 10.0; Win64; x64",
                IdentityPlatform.MacOS => "Macintosh; Intel Mac OS X 10_15_7",
                _ => "X11; Linux x86_64",
            };
            // reduced user agent format, used by every Chrome since 110
            return $"Mozilla/5.0 ({os}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{major}.0.0.0 Safari/537.36";
        }

        public string NavigatorPlatform => Platform switch
        {
            IdentityPlatform.Windows => "Win32",
            IdentityPlatform.MacOS => "MacIntel",
            _ => "Linux x86_64",
        };

        /// <summary>
        /// Wraps the native navigator.platform getter in a Proxy: the descriptor shape, the illegal invocation
        /// TypeError and Function.prototype.toString keep behaving like the native getter.
        /// </summary>
        public string NavigatorPlatformScript => $$"""
            (() => {
              const desc = Object.getOwnPropertyDescriptor(Navigator.prototype, 'platform');
              if (!desc || !desc.get) return;
              const get = new Proxy(desc.get, {
                apply(target, self, args) {
                  Reflect.apply(target, self, args);
                  return '{{NavigatorPlatform}}';
                },
              });
              Object.defineProperty(Navigator.prototype, 'platform', { ...desc, get });
            })();
            """;

        private string HintPlatform => Platform switch
        {
            IdentityPlatform.Windows => "Windows",
            IdentityPlatform.MacOS => "macOS",
            _ => "Linux",
        };

        private string HintPlatformVersion => Platform switch
        {
            IdentityPlatform.Windows => "19.0.0", // Windows 11 24H2
            IdentityPlatform.MacOS => "15.5.0",
            _ => "6.8.0",
        };

        private string HintArchitecture => Platform == IdentityPlatform.MacOS ? "arm" : "x86";

        private static readonly string[] GreasedChars = [" ", "(", ":", "-", ".", "/", ")", ";", "=", "?", "_"];
        private static readonly string[] GreasedVersions = ["8", "99", "24"];

        private static readonly int[][] BrandOrders =
        [
            [0, 1, 2],
            [0, 2, 1],
            [1, 0, 2],
            [1, 2, 0],
            [2, 0, 1],
            [2, 1, 0],
        ];

        /// <summary>
        /// Brand list in the exact order and GREASE form real Chrome generates for this major version
        /// (components/embedder_support/user_agent_utils.cc), e.g. 133 gives "Not(A:Brand";v="99", "Google Chrome";v="133", "Chromium";v="133".
        /// </summary>
        public IReadOnlyList<(string Brand, string Version)> GetBrands(bool fullVersion)
        {
            var seed = ChromeVersion.Major;
            var greasedBrand = $"Not{GreasedChars[seed % GreasedChars.Length]}A{GreasedChars[(seed + 1) % GreasedChars.Length]}Brand";
            var greasedVersion = GreasedVersions[seed % GreasedVersions.Length];
            var chromeVersion = fullVersion ? ChromeVersion.ToString() : seed.ToString();
            if (fullVersion) greasedVersion = $"{greasedVersion}.0.0.0";

            var order = BrandOrders[seed % BrandOrders.Length];
            var brands = new (string, string)[3];
            brands[order[0]] = (greasedBrand, greasedVersion);
            brands[order[1]] = ("Chromium", chromeVersion);
            brands[order[2]] = ("Google Chrome", chromeVersion);
            return brands;
        }

        /// <summary>
        /// Parameters for the CDP command Emulation.setUserAgentOverride.
        /// </summary>
        public Dictionary<string, object> ToCdpParameters()
        {
            static List<object> ToList(IEnumerable<(string Brand, string Version)> brands) =>
                brands.Select(x => (object)new Dictionary<string, object> { ["brand"] = x.Brand, ["version"] = x.Version }).ToList();

            return new Dictionary<string, object>
            {
                ["userAgent"] = UserAgent,
                ["platform"] = NavigatorPlatform,
                ["userAgentMetadata"] = new Dictionary<string, object>
                {
                    ["brands"] = ToList(GetBrands(false)),
                    ["fullVersionList"] = ToList(GetBrands(true)),
                    ["fullVersion"] = ChromeVersion.ToString(),
                    ["platform"] = HintPlatform,
                    ["platformVersion"] = HintPlatformVersion,
                    ["architecture"] = HintArchitecture,
                    ["model"] = "",
                    ["mobile"] = false,
                    ["bitness"] = "64",
                    ["wow64"] = false,
                    ["formFactors"] = new List<object> { "Desktop" },
                },
            };
        }

        public override string ToString() => $"{Platform} / {UserAgent}";
    }
}
