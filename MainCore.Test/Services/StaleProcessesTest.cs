using MainCore.Services;
using System.Diagnostics;

namespace MainCore.Test.Services
{
    public class StaleProcessesTest
    {
        [Fact]
        public void KillChromeUsingProfile_ShouldOnlyKillMatchingProfile()
        {
            if (!OperatingSystem.IsLinux()) return;

            var profile = $"/tmp/tbs-test-{Guid.NewGuid():N}";
            // a stand-in process whose command line carries the same argument chrome would
            using var target = Process.Start("/bin/sh", ["-c", "sleep 300", "sh", $"--user-data-dir={profile}"]);
            using var other = Process.Start("/bin/sh", ["-c", "sleep 300", "sh", $"--user-data-dir={profile}-other"]);
            try
            {
                Thread.Sleep(200);
                StaleProcesses.KillChromeUsingProfile(profile).ShouldBe(1);
                target.HasExited.ShouldBeTrue();
                other.HasExited.ShouldBeFalse();
            }
            finally
            {
                if (!other.HasExited) other.Kill();
                if (!target.HasExited) target.Kill();
            }
        }
    }
}
