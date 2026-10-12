using System.Diagnostics;

namespace MainCore.Services
{
    /// <summary>
    /// On Linux, chrome and chromedriver keep running when the bot dies (crash, kill, power loss of the X session).
    /// A leftover chrome keeps its profile locked and every later login fails, so they are cleaned up first.
    /// </summary>
    public static class StaleProcesses
    {
        /// <returns>how many processes were killed</returns>
        public static int Kill(Func<string[], bool> match)
        {
            if (!OperatingSystem.IsLinux()) return 0;

            var killed = 0;
            foreach (var directory in Directory.EnumerateDirectories("/proc"))
            {
                if (!int.TryParse(Path.GetFileName(directory), out var pid) || pid == Environment.ProcessId) continue;

                string[] arguments;
                try
                {
                    arguments = File.ReadAllText(Path.Combine(directory, "cmdline")).Split('\0', StringSplitOptions.RemoveEmptyEntries);
                }
                catch
                {
                    continue; // process exited or belongs to another user
                }

                if (arguments.Length == 0 || !match(arguments)) continue;

                try
                {
                    using var process = Process.GetProcessById(pid);
                    process.Kill();
                    process.WaitForExit(5000);
                    killed++;
                }
                catch
                {
                    // already gone
                }
            }
            return killed;
        }

        public static int KillChromeUsingProfile(string userDataDir)
        {
            var argument = $"--user-data-dir={userDataDir}";
            return Kill(arguments => arguments.Any(x => x == argument));
        }

        public static int KillDriver(string driverPath)
        {
            return Kill(arguments => arguments[0] == driverPath);
        }
    }
}
