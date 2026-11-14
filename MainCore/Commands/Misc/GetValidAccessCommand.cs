using System.Net;

namespace MainCore.Commands.Misc
{
    [Handler]
    public static partial class GetValidAccessCommand
    {
        /// <param name="ReuseLastAccess">
        /// Prefer the most recently used access (keep the same proxy) instead of rotating to the oldest one.
        /// Used for manual logins so the connection doesn't change between them.
        /// </param>
        public sealed record Command(AccountId AccountId, bool IgnoreSleepTime = false, bool ReuseLastAccess = false) : IAccountCommand;

        private static async ValueTask<Result<AccessDto>> HandleAsync(
            Command command,
            ILogger logger,
            AppDbContext context
            )
        {
            var (accountId, ignoreSleepTime, reuseLastAccess) = command;

            var query = context.Accesses
               .Where(x => x.AccountId == accountId.Value);

            var accesses = (reuseLastAccess
                    ? query.OrderByDescending(x => x.LastUsed) // get newest one
                    : query.OrderBy(x => x.LastUsed)) // get oldest one
               .ToDto()
               .ToList();

            async Task<AccessDto?> GetValidAccess(List<AccessDto> proxies)
            {
                foreach (var proxy in proxies)
                {
                    var client = GetHttpClient(proxy);
                    logger.Information("Checking proxy {Proxy}, last used {LastUsed}", proxy.Proxy, proxy.LastUsed);
                    try
                    {
                        var response = await client.GetAsync(TRAVIAN_PAGE);
                        if (response.IsSuccessStatusCode)
                        {
                            logger.Information("Access {Proxy} is good", proxy.Proxy);
                            return proxy;
                        }

                        logger.Warning("Access {Proxy} is not working, status code: {StatusCode}", proxy.Proxy, response.StatusCode);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "{Message}", ex.Message);
                    }
                }
                return null;
            }

            var access = await GetValidAccess(accesses);
            if (access is null) return Stop.Error.WithError("All accesses not working");

            if (accesses.Count == 1) return access;
            if (ignoreSleepTime || reuseLastAccess) return access;

            var minSleep = context.ByName(accountId, AccountSettingEnums.SleepTimeMin);
            var timeValid = DateTime.Now.AddMinutes(-minSleep);
            if (access.LastUsed > timeValid) return Stop.Error.WithError("Last access is reused, it may get MH's attention");
            return access;
        }

        private static readonly NetworkCredential _networkCredential = new();

        private static readonly WebProxy _proxyWithAuth = new()
        {
            Credentials = _networkCredential,
        };

        private static readonly WebProxy _proxyWithoutAuth = new();

        private static readonly HttpClient _proxyWithoutAuthHttpClient = new(new HttpClientHandler()
        {
            Proxy = _proxyWithoutAuth,
            UseProxy = true,
        });

        private static readonly HttpClient _proxyWithAuthHttpClient = new(new HttpClientHandler()
        {
            Proxy = _proxyWithAuth,
            UseProxy = true,
        });

        private static readonly HttpClient _defaultHttpClient = new(new HttpClientHandler()
        {
            UseProxy = false,
        });

        private const string TRAVIAN_PAGE = "https://www.travian.com/international";

        private static HttpClient GetHttpClient(AccessDto access)
        {
            if (string.IsNullOrEmpty(access.ProxyHost)) return _defaultHttpClient;

            if (string.IsNullOrEmpty(access.ProxyUsername))
            {
                _proxyWithoutAuth.Address = new Uri($"http://{access.ProxyHost}:{access.ProxyPort}");
                return _proxyWithoutAuthHttpClient;
            }

            _networkCredential.UserName = access.ProxyUsername;
            _networkCredential.Password = access.ProxyPassword;
            _proxyWithAuth.Address = new Uri($"http://{access.ProxyHost}:{access.ProxyPort}");
            return _proxyWithAuthHttpClient;
        }
    }
}