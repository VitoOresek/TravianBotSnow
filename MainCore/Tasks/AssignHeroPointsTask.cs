using MainCore.Commands.Features.AssignHeroPoints;
using MainCore.Tasks.Base;

namespace MainCore.Tasks
{
    [Handler]
    public static partial class AssignHeroPointsTask
    {
        public sealed class Task : AccountTask
        {
            public Task(AccountId accountId) : base(accountId)
            {
            }

            protected override string TaskName => "Assign hero points";

            public override bool CanStart(AppDbContext context)
            {
                var settingEnable = context.BooleanByName(AccountId, AccountSettingEnums.EnableAutoAssignHeroPoints);
                if (!settingEnable) return false;

                return true;
            }
        }

        private static async ValueTask<Result> HandleAsync(
            Task task,
            ToHeroAttributesPageCommand.Handler toHeroAttributesPageCommand,
            AssignHeroPointsCommand.Handler assignHeroPointsCommand,
            CancellationToken cancellationToken)
        {
            // the level up badge can outlive a successful save for a while, do not come back right away
            UpdateHeroPointsCommand.Postpone(task.AccountId, TimeSpan.FromMinutes(10));

            Result result;
            result = await toHeroAttributesPageCommand.HandleAsync(new(), cancellationToken);
            if (result.IsFailed) return result;

            result = await assignHeroPointsCommand.HandleAsync(new(task.AccountId), cancellationToken);
            if (result.IsFailed)
            {
                UpdateHeroPointsCommand.Postpone(task.AccountId, TimeSpan.FromHours(1));
                return result;
            }

            return Result.Ok();
        }
    }
}
