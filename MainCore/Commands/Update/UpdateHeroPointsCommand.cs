using System.Collections.Concurrent;

namespace MainCore.Commands.Update
{
    [Handler]
    public static partial class UpdateHeroPointsCommand
    {
        public sealed record Command(AccountId AccountId) : IAccountConstraint;

        private static readonly ConcurrentDictionary<AccountId, DateTime> _nextCheck = new();

        public static void Postpone(AccountId accountId, TimeSpan delay)
        {
            _nextCheck[accountId] = DateTime.Now.Add(delay);
        }

        private static async ValueTask HandleAsync(
           Command command,
           IChromeBrowser browser,
           AppDbContext context,
           ITaskManager taskManager
           )
        {
            await Task.CompletedTask;
            var accountId = command.AccountId;
            if (!HeroParser.HasLevelUp(browser.Html)) return;
            if (_nextCheck.TryGetValue(accountId, out var next) && next > DateTime.Now) return;

            var assignHeroPointsTask = new AssignHeroPointsTask.Task(accountId);
            if (!assignHeroPointsTask.CanStart(context) || taskManager.IsExist<AssignHeroPointsTask.Task>(accountId))
            {
                return;
            }

            taskManager.Add(assignHeroPointsTask);
        }
    }
}
