using MainCore.Models;

namespace MainCore.Test.Services
{
    public class BrowserIdentityTest
    {
        [Theory]
        [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", IdentityPlatform.Windows)]
        [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", IdentityPlatform.MacOS)]
        [InlineData("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", IdentityPlatform.Linux)]
        [InlineData("Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Mobile Safari/537.36", IdentityPlatform.Windows)]
        [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1", IdentityPlatform.Windows)]
        [InlineData("", IdentityPlatform.Windows)]
        [InlineData(null, IdentityPlatform.Windows)]
        public void DetectPlatform_ShouldMapToDesktopFamily(string? userAgent, IdentityPlatform expected)
        {
            BrowserIdentity.DetectPlatform(userAgent).ShouldBe(expected);
        }

        [Fact]
        public void UserAgent_ShouldUseInstalledChromeVersion()
        {
            var identity = BrowserIdentity.Create("Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36", new Version(142, 0, 7444, 59));
            identity.UserAgent.ShouldBe("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36");
            identity.NavigatorPlatform.ShouldBe("Win32");
        }

        // expected values are the Sec-CH-UA headers real Google Chrome sends for each major version
        [Theory]
        [InlineData(120, "Not_A Brand;8|Chromium;120|Google Chrome;120")]
        [InlineData(124, "Chromium;124|Google Chrome;124|Not-A.Brand;99")]
        [InlineData(131, "Google Chrome;131|Chromium;131|Not_A Brand;24")]
        [InlineData(133, "Not(A:Brand;99|Google Chrome;133|Chromium;133")]
        public void GetBrands_ShouldMatchRealChrome(int major, string expected)
        {
            var identity = BrowserIdentity.Create("", new Version(major, 0, 1000, 1));
            var brands = string.Join("|", identity.GetBrands(false).Select(x => $"{x.Brand};{x.Version}"));
            brands.ShouldBe(expected);
        }

        [Fact]
        public void GetBrands_FullVersion_ShouldUseFullChromeVersion()
        {
            var identity = BrowserIdentity.Create("", new Version(133, 0, 6943, 98));
            var brands = identity.GetBrands(true);
            brands.ShouldContain(("Google Chrome", "133.0.6943.98"));
            brands.ShouldContain(("Not(A:Brand", "99.0.0.0"));
        }

        [Fact]
        public void ToCdpParameters_ShouldDescribeWindowsDesktop()
        {
            var identity = BrowserIdentity.Create("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", new Version(142, 0, 7444, 59));
            var parameters = identity.ToCdpParameters();

            parameters["platform"].ShouldBe("Win32");
            var metadata = parameters["userAgentMetadata"].ShouldBeOfType<Dictionary<string, object>>();
            metadata["platform"].ShouldBe("Windows");
            metadata["mobile"].ShouldBe(false);
            metadata["fullVersion"].ShouldBe("142.0.7444.59");
        }
    }
}
