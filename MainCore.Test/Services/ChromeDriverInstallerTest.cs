using MainCore.Services;
using System.Text;

namespace MainCore.Test.Services
{
    public class ChromeDriverInstallerTest
    {
        [Theory]
        [InlineData("Google Chrome 142.0.7444.59 \n", "142.0.7444.59")]
        [InlineData("ChromeDriver 142.0.7444.59 (4b0d4a3e0e4b3a3e-refs/branch-heads/7444@{#1})", "142.0.7444.59")]
        public void ParseVersion_ShouldReadChromeAndDriverOutput(string output, string expected)
        {
            ChromeDriverInstaller.ParseVersion(output).ShouldBe(new Version(expected));
        }

        [Fact]
        public void PatchDriver_ShouldRemoveCdcMarkerAndKeepLength()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = "\u007fELF\0\0binary{window.cdc_adoQpoasnfa76pfcZLmcfl_Array = window.Array;}tail\0";
                File.WriteAllBytes(path, Encoding.Latin1.GetBytes(original));

                ChromeDriverInstaller.IsDriverPatched(path).ShouldBeFalse();
                ChromeDriverInstaller.PatchDriver(path).ShouldBeTrue();

                var patched = Encoding.Latin1.GetString(File.ReadAllBytes(path));
                patched.Length.ShouldBe(original.Length);
                patched.ShouldNotContain("cdc_");
                patched.ShouldContain("{tbs}");
                patched.ShouldEndWith("tail\0");

                ChromeDriverInstaller.IsDriverPatched(path).ShouldBeTrue();
                ChromeDriverInstaller.PatchDriver(path).ShouldBeFalse();
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
