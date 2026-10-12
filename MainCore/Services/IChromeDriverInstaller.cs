namespace MainCore.Services
{
    public interface IChromeDriverInstaller
    {
        Task Install();

        /// <summary>Full path of the Chrome binary the driver was matched against.</summary>
        string ChromePath { get; }

        /// <summary>Full version of that Chrome binary, e.g. 142.0.7444.59.</summary>
        Version ChromeVersion { get; }

        /// <summary>Full path of the patched chromedriver binary.</summary>
        string DriverPath { get; }
    }
}
